using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Supplier
{
    public string SupplierId { get; set; } = null!;

    public string SupplierName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string ContactNumber { get; set; } = null!;

    public string? EmailAddress { get; set; }

    public string? Address { get; set; }

    public string? BankName { get; set; }

    public string? BankAccountName { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankBranchName { get; set; }

    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
