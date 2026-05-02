# Permission-Based Authorization Guide

## Overview

This project now supports permission-based authorization alongside role-based authorization. You can use either or both systems together.

## Usage

### Single Permission (AND logic)

Use `[RequirePermission("permission_id")]` when the user must have a specific permission:

```csharp
[RequirePermission("sales:create_receipt")]
[HttpPost("create-receipt-with-items")]
public async Task<IActionResult> CreateReceiptWithItems(...)
```

### Multiple Permissions (OR logic - user needs ANY one)

Use `[RequireAnyPermission("perm1", "perm2")]` when the user needs at least one of several permissions:

```csharp
[RequireAnyPermission("sales:create_receipt", "sales:finalize_sale")]
[HttpPost("some-endpoint")]
public async Task<IActionResult> SomeEndpoint(...)
```

### Combining with Role-Based Authorization

You can still use role-based authorization:

```csharp
[Authorize(Roles = "OWNER,ADMIN")]
[RequirePermission("employee:create")]
[HttpPost]
public async Task<IActionResult> CreateEmployee(...)
```

### Migration Example

**Before (Role-based):**

```csharp
[Authorize(Roles = "OWNER,ADMIN,CASHIER,DISPENSER")]
[HttpPost("create-receipt-with-items")]
public async Task<IActionResult> CreateReceiptWithItems(...)
```

**After (Permission-based):**

```csharp
[RequirePermission("sales:create_receipt")]
[HttpPost("create-receipt-with-items")]
public async Task<IActionResult> CreateReceiptWithItems(...)
```

## How It Works

1. When a request comes in with `[RequirePermission("sales:create_receipt")]`
2. The `PermissionPolicyProvider` creates a policy with `PermissionRequirement`
3. The `PermissionAuthorizationHandler` checks the database:
   - Gets EmployeeId from JWT token claims
   - Queries `EmployeePermissions` table
   - Checks if employee has the required permission and it's active
4. If permission exists → Request is allowed
5. If permission doesn't exist → Request is denied (403 Forbidden)

## Benefits

- **Granular Control**: Assign specific permissions to each employee
- **Flexible**: Mix and match permissions per employee
- **Maintainable**: Change permissions without code changes
- **Backward Compatible**: Existing role-based authorization still works

## Testing

1. Ensure employee has permissions assigned in database
2. Login and get JWT token
3. Make request with token in Authorization header
4. System will check permissions automatically
