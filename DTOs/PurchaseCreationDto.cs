using System.ComponentModel.DataAnnotations;

public class PurchaseCreationDto
{
    [Required(ErrorMessage = "InvoiceNumber is required. If this is a return receipt, use 'RR-<ReceiptNumber>'. Otherwise, use 'N/A'.")]
    [MaxLength(50, ErrorMessage = "InvoiceNumber cannot exceed 50 characters.")]
    [MinLength(1, ErrorMessage = "InvoiceNumber must be at least 1 character long.")]
    public string? InvoiceNumber { get; set; }

    [Required(ErrorMessage = "InvoiceDate is required.")]
    [DataType(DataType.Date, ErrorMessage = "InvoiceDate must be a valid date.")]
    public DateTime InvoiceDate { get; set; }

    [Required(ErrorMessage = "PaymentStatus is required.")]
    [RegularExpression("Complete|Pending|Overdue", ErrorMessage = "PaymentStatus must be 'Complete', 'Pending', or 'Overdue'.")]
    public string? PaymentStatus { get; set; }

    [DataType(DataType.Date, ErrorMessage = "PaymentDueDate must be a valid date.")]
    public DateTime PaymentDueDate { get; set; }

    [RegularExpression("Cash|Credit Card|Bank Transfer|Check", ErrorMessage = "PaymentMethod must be 'Cash', 'Credit Card', 'Bank Transfer', or 'Check'.")]
    public string? PaymentMethod { get; set; }

    [Required(ErrorMessage = "TotalAmount is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be non-negative.")]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "SupplierId is required. If not applicable, use 'N/A'.")]
    public string? SupplierId { get; set; }

    [Required(ErrorMessage = "At least one purchase item is required.")]
    public List<PurchaseItemDto>? Items { get; set; }
}
