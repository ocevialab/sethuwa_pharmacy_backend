using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Linq;

namespace pharmacyPOS.API.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if it's a permission policy (format: "Permission:permission_id")
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName["Permission:".Length..];

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .RequireAuthenticatedUser()
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Check if it's an any-permission policy (format: "AnyPermission:perm1,perm2,...")
        if (policyName.StartsWith("AnyPermission:", StringComparison.OrdinalIgnoreCase))
        {
            var permissionsString = policyName["AnyPermission:".Length..];
            var permissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new AnyPermissionRequirement(permissions))
                .RequireAuthenticatedUser()
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to base implementation for role-based policies
        return base.GetPolicyAsync(policyName);
    }
}

