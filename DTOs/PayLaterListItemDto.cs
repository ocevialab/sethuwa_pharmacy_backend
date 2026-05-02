namespace pharmacyPOS.API.DTOs;

public class PayLaterListItemDto
{
    public string ReceiptNumber { get; set; } = null!;
    public string? CustomerName { get; set; }
    public string? ContactNumber { get; set; }
    public decimal FinalAmountDue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateOnly Date { get; set; }
    public string IssuedBy { get; set; } = null!;
    public int DaysOutstanding { get; set; }
}
