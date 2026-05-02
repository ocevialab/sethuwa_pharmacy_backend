using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Product
{
    public string ProductSku { get; set; } = null!;

    public string ProductType { get; set; } = null!;

    public string? MedicineId { get; set; }

    public string? GlossaryId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<CustomerRecurrentItem> CustomerRecurrentItems { get; set; } = new List<CustomerRecurrentItem>();

    public virtual Glossary? Glossary { get; set; }

    public virtual Medicine? Medicine { get; set; }

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual ICollection<SalesItem> SalesItems { get; set; } = new List<SalesItem>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
