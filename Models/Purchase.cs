using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Purchase
{
    public string PurchaseId { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateOnly? PaymentDueDate { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string SupplierId { get; set; } = null!;

    public DateTime? PaymentCompletedAt1 { get; set; }

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
