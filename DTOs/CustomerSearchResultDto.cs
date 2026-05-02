namespace pharmacyPOS.API.DTOs;

public class CustomerSearchResultDto
{
    public string CustomerId { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string ContactNumber { get; set; } = null!;
    public decimal? Discount { get; set; }
}
