public class EmployeeResponseDto
{
    public required string EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public required string Role { get; set; }
    public string? ContactNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Address { get; set; }
    public string? EmployeeStatus { get; set; }
}
