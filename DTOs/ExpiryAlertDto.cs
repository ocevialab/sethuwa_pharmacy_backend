public class ExpiryAlertDto
{
    public string? ProductName { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly ExpireDate { get; set; }
    public int DaysLeft { get; set; }
}
