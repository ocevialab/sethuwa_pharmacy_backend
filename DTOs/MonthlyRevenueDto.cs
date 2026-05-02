namespace pharmacyPOS.API.DTOs;

public class MonthlyRevenueDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalPaidReceipts { get; set; }
    public decimal PercentageGrowth { get; set; }     // Optional
}
