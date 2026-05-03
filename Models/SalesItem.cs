using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class SalesItem
{
    public long SalesItemId { get; set; }

    public long SalesId { get; set; }

    public string ProductSku { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal SubTotal { get; set; }

    public long? StockId { get; set; }

    public virtual Product ProductSkuNavigation { get; set; } = null!;

    public virtual Sale Sales { get; set; } = null!;

    public virtual Stock? Stock { get; set; }
}
