
namespace pharmacyPOS.API.DTOs
{
    public class MedicineUpdateDto
    {
        
        public required string Name { get; set; }
        public required string BrandName { get; set; }
        public required string GenericName { get; set; }
        public required string Manufacture { get; set; }
        public required string Category { get; set; }
        public required string Strength { get; set; }
        public required bool RequiredPrescription { get; set; }
        public required int LowStockThreshold { get; set; }
        public required bool IsDeleted { get; set; }
    }
}