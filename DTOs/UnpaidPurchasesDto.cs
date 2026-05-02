namespace pharmacyPOS.API.DTOs;

public class UnpaidPurchasesDto
{
    public PurchasePaymentStatusDto Pending { get; set; } = null!;
    public PurchasePaymentStatusDto Overdue { get; set; } = null!;
    public PurchasePaymentStatusDto Total { get; set; } = null!;
}

