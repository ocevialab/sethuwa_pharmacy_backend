using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Employee
{
    public string EmployeeId { get; set; } = null!;

    public string EmployeeName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string ContactNumber { get; set; } = null!;

    public string? EmailAddress { get; set; }

    public string? Address { get; set; }

    public string EmployeeStatus { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<Sale> SaleBilledBies { get; set; } = new List<Sale>();

    public virtual ICollection<Sale> SaleIssuedBies { get; set; } = new List<Sale>();

    public virtual ICollection<EmployeePermission> EmployeePermissions { get; set; } = new List<EmployeePermission>();
}
