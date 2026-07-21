using System.ComponentModel.DataAnnotations;

namespace pharmacyPOS.API.DTOs;

public class EditPurchaseItemDto
{
    /// <summary>
    /// Identifies an existing line item to update. Leave null/omitted to add a brand-new line item.
    /// </summary>
    public long? PurchaseItemId { get; set; }

    [Required(ErrorMessage = "ProductSKU is required.")]
    public string ProductSKU { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "CostPrice must be non-negative.")]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "SellingPrice must be non-negative.")]
    public decimal SellingPrice { get; set; }

    [Required(ErrorMessage = "ExpireDate is required.")]
    public DateTime ExpireDate { get; set; }
}

public class EditPurchaseDto
{
    [Required(ErrorMessage = "InvoiceNumber is required. If this is a return receipt, use 'RR-<ReceiptNumber>'. Otherwise, use 'N/A'.")]
    [MaxLength(50, ErrorMessage = "InvoiceNumber cannot exceed 50 characters.")]
    public string InvoiceNumber { get; set; } = null!;

    [Required(ErrorMessage = "InvoiceDate is required.")]
    public DateTime InvoiceDate { get; set; }

    // Nullable to match Purchase.PaymentDueDate — a "Complete" purchase may have no due date.
    // Payment status/method itself is not editable here; use the dedicated payment-status endpoint for that.
    public DateTime? PaymentDueDate { get; set; }

    [Required(ErrorMessage = "SupplierId is required. If not applicable, use 'N/A'.")]
    public string SupplierId { get; set; } = null!;

    [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be non-negative.")]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "At least one purchase item is required.")]
    [MinLength(1, ErrorMessage = "At least one purchase item is required.")]
    public List<EditPurchaseItemDto> Items { get; set; } = new();
}
