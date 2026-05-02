using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.Models;

public partial class Customer
{
    public string CustomerId { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string ContactNumber { get; set; } = null!;

    public string? EmailAddress { get; set; }

    public string? Address { get; set; }

    public decimal? Discount { get; set; }

    public string CustomerStatus { get; set; } = null!;

    public virtual ICollection<CustomerRecurrentItem> CustomerRecurrentItems { get; set; } = new List<CustomerRecurrentItem>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
