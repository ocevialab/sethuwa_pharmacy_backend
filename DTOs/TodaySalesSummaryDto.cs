namespace pharmacyPOS.API.DTOs;

public class TodaySalesSummaryDto
{
    public decimal TotalSalesToday { get; set; }
    public int TotalReceiptsToday { get; set; }
    public decimal CashTotal { get; set; }
    public decimal CardTotal { get; set; }

    // Optional: compared with yesterday
    public decimal PercentageChange { get; set; }
}
