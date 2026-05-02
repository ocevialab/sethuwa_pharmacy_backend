namespace pharmacyPOS.API.DTOs;

public class SupplierSearchResultDto
{
    public string SupplierId { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string ContactNumber { get; set; } = null!;
    public string? EmailAddress { get; set; }
}

