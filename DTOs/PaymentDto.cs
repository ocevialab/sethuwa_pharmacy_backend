namespace pharmacyPOS.API.DTOs;

public class PaymentDto
{
    public long PaymentId { get; set; }
    public long SalesId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public decimal PaymentAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentDto
{
    public string PaymentMethod { get; set; } = null!;
    public decimal PaymentAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
}
