namespace pharmacyPOS.API.DTOs;

public class SaleItemSummaryDto
{
    public string ProductName { get; set; } = null!;
    public string ProductSku { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal SubTotal { get; set; }
}

public class SaleSummaryDto
{
    public long SalesId { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public string IssuedBy { get; set; } = null!;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public decimal Total { get; set; }
    public decimal FinalAmountDue { get; set; }
    public List<SaleItemSummaryDto> Items { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new(); // New: list of payments
    public decimal TotalPaidAmount { get; set; } // Sum of all payments
}
