public class PurchaseDTO
{
    public string? PurchaseId { get; set; }

    public string? InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public required string PaymentStatus { get; set; }
    public DateOnly? PaymentDueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SupplierId { get; set; }
    public List<PurchaseItemDto>? PurchaseItems { get; set; }
}