using System.ComponentModel.DataAnnotations;

namespace pharmacyPOS.API.DTOs;

public class FinalizeSaleDto
{
    // Customer fields
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? ContactNumber { get; set; }
    public string? EmailAddress { get; set; }

    // Discounts
    [Range(0, 100)]
    public decimal CustomerDiscountPercent { get; set; } = 0;

    public decimal RoundingDiscount { get; set; } = 0;

    // Payment - Backward compatible: can use single payment or multiple payments
    [Required]
    public string PaymentMethod { get; set; } = null!;  // Cash, Card, Bank, PayLater

    public decimal ReceivedAmount { get; set; } = 0; // only for paid (single payment mode)

    // New: Multiple payments support (if provided, takes precedence over PaymentMethod/ReceivedAmount)
    public List<CreatePaymentDto>? Payments { get; set; }
}
