namespace pharmacyPOS.API.DTOs;

public class EmployeePermissionDto
{
    public string PermissionId { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string Module { get; set; } = null!;
    public DateTime GrantedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AssignPermissionsDto
{
    public required List<string> PermissionIds { get; set; }
}

public class UpdateEmployeePermissionsDto
{
    public required List<string> PermissionIds { get; set; }
}

public class EmployeeWithPermissionsDto
{
    public required string EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public required string Role { get; set; }
    public string? ContactNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Address { get; set; }
    public string? EmployeeStatus { get; set; }
    public List<EmployeePermissionDto> Permissions { get; set; } = new();
}

