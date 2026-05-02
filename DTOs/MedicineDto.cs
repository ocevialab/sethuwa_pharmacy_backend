public class MedicineDto
{
    public string? MedicineId { get; set; }
    public string? Name { get; set; }
    public string? BrandName { get; set; }
    public string? GenericName { get; set; }
    public string? Manufacture { get; set; }
    public string? Category { get; set; }
    public string? Strength { get; set; }
    public bool? RequiredPrescription { get; set; }
    public int? LowStockThreshold { get; set; }

    public bool IsDeleted { get; set; }

    public string? ProductSku { get; set; }
}
