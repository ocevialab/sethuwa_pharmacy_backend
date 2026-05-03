using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.DTOs
{
    /// <summary>
    /// This DTO combines master data with real-time stock information.
    /// </summary>
    public class ItemDetailDto
    {
        public string? ProductSku { get; set; }
        public string? Name { get; set; }
        public string? BrandName { get; set; }
        public string? ProductType { get; set; }
        public int LowStockThreshold { get; set; }
        public string? GenericName { get; set; }
        public string? Strength { get; set; }
        public bool? RequiredPrescription { get; set; }

        public int TotalQuantityOnHand { get; set; }
        public List<StockBatchDto>? StockBatches { get; set; }
    }

    public class StockBatchDto
    {
        public long StockId { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime ExpireDate { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string? LotNumber { get; set; }
        public string? SupplierId { get; set; }
        public string? SupplierName { get; set; }
    }
}