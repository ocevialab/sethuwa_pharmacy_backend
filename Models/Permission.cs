using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Permission
{
    public string PermissionId { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    public string Module { get; set; } = null!;

    public string? Description { get; set; }

    public string? Endpoint { get; set; }

    public string? HttpMethod { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<EmployeePermission> EmployeePermissions { get; set; } = new List<EmployeePermission>();
}
