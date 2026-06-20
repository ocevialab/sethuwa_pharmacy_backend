namespace pharmacyPOS.API.DTOs;

public class MedicineBarcodeDto
{
    public string MedicineId { get; set; } = null!;
    public string ProductSku { get; set; } = null!;
    public string Barcode { get; set; } = null!;
    public string? MedicineName { get; set; }
}
