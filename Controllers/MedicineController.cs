using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class MedicineController : ControllerBase
{
    private readonly ThilankaPharmacyDbContext _context;
    private readonly ILogger<MedicineController> _logger;

    public MedicineController(ThilankaPharmacyDbContext context, ILogger<MedicineController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // POST: api/Medicine
    [RequirePermission("medicine:create")]
    [HttpPost]
    public async Task<ActionResult<MedicineDto>> CreateMedicine(MedicineCreationDto dto)
    {
        _logger.LogInformation("Creating a new medicine with data: {@MedicineCreationDto}", dto);
        if (dto == null)
        {
            _logger.LogWarning("Invalid medicine creation request payload.");
            return BadRequest("Invalid request payload.");
        }

        // Check for duplicate medicine by Name, BrandName, GenericName, Strength
        if (await _context.Medicines.AnyAsync(m => m.Name == dto.Name && !m.IsDeleted))
        {
            _logger.LogWarning("Duplicate medicine creation attempt for name: {Name}", dto.Name);
            return Conflict("A medicine with the same name already exists.");
        }

        // Generate sequential ID
        var newMedicineId = await _context.Medicines
            .GenerateNextSequentialId("MED", "MedicineId", _logger);

        _logger.LogInformation("Generated new MedicineId: {MedicineId}", newMedicineId);

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {

            _logger.LogInformation("Creating Medicine entity with ID: {MedicineId}", newMedicineId);
            var medicine = new Medicine
            {
                MedicineId = newMedicineId,
                Name = dto.Name,
                BrandName = dto.BrandName,
                GenericName = dto.GenericName,
                Manufacture = dto.Manufacture,
                Category = dto.Category,
                Strength = dto.Strength,
                RequiredPrescription = dto.RequiredPrescription,
                LowStockThreshold = dto.LowStockThreshold,
                IsDeleted = false
            };
            _logger.LogInformation("Medicine entity created: {@Medicine}", medicine);
            _context.Medicines.Add(medicine);

            // Create Product abstraction
            var product = new Product
            {
                ProductSku = newMedicineId,
                ProductType = "Medicine",
                MedicineId = newMedicineId,
                GlossaryId = null,
                IsDeleted = false
            };

            _context.Products.Add(product);
            _logger.LogInformation("Product abstraction created for MedicineId: {MedicineId}", newMedicineId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Medicine creation transaction committed for MedicineId: {MedicineId}", newMedicineId);

            // Response DTO
            var response = new MedicineDto
            {
                MedicineId = medicine.MedicineId,
                Name = medicine.Name,
                BrandName = medicine.BrandName,
                GenericName = medicine.GenericName,
                Manufacture = medicine.Manufacture,
                Category = medicine.Category,
                Strength = medicine.Strength,
                RequiredPrescription = medicine.RequiredPrescription ?? false,
                LowStockThreshold = medicine.LowStockThreshold ?? 0,
                ProductSku = product.ProductSku,
                IsDeleted = medicine.IsDeleted
            };
            _logger.LogInformation("Medicine creation response prepared: {@MedicineDto}", response);
            return CreatedAtAction(nameof(GetMedicine), new { id = newMedicineId }, response);
        }
        catch
        {
            _logger.LogError("Error occurred while creating medicine with ID: {MedicineId}. Rolling back transaction.", newMedicineId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    // GET: api/Medicine/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Medicine>> GetMedicine(string id)
    {
        var medicine = await _context.Medicines.FindAsync(id);

        if (medicine == null)
            return NotFound();

        return medicine;
    }

    // PUT: api/Medicine/{id}
    [RequirePermission("medicine:update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMedicine(string id, MedicineUpdateDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid request payload.");

        if (await _context.Medicines.AnyAsync(m => m.Name == dto.Name && m.MedicineId != id && !m.IsDeleted))
        {
            return Conflict("A medicine with the same name already exists.");
        }

        var existingMedicine = await _context.Medicines.FindAsync(id);

        if (existingMedicine == null)
            return NotFound($"Medicine with ID {id} not found.");

        // Apply updates
        existingMedicine.Name = dto.Name;
        existingMedicine.BrandName = dto.BrandName;
        existingMedicine.GenericName = dto.GenericName;
        existingMedicine.Manufacture = dto.Manufacture;
        existingMedicine.Category = dto.Category;
        existingMedicine.Strength = dto.Strength;
        existingMedicine.RequiredPrescription = dto.RequiredPrescription;
        existingMedicine.LowStockThreshold = dto.LowStockThreshold;
        existingMedicine.IsDeleted = dto.IsDeleted;

        _context.Entry(existingMedicine).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Medicines.Any(e => e.MedicineId == id))
                return NotFound();

            throw;
        }

        return NoContent();
    }

    // SOFT DELETE: api/Medicine/{id}
    [RequirePermission("medicine:delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDeleteMedicine(string id)
    {
        var medicine = await _context.Medicines.FindAsync(id);

        if (medicine == null)
            return NotFound($"Medicine with ID {id} not found.");

        // Already deleted?
        if (medicine.IsDeleted)
            return BadRequest("This medicine is already deleted.");

        // Soft delete medicine
        medicine.IsDeleted = true;

        // Also soft delete the product abstraction
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.MedicineId == id);

        if (product != null)
            product.IsDeleted = true;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/Medicine?page=1&pageSize=10&sortBy=name&sortDirection=asc&medicineId=&name=&brandName=&genericName=&manufacture=&category=&strength=&productSku=&requiredPrescription=&minLowStockThreshold=&maxLowStockThreshold=
    [RequirePermission("medicine:view_all")]
    [HttpGet]
    public async Task<IActionResult> GetAllMedicines(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? sortDirection = null,
        string? medicineId = null,
        string? name = null,
        string? brandName = null,
        string? genericName = null,
        string? manufacture = null,
        string? category = null,
        string? strength = null,
        string? productSku = null,
        bool? requiredPrescription = null,
        int? minLowStockThreshold = null,
        int? maxLowStockThreshold = null)
    {
        // Pagination validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation($"Fetching medicines with filters - page: {page}, pageSize: {pageSize}, sortBy: {sortBy}, sortDirection: {sortDirection}");

        // Base query with includes - exclude deleted items
        var query = _context.Medicines
            .Where(m => !m.IsDeleted)
            .Include(m => m.Products)
            .AsNoTracking()
            .AsQueryable();

        // ============================================
        // FILTERING
        // ============================================

        // Filter by MedicineId (Contains search)
        if (!string.IsNullOrWhiteSpace(medicineId))
        {
            medicineId = medicineId.Trim();
            query = query.Where(m => m.MedicineId.Contains(medicineId));
        }

        // Filter by Name (Contains search)
        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
            query = query.Where(m => m.Name.Contains(name));
        }

        // Filter by BrandName (Contains search)
        if (!string.IsNullOrWhiteSpace(brandName))
        {
            brandName = brandName.Trim();
            query = query.Where(m => m.BrandName != null && m.BrandName.Contains(brandName));
        }

        // Filter by GenericName (Contains search)
        if (!string.IsNullOrWhiteSpace(genericName))
        {
            genericName = genericName.Trim();
            query = query.Where(m => m.GenericName != null && m.GenericName.Contains(genericName));
        }

        // Filter by Manufacture (Contains search)
        if (!string.IsNullOrWhiteSpace(manufacture))
        {
            manufacture = manufacture.Trim();
            query = query.Where(m => m.Manufacture != null && m.Manufacture.Contains(manufacture));
        }

        // Filter by Category (Contains search)
        if (!string.IsNullOrWhiteSpace(category))
        {
            category = category.Trim();
            query = query.Where(m => m.Category != null && m.Category.Contains(category));
        }

        // Filter by Strength (Contains search)
        if (!string.IsNullOrWhiteSpace(strength))
        {
            strength = strength.Trim();
            query = query.Where(m => m.Strength != null && m.Strength.Contains(strength));
        }

        // Filter by ProductSku (search in related Products)
        if (!string.IsNullOrWhiteSpace(productSku))
        {
            productSku = productSku.Trim();
            query = query.Where(m => m.Products.Any(p => p.ProductSku.Contains(productSku) && !p.IsDeleted));
        }

        // Filter by RequiredPrescription
        if (requiredPrescription.HasValue)
        {
            query = query.Where(m => m.RequiredPrescription == requiredPrescription.Value);
        }

        // Filter by LowStockThreshold Range
        if (minLowStockThreshold.HasValue)
        {
            query = query.Where(m => m.LowStockThreshold >= minLowStockThreshold.Value);
        }

        if (maxLowStockThreshold.HasValue)
        {
            query = query.Where(m => m.LowStockThreshold <= maxLowStockThreshold.Value);
        }

        // ============================================
        // SORTING
        // ============================================

        // Validate sortDirection
        bool isAscending = !string.IsNullOrWhiteSpace(sortDirection) &&
                          sortDirection.Trim().ToLower() == "asc";

        // Apply sorting based on sortBy parameter
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = sortBy.Trim().ToLower();

            query = sortBy switch
            {
                "id" => isAscending
                    ? query.OrderBy(m => m.MedicineId)
                    : query.OrderByDescending(m => m.MedicineId),

                "name" => isAscending
                    ? query.OrderBy(m => m.Name)
                    : query.OrderByDescending(m => m.Name),

                "brandname" => isAscending
                    ? query.OrderBy(m => m.BrandName)
                    : query.OrderByDescending(m => m.BrandName),

                "genericname" => isAscending
                    ? query.OrderBy(m => m.GenericName)
                    : query.OrderByDescending(m => m.GenericName),

                "category" => isAscending
                    ? query.OrderBy(m => m.Category)
                    : query.OrderByDescending(m => m.Category),

                "strength" => isAscending
                    ? query.OrderBy(m => m.Strength)
                    : query.OrderByDescending(m => m.Strength),

                "lowstockthreshold" => isAscending
                    ? query.OrderBy(m => m.LowStockThreshold)
                    : query.OrderByDescending(m => m.LowStockThreshold),

                _ => query.OrderBy(m => m.Name) // Default: name asc
            };
        }
        else
        {
            // Default sorting: name ascending
            query = query.OrderBy(m => m.Name);
        }

        // ============================================
        // COUNT TOTAL ITEMS (before pagination)
        // ============================================
        var totalItems = await query.CountAsync();

        // ============================================
        // PAGINATION
        // ============================================
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var medicines = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ============================================
        // MAPPING TO DTO
        // ============================================
        var medicineDtos = medicines.Select(m => new MedicineDto
        {
            MedicineId = m.MedicineId,
            Name = m.Name,
            BrandName = m.BrandName,
            GenericName = m.GenericName,
            Manufacture = m.Manufacture,
            Category = m.Category,
            Strength = m.Strength,
            RequiredPrescription = m.RequiredPrescription ?? false,
            LowStockThreshold = m.LowStockThreshold ?? 0,
            ProductSku = m.Products.FirstOrDefault()?.ProductSku,
            IsDeleted = m.IsDeleted
        }).ToList();

        // ============================================
        // RETURN PAGINATION ENVELOPE
        // ============================================
        return Ok(new
        {
            currentPage = page,
            pageSize,
            totalItems,
            totalPages,
            data = medicineDtos
        });
    }

    // GET: api/Medicine/deleted
    [RequirePermission("medicine:view_deleted")]
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedMedicines()
    {
        _logger.LogInformation("Fetching all deleted medicines");

        var deletedMedicines = await _context.Medicines
            .Where(m => m.IsDeleted)
            .Include(m => m.Products)
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();

        var medicineDtos = deletedMedicines.Select(m => new MedicineDto
        {
            MedicineId = m.MedicineId,
            Name = m.Name,
            BrandName = m.BrandName,
            GenericName = m.GenericName,
            Manufacture = m.Manufacture,
            Category = m.Category,
            Strength = m.Strength,
            RequiredPrescription = m.RequiredPrescription ?? false,
            LowStockThreshold = m.LowStockThreshold ?? 0,
            IsDeleted = m.IsDeleted,
            ProductSku = m.Products.FirstOrDefault()?.ProductSku
        }).ToList();

        return Ok(medicineDtos);
    }

    // RESTORE: api/Medicine/restore/{id}
    // Soft restore
    [RequirePermission("medicine:restore")]
    [HttpPost("restore/{id}")]
    public async Task<IActionResult> RestoreMedicine(string id)
    {
        _logger.LogInformation("Restoring medicine with ID: {MedicineId}", id);

        var medicine = await _context.Medicines.FindAsync(id);
        if (medicine == null)
        {
            _logger.LogWarning("Medicine not found for restore: {MedicineId}", id);
            return NotFound($"Medicine with ID {id} not found.");
        }

        // Already restored?
        if (!medicine.IsDeleted)
        {
            _logger.LogWarning("Medicine is already active (not deleted): {MedicineId}", id);
            return BadRequest("This medicine is already active (not deleted).");
        }

        medicine.IsDeleted = false;

        // Also restore the product abstraction
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.MedicineId == id);

        if (product != null)
        {
            product.IsDeleted = false;
            _logger.LogInformation("Product abstraction restored for MedicineId: {MedicineId}", id);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Medicine restored successfully: {MedicineId}", id);

        return NoContent();
    }

    // GET: api/Medicine/summary
    [RequirePermission("medicine:view_summary")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetMedicineSummary()
    {
        _logger.LogInformation("Fetching medicine summary");

        // Total medicines (including deleted)
        var totalMedicines = await _context.Medicines.CountAsync();

        // Active medicines (not deleted)
        var activeMedicines = await _context.Medicines
            .Where(m => !m.IsDeleted)
            .CountAsync();

        // Deleted medicines
        var deletedMedicines = await _context.Medicines
            .Where(m => m.IsDeleted)
            .CountAsync();

        // Medicines requiring prescription
        var prescriptionRequired = await _context.Medicines
            .Where(m => !m.IsDeleted && m.RequiredPrescription == true)
            .CountAsync();

        // Non-prescription medicines
        var nonPrescription = await _context.Medicines
            .Where(m => !m.IsDeleted && (m.RequiredPrescription == false || m.RequiredPrescription == null))
            .CountAsync();

        var summary = new MedicineSummaryDto
        {
            TotalMedicines = totalMedicines,
            ActiveMedicines = activeMedicines,
            DeletedMedicines = deletedMedicines,
            PrescriptionRequired = prescriptionRequired,
            NonPrescription = nonPrescription
        };

        _logger.LogInformation("Medicine summary retrieved: Total={Total}, Active={Active}, Deleted={Deleted}",
            totalMedicines, activeMedicines, deletedMedicines);

        return Ok(summary);
    }

    //test api
    [RequirePermission("medicine:view")]
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("Test API is working");
    }

}
