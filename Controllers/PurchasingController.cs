using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.Authorization;


[Route("api/[controller]")]
[ApiController]
public class PurchasingController : ControllerBase
{
    private readonly SethsuwaPharmacyDbContext _context;
    private readonly ILogger<PurchasingController> _logger;

    public PurchasingController(SethsuwaPharmacyDbContext context, ILogger<PurchasingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // POST: api/Purchasing

    [RequirePermission("purchasing:create")]
    [HttpPost]
    public async Task<IActionResult> CreatePurchase(PurchaseCreationDto dto)
    {
        // Check model validation
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for CreatePurchase request. Errors: {@ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        if (dto == null || dto.Items == null || dto.Items.Count == 0)
        {
            _logger.LogWarning("Invalid request payload: dto is null or items are empty");
            return BadRequest("Invalid request payload or empty purchase items.");
        }

        _logger.LogInformation("Received CreatePurchase request with InvoiceNumber: {InvoiceNumber}", dto.InvoiceNumber);

        _logger.LogInformation("Validating Invoice Number uniqueness...");

        if (!string.Equals(dto.InvoiceNumber, "N/A", StringComparison.OrdinalIgnoreCase))
        {
            var invoiceExists = await _context.Purchases
                .AnyAsync(p => p.InvoiceNumber == dto.InvoiceNumber);

            if (invoiceExists)
                return Conflict($"A purchase with Invoice Number '{dto.InvoiceNumber}' already exists.");
        }

        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.SupplierId == dto.SupplierId);

        if (!supplierExists)
            return BadRequest($"Supplier with ID '{dto.SupplierId}' does not exist.");

        // Validate all product SKUs upfront (before creating purchase)
        _logger.LogInformation("Validating product SKUs...");
        var productSkus = dto.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.ProductSKU))
            .Select(item => item.ProductSKU!)
            .Distinct()
            .ToList();

        if (productSkus.Count == 0)
            return BadRequest("No valid product SKUs provided in purchase items.");

        var validProducts = await _context.Products
            .Where(p => productSkus.Contains(p.ProductSku) && !p.IsDeleted)
            .Select(p => p.ProductSku)
            .ToListAsync();

        var invalidSkus = productSkus.Except(validProducts).ToList();

        if (invalidSkus.Any())
        {
            _logger.LogWarning("Invalid product SKUs found: {InvalidSkus}", string.Join(", ", invalidSkus));
            return BadRequest($"Invalid product SKU(s): {string.Join(", ", invalidSkus)}. Please ensure all products exist and are not deleted.");
        }

        _logger.LogInformation("Generating custom Purchase ID...");

        // Generate custom PurchaseId
        var newPurchaseId = await _context.Purchases.GenerateNextSequentialId(
            "PUR",           // Prefix
            "PurchaseId",    // Property on Purchase entity
            _logger          // Optional
        );



        _logger.LogInformation("Creating new Purchase record with PurchaseId: {PurchaseId}", newPurchaseId);

        var purchase = new Purchase
        {
            PurchaseId = newPurchaseId, // <-- Custom ID
            InvoiceNumber = dto.InvoiceNumber?.ToUpper().Replace(" ", "") ?? string.Empty,
            InvoiceDate = DateOnly.FromDateTime(dto.InvoiceDate),
            PaymentStatus = dto.PaymentStatus ?? string.Empty,
            PaymentDueDate = DateOnly.FromDateTime(dto.PaymentDueDate),
            TotalAmount = dto.TotalAmount,
            SupplierId = dto.SupplierId ?? string.Empty
        };

        _context.Purchases.Add(purchase);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547)
            {
                _logger.LogError("Foreign key violation while creating purchase: {Message}", ex.Message);
                return BadRequest("Cannot create purchase: invalid Supplier ID or related entity does not exist.");
            }
            throw;
        }

        // Load all products in bulk for faster access
        var allProducts = await _context.Products
            .Where(p => productSkus.Contains(p.ProductSku) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.ProductSku);

        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductSKU) || !allProducts.ContainsKey(item.ProductSKU))
            {
                _logger.LogError("Product SKU validation failed for: {ProductSKU}", item.ProductSKU);
                return BadRequest($"Invalid product SKU: {item.ProductSKU}");
            }

            var purchaseItem = new PurchaseItem
            {
                PurchaseId = purchase.PurchaseId,
                ProductSku = item.ProductSKU ?? string.Empty,
                CostPrice = item.CostPrice,
                SellingPrice = item.SellingPrice,
                Quantity = item.Quantity,
                ExpireDate = DateOnly.FromDateTime(item.ExpireDate)
            };

            _context.PurchaseItems.Add(purchaseItem);

            var stock = new Stock
            {
                ProductSku = item.ProductSKU ?? string.Empty,
                QuantityOnHand = item.Quantity,
                CostPrice = item.CostPrice,
                SellingPrice = item.SellingPrice,
                ExpireDate = DateOnly.FromDateTime(item.ExpireDate),
                LotNumber = Guid.NewGuid().ToString().Substring(0, 8),
                SupplierId = purchase.SupplierId,
                PurchaseItem = purchaseItem // links the lot back to this purchase item so edits can safely reconcile stock later
            };
            _context.Stocks.Add(stock);
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Purchase created successfully.", purchase.PurchaseId });
    }

    /*  EDIT PURCHASE — updates header + line items, reconciling stock lots.
     *  Safety rule: a line item's quantity can never be reduced (or removed, or have its
     *  product changed) below however many units have already been sold from its linked
     *  stock lot. Line items created before Stock->PurchaseItem tracking existed (no link)
     *  cannot be edited at all. Adding brand-new line items is always allowed.
     */
    [RequirePermission("purchasing:edit_purchase")]
    [HttpPut("{purchaseId}")]
    public async Task<IActionResult> EditPurchase(string purchaseId, EditPurchaseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var purchase = await _context.Purchases
            .Include(p => p.PurchaseItems)
            .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);

        if (purchase == null)
            return NotFound($"Purchase with ID {purchaseId} not found.");

        var normalizedInvoice = dto.InvoiceNumber.ToUpper().Replace(" ", "");
        if (!string.Equals(normalizedInvoice, "N/A", StringComparison.OrdinalIgnoreCase))
        {
            var invoiceExists = await _context.Purchases
                .AnyAsync(p => p.InvoiceNumber == normalizedInvoice && p.PurchaseId != purchaseId);

            if (invoiceExists)
                return Conflict($"A purchase with Invoice Number '{normalizedInvoice}' already exists.");
        }

        var supplierExists = await _context.Suppliers.AnyAsync(s => s.SupplierId == dto.SupplierId);
        if (!supplierExists)
            return BadRequest($"Supplier with ID '{dto.SupplierId}' does not exist.");

        var productSkus = dto.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ProductSKU))
            .Select(i => i.ProductSKU)
            .Distinct()
            .ToList();

        if (productSkus.Count == 0)
            return BadRequest("No valid product SKUs provided in purchase items.");

        var validProducts = await _context.Products
            .Where(p => productSkus.Contains(p.ProductSku) && !p.IsDeleted)
            .Select(p => p.ProductSku)
            .ToListAsync();

        var invalidSkus = productSkus.Except(validProducts).ToList();
        if (invalidSkus.Any())
            return BadRequest($"Invalid product SKU(s): {string.Join(", ", invalidSkus)}. Please ensure all products exist and are not deleted.");

        // Load the stock lot linked to each of this purchase's existing items (one lot per item today)
        var existingItemIds = purchase.PurchaseItems.Select(pi => pi.PurchaseItemId).ToList();
        var linkedStocks = await _context.Stocks
            .Where(s => s.PurchaseItemId != null && existingItemIds.Contains(s.PurchaseItemId.Value))
            .ToListAsync();
        var stockByItemId = linkedStocks
            .GroupBy(s => s.PurchaseItemId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var incomingIds = dto.Items
            .Where(i => i.PurchaseItemId.HasValue)
            .Select(i => i.PurchaseItemId!.Value)
            .ToHashSet();

        // ---- VALIDATION PASS (no mutation yet, so a rejected edit leaves nothing half-applied) ----
        var errors = new List<string>();

        foreach (var existingItem in purchase.PurchaseItems)
        {
            if (incomingIds.Contains(existingItem.PurchaseItemId))
                continue; // still present, checked below

            stockByItemId.TryGetValue(existingItem.PurchaseItemId, out var linkedStock);
            if (linkedStock == null)
            {
                errors.Add($"Cannot remove item '{existingItem.ProductSku}' — it predates stock tracking and cannot be safely edited.");
            }
            else if (linkedStock.QuantityOnHand != existingItem.Quantity)
            {
                var sold = existingItem.Quantity - linkedStock.QuantityOnHand;
                errors.Add($"Cannot remove item '{existingItem.ProductSku}' — {sold} unit(s) already sold from this batch.");
            }
        }

        foreach (var incoming in dto.Items.Where(i => i.PurchaseItemId.HasValue))
        {
            var existingItem = purchase.PurchaseItems.FirstOrDefault(pi => pi.PurchaseItemId == incoming.PurchaseItemId!.Value);
            if (existingItem == null)
            {
                errors.Add($"Purchase item {incoming.PurchaseItemId} does not belong to this purchase.");
                continue;
            }

            stockByItemId.TryGetValue(existingItem.PurchaseItemId, out var linkedStock);
            if (linkedStock == null)
            {
                errors.Add($"Cannot edit item '{existingItem.ProductSku}' — it predates stock tracking and cannot be safely edited.");
                continue;
            }

            var quantitySold = existingItem.Quantity - linkedStock.QuantityOnHand;
            var productChanged = !string.Equals(existingItem.ProductSku, incoming.ProductSKU, StringComparison.OrdinalIgnoreCase);

            if (productChanged)
            {
                // Changing the product is treated as remove-old + add-new, so it's only safe if nothing has been sold yet
                if (quantitySold > 0)
                    errors.Add($"Cannot change product for item '{existingItem.ProductSku}' — {quantitySold} unit(s) already sold from this batch.");
            }
            else if (incoming.Quantity < quantitySold)
            {
                errors.Add($"Cannot reduce quantity for '{existingItem.ProductSku}' below {quantitySold} unit(s) already sold (requested {incoming.Quantity}).");
            }
        }

        if (errors.Any())
            return BadRequest(new { Message = "Cannot apply this edit: " + string.Join(" ", errors) });

        // ---- MUTATION PASS ----
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1) Remove items dropped from the list (already verified above that nothing was sold from them)
            foreach (var existingItem in purchase.PurchaseItems.Where(pi => !incomingIds.Contains(pi.PurchaseItemId)).ToList())
            {
                if (stockByItemId.TryGetValue(existingItem.PurchaseItemId, out var linkedStock))
                    _context.Stocks.Remove(linkedStock);

                _context.PurchaseItems.Remove(existingItem);
            }

            // 2) Update existing items (or swap them out if the product changed)
            foreach (var incoming in dto.Items.Where(i => i.PurchaseItemId.HasValue))
            {
                var existingItem = purchase.PurchaseItems.First(pi => pi.PurchaseItemId == incoming.PurchaseItemId!.Value);
                stockByItemId.TryGetValue(existingItem.PurchaseItemId, out var linkedStock);
                var productChanged = !string.Equals(existingItem.ProductSku, incoming.ProductSKU, StringComparison.OrdinalIgnoreCase);

                if (productChanged)
                {
                    if (linkedStock != null)
                        _context.Stocks.Remove(linkedStock);
                    _context.PurchaseItems.Remove(existingItem);

                    var newItem = new PurchaseItem
                    {
                        PurchaseId = purchase.PurchaseId,
                        ProductSku = incoming.ProductSKU,
                        CostPrice = incoming.CostPrice,
                        SellingPrice = incoming.SellingPrice,
                        Quantity = incoming.Quantity,
                        ExpireDate = DateOnly.FromDateTime(incoming.ExpireDate)
                    };
                    _context.PurchaseItems.Add(newItem);

                    _context.Stocks.Add(new Stock
                    {
                        ProductSku = incoming.ProductSKU,
                        QuantityOnHand = incoming.Quantity,
                        CostPrice = incoming.CostPrice,
                        SellingPrice = incoming.SellingPrice,
                        ExpireDate = DateOnly.FromDateTime(incoming.ExpireDate),
                        LotNumber = Guid.NewGuid().ToString().Substring(0, 8),
                        SupplierId = dto.SupplierId,
                        PurchaseItem = newItem
                    });
                }
                else
                {
                    var delta = incoming.Quantity - existingItem.Quantity;

                    existingItem.Quantity = incoming.Quantity;
                    existingItem.CostPrice = incoming.CostPrice;
                    existingItem.SellingPrice = incoming.SellingPrice;
                    existingItem.ExpireDate = DateOnly.FromDateTime(incoming.ExpireDate);

                    if (linkedStock != null)
                    {
                        linkedStock.QuantityOnHand += delta;
                        linkedStock.CostPrice = incoming.CostPrice;
                        linkedStock.SellingPrice = incoming.SellingPrice;
                        linkedStock.ExpireDate = DateOnly.FromDateTime(incoming.ExpireDate);
                        linkedStock.SupplierId = dto.SupplierId;
                    }
                }
            }

            // 3) Add brand-new items
            foreach (var incoming in dto.Items.Where(i => !i.PurchaseItemId.HasValue))
            {
                var newItem = new PurchaseItem
                {
                    PurchaseId = purchase.PurchaseId,
                    ProductSku = incoming.ProductSKU,
                    CostPrice = incoming.CostPrice,
                    SellingPrice = incoming.SellingPrice,
                    Quantity = incoming.Quantity,
                    ExpireDate = DateOnly.FromDateTime(incoming.ExpireDate)
                };
                _context.PurchaseItems.Add(newItem);

                _context.Stocks.Add(new Stock
                {
                    ProductSku = incoming.ProductSKU,
                    QuantityOnHand = incoming.Quantity,
                    CostPrice = incoming.CostPrice,
                    SellingPrice = incoming.SellingPrice,
                    ExpireDate = DateOnly.FromDateTime(incoming.ExpireDate),
                    LotNumber = Guid.NewGuid().ToString().Substring(0, 8),
                    SupplierId = dto.SupplierId,
                    PurchaseItem = newItem
                });
            }

            // 4) Update purchase header (payment status/method are intentionally untouched here —
            // use the dedicated payment-status endpoint for those)
            purchase.InvoiceNumber = normalizedInvoice;
            purchase.InvoiceDate = DateOnly.FromDateTime(dto.InvoiceDate);
            purchase.PaymentDueDate = dto.PaymentDueDate.HasValue ? DateOnly.FromDateTime(dto.PaymentDueDate.Value) : null;
            purchase.SupplierId = dto.SupplierId;
            purchase.TotalAmount = dto.TotalAmount;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                Message = "Purchase updated successfully.",
                purchase.PurchaseId
            });
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();

            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547)
            {
                _logger.LogError("Foreign key violation while editing purchase: {Message}", ex.Message);
                return BadRequest("Cannot update purchase: invalid Supplier ID or related entity does not exist.");
            }
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }


    /**update purchase payment status
    1. Only OWNER and ADMIN can update payment status
    2. Validate new status.
    3. If purchase not found, return 404.
    4. If Current Payment status is Complete, no further updates allowed.
    5. New update comes under complete status, payment completed date is set to current date.
    6. Only payment status "Pending" can update to "Overdue" and vice versa.
    7. 

    **/
    [RequirePermission("purchasing:update_payment_status")]
    [HttpPut("{purchaseId}/payment-status")]
    public async Task<IActionResult> UpdatePaymentStatus(string purchaseId, [FromBody] PaymentStatusUpdateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.PaymentStatus))
            return BadRequest("PaymentStatus is required.");

        var validStatuses = new[] { "Complete", "Pending", "Overdue" };
        if (!validStatuses.Contains(dto.PaymentStatus))
            return BadRequest("Invalid payment status. Valid statuses are: Complete, Pending, Overdue.");

        try
        {
            var purchase = await _context.Purchases
                .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);

            if (purchase == null)
                return NotFound($"Purchase with ID {purchaseId} not found.");

            // Prevent changes after completion
            if (purchase.PaymentStatus == "Complete")
                return BadRequest("Cannot update payment status of a completed purchase.");

            var newStatus = dto.PaymentStatus;

            // Status transition rules
            if (newStatus == "Complete")
            {
                purchase.PaymentStatus = "Complete";
                // Make sure this column exists and is nullable in the database
                purchase.PaymentCompletedAt1 = DateTime.UtcNow;
            }
            else if ((purchase.PaymentStatus == "Pending" && newStatus == "Overdue") ||
                     (purchase.PaymentStatus == "Overdue" && newStatus == "Pending"))
            {
                purchase.PaymentStatus = newStatus;
            }
            else
            {
                return BadRequest("Invalid status transition.");
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Payment status updated successfully.",
                purchase.PurchaseId,
                purchase.PaymentStatus
            });
        }
        catch (DbUpdateException dbEx)
        {
            // Database-specific errors
            return StatusCode(500, $"Database update error: {dbEx.InnerException?.Message ?? dbEx.Message}");
        }
        catch (Exception ex)
        {
            // Other unhandled errors
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }


    // View All Purchases - OWNER and ADMIN only
    [RequirePermission("purchasing:view_all")]
    [HttpGet]
    public async Task<IActionResult> GetAllPurchases(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation($"Fetching purchases page {page}, size {pageSize}...");

        // Base query with includes
        var query = _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems)
            .AsNoTracking()
            .OrderByDescending(p => p.InvoiceDate)
            .ThenByDescending(p => p.PurchaseId)
            .AsQueryable();

        // Count total items
        var totalItems = await query.CountAsync();

        // Pagination calculation
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Fetch requested page
        var purchases = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load all product SKUs and their names efficiently
        var allProductSkus = purchases
            .SelectMany(p => p.PurchaseItems)
            .Select(pi => pi.ProductSku)
            .Distinct()
            .ToList();

        var allProducts = await _context.Products
            .Where(pr => allProductSkus.Contains(pr.ProductSku))
            .AsNoTracking()
            .ToListAsync();

        var medicineIds = allProducts
            .Where(p => p.MedicineId != null)
            .Select(p => p.MedicineId!)
            .Distinct()
            .ToList();

        var glossaryIds = allProducts
            .Where(p => p.GlossaryId != null)
            .Select(p => p.GlossaryId!)
            .Distinct()
            .ToList();

        var medicines = await _context.Medicines
            .Where(m => medicineIds.Contains(m.MedicineId))
            .AsNoTracking()
            .ToListAsync();

        var glossaries = await _context.Glossaries
            .Where(g => glossaryIds.Contains(g.GlossaryId))
            .AsNoTracking()
            .ToListAsync();

        var productLookup = allProducts.ToDictionary(p => p.ProductSku);
        var medicineLookup = medicines.ToDictionary(m => m.MedicineId);
        var glossaryLookup = glossaries.ToDictionary(g => g.GlossaryId);

        // Map to DTO
        var purchaseDTOs = purchases.Select(p => new PurchaseDTO
        {
            PurchaseId = p.PurchaseId,
            InvoiceNumber = p.InvoiceNumber,
            InvoiceDate = p.InvoiceDate,
            PaymentStatus = p.PaymentStatus,
            PaymentDueDate = p.PaymentDueDate,
            TotalAmount = p.TotalAmount,
            SupplierId = p.SupplierId,
            PurchaseItems = p.PurchaseItems?.Select(pi =>
            {
                string productName = "Unknown";

                if (productLookup.TryGetValue(pi.ProductSku, out var product))
                {
                    if (product.ProductType == "Medicine" && product.MedicineId != null)
                    {
                        if (medicineLookup.TryGetValue(product.MedicineId, out var medicine))
                        {
                            productName = medicine.Name;
                        }
                    }
                    else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                    {
                        if (glossaryLookup.TryGetValue(product.GlossaryId, out var glossary))
                        {
                            productName = glossary.Name;
                        }
                    }
                }

                return new PurchaseItemDto
                {
                    PurchaseItemId = pi.PurchaseItemId,
                    ProductSKU = pi.ProductSku,
                    ProductName = productName,
                    Quantity = pi.Quantity,
                    CostPrice = pi.CostPrice,
                    SellingPrice = pi.SellingPrice,
                    ExpireDate = pi.ExpireDate.ToDateTime(new TimeOnly(0, 0))
                };
            }).ToList() ?? new List<PurchaseItemDto>()
        }).ToList();

        // Return pagination envelope
        return Ok(new
        {
            currentPage = page,
            pageSize,
            totalItems,
            totalPages,
            data = purchaseDTOs
        });
    }

    // GET: api/Purchasing/by-supplier/{supplierId}?page=1&pageSize=10
    [RequirePermission("purchasing:view_all")]
    [HttpGet("by-supplier/{supplierId}")]
    public async Task<IActionResult> GetPurchasesBySupplierId(string supplierId, int page = 1, int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            _logger.LogWarning("GetPurchasesBySupplierId called with empty supplierId");
            return BadRequest("Supplier ID is required.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation("Fetching purchases for supplier {SupplierId}, page {Page}, size {PageSize}...", supplierId, page, pageSize);

        // Validate supplier exists
        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.SupplierId == supplierId);

        if (!supplierExists)
        {
            _logger.LogWarning("Supplier not found: {SupplierId}", supplierId);
            return NotFound($"Supplier with ID '{supplierId}' not found.");
        }

        // Base query with includes, filtered by supplierId
        var query = _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems)
            .Where(p => p.SupplierId == supplierId)
            .AsNoTracking()
            .OrderByDescending(p => p.InvoiceDate)
            .ThenByDescending(p => p.PurchaseId)
            .AsQueryable();

        // Count total items
        var totalItems = await query.CountAsync();

        // Pagination calculation
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Fetch requested page
        var purchases = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load all product SKUs and their names efficiently
        var allProductSkus = purchases
            .SelectMany(p => p.PurchaseItems)
            .Select(pi => pi.ProductSku)
            .Distinct()
            .ToList();

        var allProducts = await _context.Products
            .Where(pr => allProductSkus.Contains(pr.ProductSku))
            .AsNoTracking()
            .ToListAsync();

        var medicineIds = allProducts
            .Where(p => p.MedicineId != null)
            .Select(p => p.MedicineId!)
            .Distinct()
            .ToList();

        var glossaryIds = allProducts
            .Where(p => p.GlossaryId != null)
            .Select(p => p.GlossaryId!)
            .Distinct()
            .ToList();

        var medicines = await _context.Medicines
            .Where(m => medicineIds.Contains(m.MedicineId))
            .AsNoTracking()
            .ToListAsync();

        var glossaries = await _context.Glossaries
            .Where(g => glossaryIds.Contains(g.GlossaryId))
            .AsNoTracking()
            .ToListAsync();

        var productLookup = allProducts.ToDictionary(p => p.ProductSku);
        var medicineLookup = medicines.ToDictionary(m => m.MedicineId);
        var glossaryLookup = glossaries.ToDictionary(g => g.GlossaryId);

        // Map to DTO
        var purchaseDTOs = purchases.Select(p => new PurchaseDTO
        {
            PurchaseId = p.PurchaseId,
            InvoiceNumber = p.InvoiceNumber,
            InvoiceDate = p.InvoiceDate,
            PaymentStatus = p.PaymentStatus,
            PaymentDueDate = p.PaymentDueDate,
            TotalAmount = p.TotalAmount,
            SupplierId = p.SupplierId,
            PurchaseItems = p.PurchaseItems?.Select(pi =>
            {
                string productName = "Unknown";

                if (productLookup.TryGetValue(pi.ProductSku, out var product))
                {
                    if (product.ProductType == "Medicine" && product.MedicineId != null)
                    {
                        if (medicineLookup.TryGetValue(product.MedicineId, out var medicine))
                        {
                            productName = medicine.Name;
                        }
                    }
                    else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                    {
                        if (glossaryLookup.TryGetValue(product.GlossaryId, out var glossary))
                        {
                            productName = glossary.Name;
                        }
                    }
                }

                return new PurchaseItemDto
                {
                    PurchaseItemId = pi.PurchaseItemId,
                    ProductSKU = pi.ProductSku,
                    ProductName = productName,
                    Quantity = pi.Quantity,
                    CostPrice = pi.CostPrice,
                    SellingPrice = pi.SellingPrice,
                    ExpireDate = pi.ExpireDate.ToDateTime(new TimeOnly(0, 0))
                };
            }).ToList() ?? new List<PurchaseItemDto>()
        }).ToList();

        // Return pagination envelope
        return Ok(new
        {
            currentPage = page,
            pageSize,
            totalItems,
            totalPages,
            supplierId,
            data = purchaseDTOs
        });
    }

    // Specific routes must come before generic {purchaseId} route to avoid ambiguity
    [RequirePermission("purchasing:view_summary")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetPurchasingSummary(int month, int year)
    {
        // Calculate month range
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        // ---------------------------
        // 1️⃣ Pending Payments
        // ---------------------------
        var pending = await _context.Purchases
            .Where(p => p.PaymentStatus != "Complete")
            .ToListAsync();

        var pendingDto = new PendingPaymentsDto
        {
            TotalAmount = pending.Sum(p => p.TotalAmount),
            InvoiceCount = pending.Count
        };

        // ---------------------------
        // 2️⃣ Purchases This Month
        // ---------------------------
        var monthPurchases = await _context.Purchases
            .Where(p => p.InvoiceDate >= start && p.InvoiceDate < end)
            .ToListAsync();

        var monthPurchasesDto = new MonthlyPurchasesDto
        {
            TotalAmount = monthPurchases.Sum(p => p.TotalAmount),
            InvoiceCount = monthPurchases.Count
        };

        // ---------------------------
        // 3️⃣ Items Received This Month (from PurchaseItems)
        // ---------------------------
        var items = await (
            from pi in _context.PurchaseItems
            join p in _context.Purchases on pi.PurchaseId equals p.PurchaseId
            where p.InvoiceDate >= start && p.InvoiceDate < end
            select pi
        ).ToListAsync();

        var itemsReceivedDto = new ItemsReceivedDto
        {
            TotalQuantity = items.Sum(i => i.Quantity),
            Batches = items.Count
        };

        // ---------------------------
        // 4️⃣ Active Suppliers
        // ---------------------------
        int activeSuppliers = await _context.Suppliers.CountAsync();

        // ---------------------------
        // Combine summary
        // ---------------------------
        var response = new PurchasingSummaryDto
        {
            PendingPayments = pendingDto,
            MonthlyPurchases = monthPurchasesDto,
            ItemsReceived = itemsReceivedDto,
            ActiveSuppliers = activeSuppliers
        };

        return Ok(response);
    }

    // GET: api/Purchasing/payment-summary?month=1&year=2024
    [RequirePermission("purchasing:view_summary")]
    [HttpGet("payment-summary")]
    public async Task<IActionResult> GetPaymentSummary([FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        // Default to current month/year if not provided
        if (!month.HasValue || !year.HasValue)
        {
            var now = DateTime.Now;
            month = now.Month;
            year = now.Year;
        }

        _logger.LogInformation("Fetching payment summary for month: {Month}, year: {Year}", month, year);

        // Calculate monthly completed payments date range
        var start = new DateOnly(year.Value, month.Value, 1);
        var end = start.AddMonths(1);

        // ---------------------------
        // 1️⃣ Monthly Completed Payments
        // ---------------------------
        var startDateTime = start.ToDateTime(new TimeOnly(0, 0));
        var endDateTime = end.ToDateTime(new TimeOnly(0, 0));

        var monthlyPayments = await _context.Purchases
            .Where(p => p.PaymentStatus == "Complete" &&
                       p.PaymentCompletedAt1.HasValue &&
                       p.PaymentCompletedAt1.Value.Date >= startDateTime &&
                       p.PaymentCompletedAt1.Value.Date < endDateTime)
            .ToListAsync();

        var monthlyPaymentsDto = new MonthlyPaymentsDto
        {
            TotalAmount = monthlyPayments.Sum(p => p.TotalAmount),
            PaymentCount = monthlyPayments.Count
        };

        // ---------------------------
        // 2️⃣ Pending Payments
        // ---------------------------
        var pendingPayments = await _context.Purchases
            .Where(p => p.PaymentStatus == "Pending")
            .ToListAsync();

        var pendingPaymentsDto = new PendingPaymentSummaryDto
        {
            TotalAmount = pendingPayments.Sum(p => p.TotalAmount),
            PaymentCount = pendingPayments.Count
        };

        // ---------------------------
        // 3️⃣ Overdue Payments
        // ---------------------------
        var overduePayments = await _context.Purchases
            .Where(p => p.PaymentStatus == "Overdue")
            .ToListAsync();

        var overduePaymentsDto = new OverduePaymentSummaryDto
        {
            TotalAmount = overduePayments.Sum(p => p.TotalAmount),
            PaymentCount = overduePayments.Count
        };

        // ---------------------------
        // Combine summary
        // ---------------------------
        var response = new PaymentSummaryDto
        {
            MonthlyPayments = monthlyPaymentsDto,
            PendingPayments = pendingPaymentsDto,
            OverduePayments = overduePaymentsDto
        };

        return Ok(response);
    }

    [RequirePermission("purchasing:search_products")]
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<ProductSearchResultDto>());

        q = q.Trim().ToLower();

        _logger.LogInformation($"Fast product search for purchasing: {q}");

        // STEP 1 — Search medicines by name (optimized with joins)
        var medicineProducts = await _context.Products
            .Where(p => !p.IsDeleted && p.ProductType == "Medicine" && p.MedicineId != null)
            .Join(_context.Medicines,
                p => p.MedicineId,
                m => m.MedicineId,
                (p, m) => new { Product = p, Medicine = m })
            .Where(x => x.Medicine.Name.ToLower().Contains(q) ||
                        x.Medicine.BrandName.ToLower().Contains(q))
            .Select(x => new
            {
                x.Product.ProductSku,
                x.Product.ProductType,
                ProductName = x.Medicine.Name,
                Strength = x.Medicine.Strength
            })
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        // STEP 2 — Search glossaries by name (optimized with joins)
        var glossaryProducts = await _context.Products
            .Where(p => !p.IsDeleted && p.ProductType == "Glossary" && p.GlossaryId != null)
            .Join(_context.Glossaries.Where(g => !g.IsDeleted),
                p => p.GlossaryId,
                g => g.GlossaryId,
                (p, g) => new { Product = p, Glossary = g })
            .Where(x => x.Glossary.Name.ToLower().Contains(q))
            .Select(x => new
            {
                x.Product.ProductSku,
                x.Product.ProductType,
                ProductName = x.Glossary.Name,
                Strength = string.Empty
            })
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        // STEP 3 — Combine results
        var allProducts = medicineProducts
            .Select(mp => new { mp.ProductSku, mp.ProductType, mp.ProductName, Strength = mp.Strength ?? string.Empty })
            .Concat(glossaryProducts.Select(gp => new { gp.ProductSku, gp.ProductType, gp.ProductName, gp.Strength }))
            .Take(limit)
            .ToList();

        if (allProducts.Count == 0)
            return Ok(new List<ProductSearchResultDto>());

        // STEP 4 — Load stock data in bulk (single query)
        var productSkus = allProducts.Select(p => p.ProductSku).ToList();

        var stockData = await _context.Stocks
            .Where(s => productSkus.Contains(s.ProductSku) && s.QuantityOnHand > 0)
            .GroupBy(s => s.ProductSku)
            .Select(g => new
            {
                ProductSku = g.Key,
                SellingPrice = g.OrderByDescending(s => s.StockId).First().SellingPrice,
                TotalQuantity = g.Sum(s => s.QuantityOnHand)
            })
            .AsNoTracking()
            .ToListAsync();

        var stockLookup = stockData.ToDictionary(s => s.ProductSku);

        var barcodeLookup = await _context.Products
            .Where(p => productSkus.Contains(p.ProductSku))
            .Select(p => new { p.ProductSku, p.Barcode })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ProductSku, x => x.Barcode);

        // STEP 5 — Map to DTOs
        var result = allProducts.Select(p =>
        {
            stockLookup.TryGetValue(p.ProductSku, out var stock);
            barcodeLookup.TryGetValue(p.ProductSku, out var barcode);

            return new ProductSearchResultDto
            {
                ProductSku = p.ProductSku,
                Name = p.ProductName,
                Strength = p.Strength ?? "",
                ProductType = p.ProductType,
                SellingPrice = stock?.SellingPrice ?? 0,
                TotalQuantityOnHand = stock?.TotalQuantity ?? 0,
                Barcode = barcode
            };
        }).ToList();

        return Ok(result);
    }

    // View Purchase by ID - OWNER and ADMIN only (must be after specific routes)
    [RequirePermission("purchasing:view")]
    [HttpGet("{purchaseId}")]
    public async Task<IActionResult> GetPurchaseById(string purchaseId)
    {
        _logger.LogInformation("Fetching purchase with ID: {PurchaseId}", purchaseId);
        var purchase = await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems)
            .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);

        if (purchase == null)
        {
            return NotFound($"Purchase with ID {purchaseId} not found.");
        }

        // Resolve product names via Medicine/Glossary lookup (same approach as GetAllPurchases)
        var productSkus = purchase.PurchaseItems.Select(pi => pi.ProductSku).Distinct().ToList();

        var products = await _context.Products
            .Where(p => productSkus.Contains(p.ProductSku))
            .AsNoTracking()
            .ToListAsync();

        var medicineIds = products.Where(p => p.MedicineId != null).Select(p => p.MedicineId!).Distinct().ToList();
        var glossaryIds = products.Where(p => p.GlossaryId != null).Select(p => p.GlossaryId!).Distinct().ToList();

        var medicines = await _context.Medicines
            .Where(m => medicineIds.Contains(m.MedicineId))
            .AsNoTracking()
            .ToListAsync();

        var glossaries = await _context.Glossaries
            .Where(g => glossaryIds.Contains(g.GlossaryId))
            .AsNoTracking()
            .ToListAsync();

        var productLookup = products.ToDictionary(p => p.ProductSku);
        var medicineLookup = medicines.ToDictionary(m => m.MedicineId);
        var glossaryLookup = glossaries.ToDictionary(g => g.GlossaryId);

        var purchaseDTO = new PurchaseDTO
        {
            PurchaseId = purchase.PurchaseId,
            InvoiceNumber = purchase.InvoiceNumber,
            InvoiceDate = purchase.InvoiceDate,
            PaymentStatus = purchase.PaymentStatus,
            PaymentDueDate = purchase.PaymentDueDate,
            TotalAmount = purchase.TotalAmount,
            SupplierId = purchase.SupplierId,
            PurchaseItems = purchase.PurchaseItems.Select(pi =>
            {
                string productName = "Unknown";

                if (productLookup.TryGetValue(pi.ProductSku, out var product))
                {
                    if (product.ProductType == "Medicine" && product.MedicineId != null)
                    {
                        if (medicineLookup.TryGetValue(product.MedicineId, out var medicine))
                        {
                            productName = medicine.Name;
                        }
                    }
                    else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                    {
                        if (glossaryLookup.TryGetValue(product.GlossaryId, out var glossary))
                        {
                            productName = glossary.Name;
                        }
                    }
                }

                return new PurchaseItemDto
                {
                    PurchaseItemId = pi.PurchaseItemId,
                    ProductSKU = pi.ProductSku,
                    ProductName = productName,
                    Quantity = pi.Quantity,
                    CostPrice = pi.CostPrice,
                    SellingPrice = pi.SellingPrice,
                    ExpireDate = pi.ExpireDate.ToDateTime(new TimeOnly(0, 0))
                };
            }).ToList()
        };

        return Ok(purchaseDTO);
    }

}
