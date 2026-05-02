using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class EmployeePermission
{
    public long EmployeePermissionId { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string PermissionId { get; set; } = null!;

    public DateTime GrantedAt { get; set; }

    public string? GrantedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Permission Permission { get; set; } = null!;
}
