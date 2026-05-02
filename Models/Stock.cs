using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Stock
{
    public long StockId { get; set; }

    public string? LotNumber { get; set; }

    public int QuantityOnHand { get; set; }

    public DateOnly ExpireDate { get; set; }

    public decimal CostPrice { get; set; }

    public string ProductSku { get; set; } = null!;

    public decimal SellingPrice { get; set; }

    public virtual Product ProductSkuNavigation { get; set; } = null!;
}
