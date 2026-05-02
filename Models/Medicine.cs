using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Medicine
{
    public string MedicineId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? BrandName { get; set; }

    public string? GenericName { get; set; }

    public string? Manufacture { get; set; }

    public string? Category { get; set; }

    public string? Strength { get; set; }

    public bool? RequiredPrescription { get; set; }

    public int? LowStockThreshold { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
