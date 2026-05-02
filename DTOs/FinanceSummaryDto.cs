namespace pharmacyPOS.API.DTOs;

public class FinanceSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal CostOfSold { get; set; }
    public decimal GrossProfit { get; set; }
    public int PendingPayments { get; set; }
    public UnpaidPurchasesDto? UnpaidPurchases { get; set; }
}
