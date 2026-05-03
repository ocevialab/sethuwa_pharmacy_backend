using Microsoft.AspNetCore.Mvc;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
    private readonly SethuwaPharmacyDbContext _context;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(SethuwaPharmacyDbContext context, ILogger<InventoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Inventory/ItemDetails/MED-10001
    // FR-INV-001: Dedicated screen for a single item showing master data and real-time stock
    [RequirePermission("inventory:view_details")]
    [HttpGet("ItemDetails/{sku}")]
    public async Task<ActionResult<ItemDetailDto>> GetMedicineDetails(string sku)
    {
        _logger.LogInformation("Fetching item details for SKU: {SKU}", sku);

        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductSku == sku);
        _logger.LogInformation("Product found: {@Product}", product);

        if (product == null || product.ProductType != "Medicine")
        {
            _logger.LogWarning("Medicine not found or invalid type for SKU: {SKU}", sku);
            return NotFound($"Medicine with SKU {sku} not found.");
        }

        // 1. Fetch Master Data
        var medicine = await _context.Medicines.FindAsync(product.MedicineId);
        _logger.LogInformation("Medicine found: {@Medicine}", medicine);

        if (medicine == null)
        {
            _logger.LogWarning("Medicine details not found for MedicineId: {MedicineId}", product.MedicineId);
            return NotFound($"Medicine with ID {product.MedicineId} not found.");
        }

        var itemData = new ItemDetailDto
        {
            ProductSku = product.ProductSku,
            ProductType = product.ProductType,
            Name = medicine.Name,
            BrandName = medicine.BrandName,
            LowStockThreshold = medicine.LowStockThreshold ?? 0,
            GenericName = medicine.GenericName,
            Strength = medicine.Strength,
            RequiredPrescription = medicine.RequiredPrescription,
        };
        _logger.LogInformation("Item detail data prepared: {@ItemData}", itemData);

        // 2. Fetch Real-Time Stock Batches (include supplier info) — FEFO: earliest expiry, then oldest Stock_ID
        var stockBatches = await _context.Stocks
            .Where(s => s.ProductSku == sku && s.QuantityOnHand > 0)
            .OrderBy(s => s.ExpireDate)
            .ThenBy(s => s.StockId)
            .Select(s => new StockBatchDto
            {
                StockId = s.StockId,
                QuantityOnHand = s.QuantityOnHand,
                ExpireDate = s.ExpireDate.ToDateTime(TimeOnly.MinValue),
                CostPrice = s.CostPrice,
                SellingPrice = s.SellingPrice,
                LotNumber = s.LotNumber,
                SupplierId = s.SupplierId,
                SupplierName = s.Supplier != null ? s.Supplier.SupplierName : null
            })
            .ToListAsync();
        _logger.LogInformation("Stock batches retrieved: {@StockBatches}", stockBatches);

        // 3. Aggregate and attach batches
        itemData.TotalQuantityOnHand = stockBatches.Sum(b => b.QuantityOnHand);
        itemData.StockBatches = stockBatches;
        _logger.LogInformation("Final item detail data with stock batches: {@FinalItemData}", itemData);

        return itemData;
    }


    // GET: api/Inventory/batches/MED-10001
    // Returns all available stock batches for a product with supplier info — used by POS batch picker
    [RequirePermission("inventory:view_details")]
    [HttpGet("batches/{sku}")]
    public async Task<IActionResult> GetStockBatchesForProduct(string sku)
    {
        _logger.LogInformation("Fetching available stock batches for SKU: {SKU}", sku);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductSku == sku && !p.IsDeleted);

        if (product == null)
            return NotFound($"Product with SKU {sku} not found.");

        var batches = await _context.Stocks
            .Where(s => s.ProductSku == sku && s.QuantityOnHand > 0)
            .OrderBy(s => s.ExpireDate)
            .ThenBy(s => s.StockId)
            .Select(s => new StockBatchDto
            {
                StockId = s.StockId,
                QuantityOnHand = s.QuantityOnHand,
                ExpireDate = s.ExpireDate.ToDateTime(TimeOnly.MinValue),
                CostPrice = s.CostPrice,
                SellingPrice = s.SellingPrice,
                LotNumber = s.LotNumber,
                SupplierId = s.SupplierId,
                SupplierName = s.Supplier != null ? s.Supplier.SupplierName : null
            })
            .ToListAsync();

        return Ok(batches);
    }

    /// <summary>
    /// POST: api/Inventory/batches-for-sale — in-stock batches per SKU for POS (one round-trip).
    /// Ordering per SKU: in-stock rows only; FEFO — earliest Expire_Date, then Stock_ID (older batch first).
    /// </summary>
    [RequirePermission("inventory:view_details")]
    [HttpPost("batches-for-sale")]
    public async Task<IActionResult> GetBatchesForSale([FromBody] List<string>? productSkus)
    {
        if (productSkus == null || productSkus.Count == 0)
            return Ok(new Dictionary<string, List<StockBatchDto>>());

        var distinct = productSkus
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        if (distinct.Count == 0)
            return Ok(new Dictionary<string, List<StockBatchDto>>());

        var rows = await _context.Stocks
            .Where(s => distinct.Contains(s.ProductSku) && s.QuantityOnHand > 0)
            .Select(s => new
            {
                s.ProductSku,
                s.StockId,
                s.QuantityOnHand,
                s.ExpireDate,
                s.CostPrice,
                s.SellingPrice,
                s.LotNumber,
                s.SupplierId,
                SupplierName = s.Supplier != null ? s.Supplier.SupplierName : null
            })
            .ToListAsync();

        var result = new Dictionary<string, List<StockBatchDto>>(StringComparer.Ordinal);

        foreach (var sku in distinct)
        {
            var list = rows
                .Where(x => x.ProductSku == sku)
                .OrderBy(x => x.ExpireDate)
                .ThenBy(x => x.StockId)
                .Select(x => new StockBatchDto
                {
                    StockId = x.StockId,
                    QuantityOnHand = x.QuantityOnHand,
                    ExpireDate = x.ExpireDate.ToDateTime(TimeOnly.MinValue),
                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    LotNumber = x.LotNumber,
                    SupplierId = x.SupplierId,
                    SupplierName = x.SupplierName
                })
                .ToList();

            if (list.Count > 0)
                result[sku] = list;
        }

        return Ok(result);
    }

    [RequirePermission("inventory:view_list")]
    [HttpGet("list")]
    public async Task<IActionResult> GetProductList([FromQuery] string? q)
    {
        q = q?.Trim().ToLower();

        // STEP 1 — Load products with navigation properties
        var products = await _context.Products
            .Include(p => p.Medicine)
            .Include(p => p.Glossary)
            .Where(p => !p.IsDeleted &&
                   (q == null ||
                    (p.ProductType == "Medicine" && p.Medicine!.Name.ToLower().Contains(q)) ||
                    (p.ProductType == "Glossary" && p.Glossary!.Name.ToLower().Contains(q))
                   ))
            .ToListAsync();

        var productSkuList = products.Select(p => p.ProductSku).ToList();

        var supplierRows = await _context.Stocks
            .Where(s => productSkuList.Contains(s.ProductSku) && s.QuantityOnHand > 0)
            .Select(s => new
            {
                s.ProductSku,
                SupplierName = s.Supplier != null ? s.Supplier.SupplierName : null
            })
            .ToListAsync();

        var supplierSummaryBySku = supplierRows
            .GroupBy(x => x.ProductSku)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g
                    .Select(x => x.SupplierName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)));

        var result = new List<ProductListItemDto>();

        // STEP 2 — Build full model
        foreach (var p in products)
        {
            string name = p.ProductType == "Medicine"
                ? p.Medicine!.Name
                : p.Glossary!.Name;

            int stock = await _context.Stocks
                .Where(s => s.ProductSku == p.ProductSku)
                .SumAsync(s => s.QuantityOnHand);

            var latestBatch = await _context.Stocks
                .Where(s => s.ProductSku == p.ProductSku)
                .OrderByDescending(s => s.StockId)
                .FirstOrDefaultAsync();

            decimal price = latestBatch?.SellingPrice ?? 0;

            int lowThreshold = p.ProductType == "Medicine"
                ? p.Medicine!.LowStockThreshold ?? 0
                : p.Glossary!.LowStockThreshold;

            supplierSummaryBySku.TryGetValue(p.ProductSku, out var supplierSummary);

            result.Add(new ProductListItemDto
            {
                ProductSku = p.ProductSku,
                Name = name,
                Stock = stock,
                UnitPrice = price,
                LowStockThreshold = lowThreshold,
                ProductType = p.ProductType,
                SupplierSummary = string.IsNullOrWhiteSpace(supplierSummary) ? null : supplierSummary
            });
        }

        return Ok(result);
    }

    // GET: api/Inventory/all?page=1&pageSize=10&q=search
    [RequirePermission("inventory:view_all")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllInventory(int page = 1, int pageSize = 10, [FromQuery] string? q = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation("Fetching inventory page {Page}, size {PageSize}, search: {Search}", page, pageSize, q);

        q = q?.Trim().ToLower();

        // Base query with includes and search filter
        var query = _context.Products
            .Include(p => p.Medicine)
            .Include(p => p.Glossary)
            .Where(p => !p.IsDeleted &&
                   (q == null ||
                    (p.ProductType == "Medicine" && p.Medicine != null && p.Medicine.Name.ToLower().Contains(q)) ||
                    (p.ProductType == "Glossary" && p.Glossary != null && p.Glossary.Name.ToLower().Contains(q)) ||
                    p.ProductSku.ToLower().Contains(q)
                   ))
            .AsNoTracking()
            .OrderBy(p => p.ProductSku)
            .AsQueryable();

        // Count total items
        var totalItems = await query.CountAsync();

        // Pagination calculation
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Fetch requested page
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load all stock data in bulk for the paginated products
        var productSkus = products.Select(p => p.ProductSku).ToList();

        // Get total stock quantity for each product
        var stockQuantities = await _context.Stocks
            .Where(s => productSkus.Contains(s.ProductSku))
            .GroupBy(s => s.ProductSku)
            .Select(g => new
            {
                ProductSku = g.Key,
                TotalQuantity = g.Sum(s => s.QuantityOnHand)
            })
            .ToDictionaryAsync(x => x.ProductSku, x => x.TotalQuantity);

        // Get latest selling price for each product (from most recent stock entry)
        var latestPrices = await _context.Stocks
            .Where(s => productSkus.Contains(s.ProductSku))
            .GroupBy(s => s.ProductSku)
            .Select(g => new
            {
                ProductSku = g.Key,
                SellingPrice = g.OrderByDescending(s => s.StockId).First().SellingPrice
            })
            .ToDictionaryAsync(x => x.ProductSku, x => x.SellingPrice);

        var supplierRows = await _context.Stocks
            .Where(s => productSkus.Contains(s.ProductSku) && s.QuantityOnHand > 0)
            .Select(s => new
            {
                s.ProductSku,
                SupplierName = s.Supplier != null ? s.Supplier.SupplierName : null
            })
            .ToListAsync();

        var supplierSummaryBySku = supplierRows
            .GroupBy(x => x.ProductSku)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g
                    .Select(x => x.SupplierName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)));

        // Map to DTOs
        var result = products.Select(p =>
        {
            string name = p.ProductType == "Medicine"
                ? (p.Medicine?.Name ?? "Unknown")
                : (p.Glossary?.Name ?? "Unknown");

            stockQuantities.TryGetValue(p.ProductSku, out int stock);
            latestPrices.TryGetValue(p.ProductSku, out decimal price);

            int lowThreshold = p.ProductType == "Medicine"
                ? (p.Medicine?.LowStockThreshold ?? 0)
                : (p.Glossary?.LowStockThreshold ?? 0);

            supplierSummaryBySku.TryGetValue(p.ProductSku, out var supplierSummary);

            return new ProductListItemDto
            {
                ProductSku = p.ProductSku,
                Name = name,
                Stock = stock,
                UnitPrice = price,
                LowStockThreshold = lowThreshold,
                ProductType = p.ProductType,
                SupplierSummary = string.IsNullOrWhiteSpace(supplierSummary) ? null : supplierSummary
            };
        }).ToList();

        // Return pagination envelope
        return Ok(new
        {
            currentPage = page,
            pageSize,
            totalItems,
            totalPages,
            data = result
        });
    }

}