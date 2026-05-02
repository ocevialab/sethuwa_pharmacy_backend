namespace pharmacyPOS.API.DTOs;

public class FinancialSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossProfitMargin { get; set; }
    public decimal TotalPurchaseExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalSalesCount { get; set; }
    public int TotalPurchaseCount { get; set; }
}

