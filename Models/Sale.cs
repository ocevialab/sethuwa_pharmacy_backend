using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Sale
{
    public long SalesId { get; set; }

    public string? ReceiptNumber { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Time { get; set; }

    public string SaleStatus { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentCompletedAt { get; set; }

    public decimal? CustomerDiscountPercent { get; set; }

    public decimal? RoundingDiscount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal FinalAmountDue { get; set; }

    public string IssuedById { get; set; } = null!;

    public string? BilledById { get; set; }

    public string? CustomerId { get; set; }

    public virtual Employee? BilledBy { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee IssuedBy { get; set; } = null!;

    public virtual ICollection<SalesItem> SalesItems { get; set; } = new List<SalesItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
