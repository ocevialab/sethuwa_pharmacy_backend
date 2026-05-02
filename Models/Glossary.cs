using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Glossary
{
    public string GlossaryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string BrandName { get; set; } = null!;

    public int LowStockThreshold { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
