using Microsoft.AspNetCore.Authorization;

namespace pharmacyPOS.API.Authorization;

/// <summary>
/// Requires the user to have at least one of the specified permissions (OR logic)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public string[] Permissions { get; }

    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission is required", nameof(permissions));

        Permissions = permissions;
        // Use the first permission as the policy name, but we'll handle multiple in the handler
        Policy = $"AnyPermission:{string.Join(",", permissions)}";
    }
}

