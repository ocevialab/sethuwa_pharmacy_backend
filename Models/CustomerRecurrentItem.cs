using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class CustomerRecurrentItem
{
    public long RecurrentId { get; set; }

    public int Quantity { get; set; }

    public string CustomerId { get; set; } = null!;

    public string ProductSku { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product ProductSkuNavigation { get; set; } = null!;
}
