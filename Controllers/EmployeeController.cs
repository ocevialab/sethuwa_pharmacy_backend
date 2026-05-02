using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class EmployeeController : ControllerBase
{
    private readonly ThilankaPharmacyDbContext _context;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(ThilankaPharmacyDbContext context, ILogger<EmployeeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    //POST: api/Employee
    [RequirePermission("employee:create")]
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(EmployeeCreationDto dto)
    {
        _logger.LogInformation("Create employee request: {@Dto}", dto);

        // Check email uniqueness
        if (!string.IsNullOrWhiteSpace(dto.EmailAddress))
        {
            if (await _context.Employees.AnyAsync(e => e.EmailAddress == dto.EmailAddress))
            {
                _logger.LogWarning("Duplicate email attempted: {Email}", dto.EmailAddress);
                return Conflict("An employee with the provided email already exists.");
            }
        }

        var newEmployeeId = await _context.Employees.GenerateNextSequentialId("EMP", "EmployeeId");

        // Get current user (who is creating the employee)
        var grantedBy = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create employee
            var employee = new Employee
            {
                EmployeeId = newEmployeeId,
                EmployeeName = dto.EmployeeName,
                Role = dto.Role?.ToUpper() ?? string.Empty,
                ContactNumber = dto.ContactNumber ?? string.Empty,
                EmailAddress = dto.EmailAddress,
                Address = dto.Address,
                EmployeeStatus = dto.EmployeeStatus,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {EmployeeId} created successfully", newEmployeeId);

            // Assign permissions if provided
            if (dto.PermissionIds != null && dto.PermissionIds.Count > 0)
            {
                _logger.LogInformation("Assigning {Count} permissions to employee {EmployeeId}", dto.PermissionIds.Count, newEmployeeId);

                // Validate all permission IDs exist
                var validPermissionIds = await _context.Permissions
                    .Where(p => dto.PermissionIds.Contains(p.PermissionId) && p.IsActive)
                    .Select(p => p.PermissionId)
                    .ToListAsync();

                var invalidPermissionIds = dto.PermissionIds.Except(validPermissionIds).ToList();
                if (invalidPermissionIds.Any())
                {
                    _logger.LogWarning("Invalid permission IDs: {InvalidIds}", string.Join(", ", invalidPermissionIds));
                    await transaction.RollbackAsync();
                    return BadRequest($"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}");
                }

                // Assign permissions
                foreach (var permissionId in validPermissionIds)
                {
                    var employeePermission = new EmployeePermission
                    {
                        EmployeeId = newEmployeeId,
                        PermissionId = permissionId,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = grantedBy,
                        IsActive = true
                    };

                    _context.EmployeePermissions.Add(employeePermission);
                    _logger.LogInformation("Assigned permission {PermissionId} to employee {EmployeeId}", permissionId, newEmployeeId);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully assigned {Count} permissions to employee {EmployeeId}", validPermissionIds.Count, newEmployeeId);
            }

            await transaction.CommitAsync();

            // Return sanitized DTO
            var response = new EmployeeResponseDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.EmployeeName,
                Role = employee.Role,
                ContactNumber = employee.ContactNumber,
                EmailAddress = employee.EmailAddress,
                Address = employee.Address,
                EmployeeStatus = employee.EmployeeStatus
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while creating the employee.");
        }
    }

    // UPDATE: api/Employee/{id}
    [RequirePermission("employee:update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(string id, EmployeeUpdateDto dto)
    {
        _logger.LogInformation("Update employee {Id}: {@Dto}", id, dto);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {Id}", id);
            return NotFound("Employee not found.");
        }

        // Email check
        if (dto.EmailAddress != null && dto.EmailAddress != employee.EmailAddress)
        {
            if (await _context.Employees.AnyAsync(e => e.EmailAddress == dto.EmailAddress))
            {
                _logger.LogWarning("Duplicate email during update on {Id}: {Email}", id, dto.EmailAddress);
                return Conflict("An employee with the provided email address already exists.");
            }

            employee.EmailAddress = dto.EmailAddress;
        }

        // Update values
        employee.EmployeeName = dto.EmployeeName;
        employee.Role = dto.Role?.ToUpper() ?? string.Empty;
        employee.ContactNumber = dto.ContactNumber ?? string.Empty;
        employee.Address = dto.Address;
        employee.EmployeeStatus = dto.EmployeeStatus;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {Id} updated successfully", id);

        var response = new EmployeeResponseDto
        {
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            Role = employee.Role,
            ContactNumber = employee.ContactNumber,
            EmailAddress = employee.EmailAddress,
            Address = employee.Address,
            EmployeeStatus = employee.EmployeeStatus
        };

        return Ok(response);
    }

    // SOFT DELETE
    [RequirePermission("employee:delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDeleteEmployee(string id)
    {
        _logger.LogInformation("Soft delete employee {Id}", id);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {Id}", id);
            return NotFound("Employee not found.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove all active permissions first
            var employeePermissions = await _context.EmployeePermissions
                .Where(ep => ep.EmployeeId == id && ep.IsActive)
                .ToListAsync();

            if (employeePermissions.Any())
            {
                _logger.LogInformation("Removing {Count} permissions from employee {Id} before deletion", employeePermissions.Count, id);

                foreach (var permission in employeePermissions)
                {
                    permission.IsActive = false;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully removed all permissions from employee {Id}", id);
            }
            else
            {
                _logger.LogInformation("No active permissions found for employee {Id}", id);
            }

            // Soft delete employee
            employee.EmployeeStatus = "INACTIVE";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Employee {Id} soft deleted successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting employee {Id}", id);
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while deleting the employee.");
        }
    }

    // GET: api/Employee/{id}
    [RequirePermission("employee:view")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(string id)
    {
        _logger.LogInformation("Get employee {Id}", id);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {Id}", id);
            return NotFound("Employee not found.");
        }

        var response = new EmployeeResponseDto
        {
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            Role = employee.Role,
            ContactNumber = employee.ContactNumber,
            EmailAddress = employee.EmailAddress,
            Address = employee.Address,
            EmployeeStatus = employee.EmployeeStatus
        };

        return Ok(response);
    }

    // GET: api/Employee/{id}/with-permissions
    [RequirePermission("employee:view_permissions")]
    [HttpGet("{id}/with-permissions")]
    public async Task<IActionResult> GetEmployeeWithPermissions(string id)
    {
        _logger.LogInformation("Get employee {Id} with permissions", id);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {Id}", id);
            return NotFound("Employee not found.");
        }

        var permissions = await _context.EmployeePermissions
            .Include(ep => ep.Permission)
            .Where(ep => ep.EmployeeId == id && ep.IsActive)
            .Select(ep => new EmployeePermissionDto
            {
                PermissionId = ep.PermissionId,
                PermissionName = ep.Permission.PermissionName,
                Module = ep.Permission.Module,
                GrantedAt = ep.GrantedAt,
                IsActive = ep.IsActive
            })
            .ToListAsync();

        var response = new EmployeeWithPermissionsDto
        {
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            Role = employee.Role,
            ContactNumber = employee.ContactNumber,
            EmailAddress = employee.EmailAddress,
            Address = employee.Address,
            EmployeeStatus = employee.EmployeeStatus,
            Permissions = permissions
        };

        return Ok(response);
    }

    // GET ALL
    [RequirePermission("employee:view_all")]
    [HttpGet]
    public async Task<IActionResult> GetAllEmployees()
    {
        _logger.LogInformation("Retrieving all employees");

        var employees = await _context.Employees.ToListAsync();

        // Convert to DTO list
        var response = employees.Select(e => new EmployeeResponseDto
        {
            EmployeeId = e.EmployeeId,
            EmployeeName = e.EmployeeName,
            Role = e.Role,
            ContactNumber = e.ContactNumber,
            EmailAddress = e.EmailAddress,
            Address = e.Address,
            EmployeeStatus = e.EmployeeStatus
        });

        return Ok(response);
    }

    //change password
    /*
    1. Verify old password
    2. Hash new password

    */

    [RequirePermission("employee:change_password")]
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(string id, ChangePasswordDto dto)
    {
        _logger.LogInformation("Change password request for employee {Id}", id);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {Id}", id);
            return NotFound("Employee not found.");
        }

        // Verify old password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, employee.PasswordHash))
        {
            _logger.LogWarning("Incorrect old password for employee {Id}", id);
            return BadRequest("Old password is incorrect.");
        }

        // Hash new password and update
        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for employee {Id}", id);

        return NoContent();
    }
}
