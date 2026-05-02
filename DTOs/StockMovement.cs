public partial class StockMovement
{
    public long Id { get; set; }
    public string ProductSku { get; set; } = null!;
    public long Stock_Id { get; set; }
    public int QuantityChanged { get; set; }
    public string Reason { get; set; } = null!;
    public long? SalesId { get; set; }
    public DateTime CreatedAt { get; set; }
}
