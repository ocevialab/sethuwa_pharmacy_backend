public class PurchaseItemDto
{
    public string? ProductSKU { get; set; }
    public string? ProductName { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpireDate { get; set; }
}
