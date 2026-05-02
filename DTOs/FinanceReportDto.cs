using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.DTOs;

public class FinanceReportDto
{
    public string ReportType { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public RevenueDetailsDto RevenueDetails { get; set; } = null!;
    public PurchaseDetailsDto PurchaseDetails { get; set; } = null!;
    public FinancialSummaryDto FinancialSummary { get; set; } = null!;
    public List<MostSellingItemDto> TopSellingItems { get; set; } = new();
    public List<SupplierExpenseDto> SupplierExpenses { get; set; } = new();
    public PaymentMethodBreakdownDto PaymentMethodBreakdown { get; set; } = null!;
    public UnpaidPurchasesDto UnpaidPurchases { get; set; } = null!;
}

