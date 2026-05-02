namespace pharmacyPOS.API.DTOs;

public class SaleListItemDto
{
    public long SalesId { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string SaleStatus { get; set; } = null!;
    public string? PaymentMethod { get; set; } // Deprecated: kept for backward compatibility
    public DateTime? PaymentCompletedAt { get; set; } // Deprecated: kept for backward compatibility
    public decimal TotalAmount { get; set; }
    public decimal FinalAmountDue { get; set; }
    public decimal? CustomerDiscountPercent { get; set; }
    public decimal? RoundingDiscount { get; set; }
    public string IssuedBy { get; set; } = null!;
    public string? CustomerName { get; set; }
    public List<SaleItemSummaryDto> Items { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new(); // New: list of payments
    public decimal TotalPaidAmount { get; set; } // Sum of all payments
}

