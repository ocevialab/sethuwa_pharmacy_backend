namespace pharmacyPOS.API.DTOs
{
    public class GlossaryUpdateDto
    {
        public required string Name { get; set; }
        public string? BrandName { get; set; }
        public int LowStockThreshold { get; set; }
    }
}

