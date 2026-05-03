using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;

namespace pharmacyPOS.API.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly SethsuwaPharmacyDbContext _context;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        SethsuwaPharmacyDbContext context,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return;
        }

        var employeeId = context.User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("EmployeeId claim not found in token");
            return;
        }

        // Check if employee has the required permission
        var hasPermission = await _context.EmployeePermissions
            .AnyAsync(ep =>
                ep.EmployeeId == employeeId &&
                ep.PermissionId == requirement.Permission &&
                ep.IsActive);

        if (hasPermission)
        {
            _logger.LogDebug("Employee {EmployeeId} has permission {Permission}",
                employeeId, requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Employee {EmployeeId} does not have permission {Permission}",
                employeeId, requirement.Permission);
        }
    }
}

// Handler for AnyPermissionRequirement (OR logic)
public class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly SethsuwaPharmacyDbContext _context;
    private readonly ILogger<AnyPermissionAuthorizationHandler> _logger;

    public AnyPermissionAuthorizationHandler(
        SethsuwaPharmacyDbContext context,
        ILogger<AnyPermissionAuthorizationHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return;
        }

        var employeeId = context.User.FindFirst("EmployeeId")?.Value;

        if (string.IsNullOrEmpty(employeeId))
        {
            _logger.LogWarning("EmployeeId claim not found in token");
            return;
        }

        // Check if employee has ANY of the required permissions
        var hasAnyPermission = await _context.EmployeePermissions
            .AnyAsync(ep =>
                ep.EmployeeId == employeeId &&
                requirement.Permissions.Contains(ep.PermissionId) &&
                ep.IsActive);

        if (hasAnyPermission)
        {
            _logger.LogDebug("Employee {EmployeeId} has at least one of the required permissions",
                employeeId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Employee {EmployeeId} does not have any of the required permissions: {Permissions}",
                employeeId, string.Join(", ", requirement.Permissions));
        }
    }
}

