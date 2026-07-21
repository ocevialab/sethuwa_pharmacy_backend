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

    public string? SupplierId { get; set; }

    /// <summary>
    /// Links this stock lot back to the purchase item that created it, so purchase edits can
    /// verify how much of the lot has already been sold before allowing a quantity reduction.
    /// Null for lots created before this tracking was introduced.
    /// </summary>
    public long? PurchaseItemId { get; set; }

    public virtual Product ProductSkuNavigation { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual PurchaseItem? PurchaseItem { get; set; }
}
