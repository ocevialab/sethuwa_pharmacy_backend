public class GlossaryDto
{
    public required string GlossaryId { get; set; }
    public required string Name { get; set; }
    public required string BrandName { get; set; }

    public required int LowStockThreshold { get; set; }

    public bool IsDeleted { get; set; }

    public string? ProductSku { get; set; }
}