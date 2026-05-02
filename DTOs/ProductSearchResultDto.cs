public class ProductSearchResultDto
{
    public string? ProductSku { get; set; }
    public string? Name { get; set; }
    public string? Strength { get; set; }
    public string? ProductType { get; set; }

    public decimal SellingPrice { get; set; }
    public int TotalQuantityOnHand { get; set; }
}
