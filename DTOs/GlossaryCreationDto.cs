namespace pharmacyPOS.API.DTOs
{
    public class GlossaryCreationDto
    {
        public required string Name { get; set; }
        public required string BrandName { get; set; }
        public int LowStockThreshold { get; set; }

        public bool IsDeleted { get; set; }
    }
}