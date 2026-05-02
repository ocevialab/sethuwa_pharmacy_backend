using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class PurchaseItem
{
    public long PurchaseItemId { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public int Quantity { get; set; }

    public DateOnly ExpireDate { get; set; }

    public string PurchaseId { get; set; } = null!;

    public string ProductSku { get; set; } = null!;

    public virtual Product ProductSkuNavigation { get; set; } = null!;

    public virtual Purchase Purchase { get; set; } = null!;
}
