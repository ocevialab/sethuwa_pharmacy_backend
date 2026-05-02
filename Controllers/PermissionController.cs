using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class PermissionController : ControllerBase
{
    private readonly SethuwaPharmacyDbContext _context;
    private readonly ILogger<PermissionController> _logger;

    public PermissionController(SethuwaPharmacyDbContext context, ILogger<PermissionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Permission/employee/{employeeId}
    [RequirePermission("permission:view")]
    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetEmployeePermissions(string employeeId)
    {
        _logger.LogInformation("Getting permissions for employee {EmployeeId}", employeeId);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {EmployeeId}", employeeId);
            return NotFound("Employee not found.");
        }

        var permissions = await _context.EmployeePermissions
            .Include(ep => ep.Permission)
            .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
            .Select(ep => new EmployeePermissionDto
            {
                PermissionId = ep.PermissionId,
                PermissionName = ep.Permission.PermissionName,
                Module = ep.Permission.Module,
                GrantedAt = ep.GrantedAt,
                IsActive = ep.IsActive
            })
            .ToListAsync();

        return Ok(permissions);
    }

    // POST: api/Permission/employee/{employeeId}/assign
    [RequirePermission("permission:assign")]
    [HttpPost("employee/{employeeId}/assign")]
    public async Task<IActionResult> AssignPermissions(string employeeId, AssignPermissionsDto dto)
    {
        _logger.LogInformation("Assigning permissions to employee {EmployeeId}: {@PermissionIds}", employeeId, dto.PermissionIds);

        if (dto.PermissionIds == null || dto.PermissionIds.Count == 0)
        {
            return BadRequest("At least one permission ID is required.");
        }

        // Validate employee exists
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {EmployeeId}", employeeId);
            return NotFound("Employee not found.");
        }

        // Get current user (who is granting permissions)
        var grantedBy = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

        // Validate all permission IDs exist
        var validPermissionIds = await _context.Permissions
            .Where(p => dto.PermissionIds.Contains(p.PermissionId) && p.IsActive)
            .Select(p => p.PermissionId)
            .ToListAsync();

        var invalidPermissionIds = dto.PermissionIds.Except(validPermissionIds).ToList();
        if (invalidPermissionIds.Any())
        {
            _logger.LogWarning("Invalid permission IDs: {InvalidIds}", string.Join(", ", invalidPermissionIds));
            return BadRequest($"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get existing active permissions for this employee
            var existingPermissions = await _context.EmployeePermissions
                .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
                .Select(ep => ep.PermissionId)
                .ToListAsync();

            // Find permissions to add (not already assigned)
            var permissionsToAdd = validPermissionIds.Except(existingPermissions).ToList();

            // Add new permissions
            foreach (var permissionId in permissionsToAdd)
            {
                var employeePermission = new EmployeePermission
                {
                    EmployeeId = employeeId,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = grantedBy,
                    IsActive = true
                };

                _context.EmployeePermissions.Add(employeePermission);
                _logger.LogInformation("Adding permission {PermissionId} to employee {EmployeeId}", permissionId, employeeId);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully assigned {Count} permissions to employee {EmployeeId}", permissionsToAdd.Count, employeeId);

            return Ok(new
            {
                Message = "Permissions assigned successfully",
                EmployeeId = employeeId,
                PermissionsAssigned = permissionsToAdd.Count,
                PermissionIds = permissionsToAdd
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permissions to employee {EmployeeId}", employeeId);
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while assigning permissions.");
        }
    }

    // PUT: api/Permission/employee/{employeeId}/update
    [RequirePermission("permission:update")]
    [HttpPut("employee/{employeeId}/update")]
    public async Task<IActionResult> UpdateEmployeePermissions(string employeeId, UpdateEmployeePermissionsDto dto)
    {
        _logger.LogInformation("Updating permissions for employee {EmployeeId}: {@PermissionIds}", employeeId, dto.PermissionIds);

        if (dto.PermissionIds == null)
        {
            return BadRequest("PermissionIds is required.");
        }

        // Validate employee exists
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {EmployeeId}", employeeId);
            return NotFound("Employee not found.");
        }

        // Get current user (who is updating permissions)
        var grantedBy = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

        // Validate all permission IDs exist
        var validPermissionIds = await _context.Permissions
            .Where(p => dto.PermissionIds.Contains(p.PermissionId) && p.IsActive)
            .Select(p => p.PermissionId)
            .ToListAsync();

        var invalidPermissionIds = dto.PermissionIds.Except(validPermissionIds).ToList();
        if (invalidPermissionIds.Any())
        {
            _logger.LogWarning("Invalid permission IDs: {InvalidIds}", string.Join(", ", invalidPermissionIds));
            return BadRequest($"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get existing active permissions
            var existingPermissions = await _context.EmployeePermissions
                .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
                .ToListAsync();

            var existingPermissionIds = existingPermissions.Select(ep => ep.PermissionId).ToList();

            // Find permissions to add
            var permissionsToAdd = validPermissionIds.Except(existingPermissionIds).ToList();

            // Find permissions to remove (soft delete)
            var permissionsToRemove = existingPermissionIds.Except(validPermissionIds).ToList();

            // Add new permissions
            foreach (var permissionId in permissionsToAdd)
            {
                var employeePermission = new EmployeePermission
                {
                    EmployeeId = employeeId,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = grantedBy,
                    IsActive = true
                };

                _context.EmployeePermissions.Add(employeePermission);
                _logger.LogInformation("Adding permission {PermissionId} to employee {EmployeeId}", permissionId, employeeId);
            }

            // Soft delete removed permissions
            foreach (var permissionId in permissionsToRemove)
            {
                var employeePermission = existingPermissions.First(ep => ep.PermissionId == permissionId);
                employeePermission.IsActive = false;
                _logger.LogInformation("Removing permission {PermissionId} from employee {EmployeeId}", permissionId, employeeId);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully updated permissions for employee {EmployeeId}. Added: {Added}, Removed: {Removed}",
                employeeId, permissionsToAdd.Count, permissionsToRemove.Count);

            return Ok(new
            {
                Message = "Permissions updated successfully",
                EmployeeId = employeeId,
                PermissionsAdded = permissionsToAdd.Count,
                PermissionsRemoved = permissionsToRemove.Count,
                NewPermissionIds = validPermissionIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions for employee {EmployeeId}", employeeId);
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while updating permissions.");
        }
    }

    // DELETE: api/Permission/employee/{employeeId}/remove-all
    [RequirePermission("permission:remove")]
    [HttpDelete("employee/{employeeId}/remove-all")]
    public async Task<IActionResult> RemoveAllEmployeePermissions(string employeeId)
    {
        _logger.LogInformation("Removing all permissions for employee {EmployeeId}", employeeId);

        // Validate employee exists
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {EmployeeId}", employeeId);
            return NotFound("Employee not found.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get all active permissions for this employee
            var employeePermissions = await _context.EmployeePermissions
                .Where(ep => ep.EmployeeId == employeeId && ep.IsActive)
                .ToListAsync();

            if (!employeePermissions.Any())
            {
                _logger.LogInformation("No active permissions found for employee {EmployeeId}", employeeId);
                await transaction.CommitAsync();
                return Ok(new
                {
                    Message = "No active permissions to remove",
                    EmployeeId = employeeId,
                    PermissionsRemoved = 0
                });
            }

            // Soft delete all permissions
            foreach (var permission in employeePermissions)
            {
                permission.IsActive = false;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully removed {Count} permissions from employee {EmployeeId}",
                employeePermissions.Count, employeeId);

            return Ok(new
            {
                Message = "All permissions removed successfully",
                EmployeeId = employeeId,
                PermissionsRemoved = employeePermissions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permissions for employee {EmployeeId}", employeeId);
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while removing permissions.");
        }
    }

    // GET: api/Permission/all
    [RequirePermission("permission:view_all")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllPermissions()
    {
        _logger.LogInformation("Getting all active permissions");

        var permissions = await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.PermissionName)
            .Select(p => new
            {
                p.PermissionId,
                p.PermissionName,
                p.Module,
                p.Description,
                p.Endpoint,
                p.HttpMethod
            })
            .ToListAsync();

        return Ok(permissions);
    }
}

