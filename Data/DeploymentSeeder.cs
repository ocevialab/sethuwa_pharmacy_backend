using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;

namespace pharmacyPOS.API.Data;

/// <summary>
/// Idempotent QA/Production seed: upsert permissions, ensure OWNER EMP-1, grant all active permissions.
/// </summary>
public static class DeploymentSeeder
{
    public const string OwnerEmployeeId = "EMP-1";
    private const string OwnerRole = "OWNER";
    private const string OwnerName = "Owner";
    private const string OwnerContact = "0000000000";
    private const string OwnerStatus = "ACTIVE";
    /// <summary>Default login when EMP-1 is first created (change after first login in production).</summary>
    private const string DefaultOwnerPlainPassword = "Admin@1234";

    public static async Task SeedAsync(SethsuwaPharmacyDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var row in PermissionSeedData.All)
        {
            var existing = await db.Permissions
                .FirstOrDefaultAsync(p => p.PermissionId == row.PermissionId, cancellationToken);

            if (existing == null)
            {
                db.Permissions.Add(new Permission
                {
                    PermissionId = row.PermissionId,
                    PermissionName = row.PermissionName,
                    Module = row.Module,
                    Description = row.Description,
                    Endpoint = Truncate(row.Endpoint, 255),
                    HttpMethod = Truncate(row.HttpMethod, 10),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = null,
                });
            }
            else
            {
                existing.PermissionName = row.PermissionName;
                existing.Module = row.Module;
                existing.Description = row.Description;
                existing.Endpoint = Truncate(row.Endpoint, 255);
                existing.HttpMethod = Truncate(row.HttpMethod, 10);
                existing.IsActive = true;
                existing.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var owner = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == OwnerEmployeeId, cancellationToken);
        if (owner == null)
        {
            owner = new Employee
            {
                EmployeeId = OwnerEmployeeId,
                EmployeeName = OwnerName,
                Role = OwnerRole,
                ContactNumber = OwnerContact,
                EmailAddress = null,
                Address = null,
                EmployeeStatus = OwnerStatus,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultOwnerPlainPassword, workFactor: 11),
            };
            db.Employees.Add(owner);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Created default OWNER {EmployeeId}. Change password immediately (default: {DefaultPassword}).",
                OwnerEmployeeId,
                DefaultOwnerPlainPassword);
        }
        else
        {
            owner.EmployeeName = OwnerName;
            owner.Role = OwnerRole;
            owner.EmployeeStatus = OwnerStatus;
            if (string.IsNullOrWhiteSpace(owner.ContactNumber))
                owner.ContactNumber = OwnerContact;
            await db.SaveChangesAsync(cancellationToken);
        }

        var activePermissionIds = await db.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.PermissionId)
            .ToListAsync(cancellationToken);

        var existingPairs = await db.EmployeePermissions
            .Where(ep => ep.EmployeeId == OwnerEmployeeId && ep.IsActive)
            .Select(ep => ep.PermissionId)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in activePermissionIds)
        {
            if (existingPairs.Contains(permissionId))
                continue;

            var alreadyInactive = await db.EmployeePermissions
                .AnyAsync(
                    ep => ep.EmployeeId == OwnerEmployeeId && ep.PermissionId == permissionId && !ep.IsActive,
                    cancellationToken);

            if (alreadyInactive)
            {
                await db.EmployeePermissions
                    .Where(ep => ep.EmployeeId == OwnerEmployeeId && ep.PermissionId == permissionId)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(ep => ep.IsActive, true)
                            .SetProperty(ep => ep.GrantedAt, now)
                            .SetProperty(ep => ep.GrantedBy, OwnerEmployeeId),
                        cancellationToken);
                continue;
            }

            db.EmployeePermissions.Add(new EmployeePermission
            {
                EmployeeId = OwnerEmployeeId,
                PermissionId = permissionId,
                GrantedAt = now,
                GrantedBy = OwnerEmployeeId,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        // "sales:view_receipt" is a prerequisite for the edit-draft UI (it loads the draft before editing it),
        // so Admins need both to actually use the feature.
        await GrantPermissionToAdminsAsync(db, "sales:edit_draft", now, cancellationToken);
        await GrantPermissionToAdminsAsync(db, "sales:view_receipt", now, cancellationToken);

        logger.LogInformation(
            "Deployment seed completed: {PermissionCount} permissions, OWNER {EmployeeId} linked to all active permissions.",
            activePermissionIds.Count,
            OwnerEmployeeId);
    }

    /// <summary>
    /// Auto-grants a specific permission to every employee whose Role is "ADMIN" (case-insensitive).
    /// OWNER already receives every active permission via the loop above; this covers the Admin role
    /// for permissions that should be restricted to Admin/Owner only (e.g. editing draft receipts).
    /// </summary>
    private static async Task GrantPermissionToAdminsAsync(
        SethsuwaPharmacyDbContext db,
        string permissionId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var permissionExists = await db.Permissions
            .AnyAsync(p => p.PermissionId == permissionId && p.IsActive, cancellationToken);
        if (!permissionExists)
            return;

        var adminEmployeeIds = await db.Employees
            .Where(e => e.Role.ToUpper() == "ADMIN")
            .Select(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        foreach (var adminId in adminEmployeeIds)
        {
            var existingGrant = await db.EmployeePermissions
                .FirstOrDefaultAsync(
                    ep => ep.EmployeeId == adminId && ep.PermissionId == permissionId,
                    cancellationToken);

            if (existingGrant == null)
            {
                db.EmployeePermissions.Add(new EmployeePermission
                {
                    EmployeeId = adminId,
                    PermissionId = permissionId,
                    GrantedAt = now,
                    GrantedBy = OwnerEmployeeId,
                    IsActive = true,
                });
            }
            else if (!existingGrant.IsActive)
            {
                existingGrant.IsActive = true;
                existingGrant.GrantedAt = now;
                existingGrant.GrantedBy = OwnerEmployeeId;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLen)
    {
        if (value == null || value.Length <= maxLen)
            return value;
        return value[..maxLen];
    }
}
