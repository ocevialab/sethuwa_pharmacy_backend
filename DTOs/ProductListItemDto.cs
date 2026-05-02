public class ProductListItemDto
{
    public string? ProductSku { get; set; }
    public string? Name { get; set; }
    public int Stock { get; set; }
    public decimal UnitPrice { get; set; }
    public int LowStockThreshold { get; set; }
    public string? ProductType { get; set; }
}
