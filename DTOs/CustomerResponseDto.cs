namespace pharmacyPOS.API.DTOs;

public class CustomerResponseDto
{
    public string CustomerId { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string ContactNumber { get; set; } = null!;
    public string? EmailAddress { get; set; }
    public string? Address { get; set; }
    public decimal? Discount { get; set; }
    public string CustomerStatus { get; set; } = null!;
}
