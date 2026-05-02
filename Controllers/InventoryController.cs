using Microsoft.AspNetCore.Mvc;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
    private readonly ThilankaPharmacyDbContext _context;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ThilankaPharmacyDbContext context, ILogger<InventoryController> logger)
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

        // 2. Fetch Real-Time Stock Batches 
        var stockBatches = await _context.Stocks
            .Where(s => s.ProductSku == sku && s.QuantityOnHand > 0)
            .Select(s => new StockBatchDto
            {
                StockId = s.StockId,
                QuantityOnHand = s.QuantityOnHand,
                ExpireDate = s.ExpireDate.ToDateTime(TimeOnly.MinValue),
                CostPrice = s.CostPrice,
                SellingPrice = s.SellingPrice,
                LotNumber = s.LotNumber
            })
            .ToListAsync();
        _logger.LogInformation("Stock batches retrieved: {@StockBatches}", stockBatches);

        // 3. Aggregate and attach batches
        itemData.TotalQuantityOnHand = stockBatches.Sum(b => b.QuantityOnHand);
        itemData.StockBatches = stockBatches;
        _logger.LogInformation("Final item detail data with stock batches: {@FinalItemData}", itemData);

        return itemData;
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

            result.Add(new ProductListItemDto
            {
                ProductSku = p.ProductSku,
                Name = name,
                Stock = stock,
                UnitPrice = price,
                LowStockThreshold = lowThreshold,
                ProductType = p.ProductType
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

            return new ProductListItemDto
            {
                ProductSku = p.ProductSku,
                Name = name,
                Stock = stock,
                UnitPrice = price,
                LowStockThreshold = lowThreshold,
                ProductType = p.ProductType
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