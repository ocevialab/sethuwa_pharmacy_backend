using System.ComponentModel.DataAnnotations;

namespace pharmacyPOS.API.DTOs;

public class CompletePayLaterPaymentDto
{
    [Required]
    public string PaymentMethod { get; set; } = null!; // Cash, Card, Bank

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal ReceivedAmount { get; set; }

    public decimal RoundingDiscount { get; set; } = 0;
}
