namespace pharmacyPOS.API.DTOs;

public class ProductBarcodeDto
{
    public string ProductSku { get; set; } = null!;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductType { get; set; } = null!;
}
