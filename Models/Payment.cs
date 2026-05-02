using System;

namespace pharmacyPOS.API.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long SalesId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal PaymentAmount { get; set; }

    public DateTime PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Sale Sale { get; set; } = null!;
}
