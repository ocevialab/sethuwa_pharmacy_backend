using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class GlossaryController : ControllerBase
{
    private readonly SethuwaPharmacyDbContext _context;
    private readonly ILogger<GlossaryController> _logger;

    public GlossaryController(SethuwaPharmacyDbContext context, ILogger<GlossaryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // POST: api/Glossary
    [RequirePermission("glossary:create")]
    [HttpPost]
    public async Task<ActionResult<GlossaryDto>> CreateGlossary(GlossaryCreationDto dto)
    {
        _logger.LogInformation("Creating a new glossary with data: {@GlossaryCreationDto}", dto);
        if (dto == null)
            return BadRequest("Invalid request payload.");

        // Generate sequential ID
        var newGlossaryId = await _context.Glossaries
            .GenerateNextSequentialId("GLO", "GlossaryId");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _logger.LogInformation("Generated new Glossary ID: {GlossaryId}", newGlossaryId);
            //create Glossary
            var glossary = new Glossary
            {
                GlossaryId = newGlossaryId,
                Name = dto.Name,
                BrandName = dto.BrandName,
                LowStockThreshold = dto.LowStockThreshold,
                IsDeleted = false
            };

            _context.Glossaries.Add(glossary);

            _logger.LogInformation("Creating product abstraction for glossary ID: {GlossaryId}", newGlossaryId);
            //craete Product abstraction
            var product = new Product
            {
                ProductSku = newGlossaryId,
                ProductType = "Glossary",
                MedicineId = null,
                GlossaryId = newGlossaryId,
                IsDeleted = false
            };
            _context.Products.Add(product);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            //responce
            var responseDto = new GlossaryDto
            {
                GlossaryId = glossary.GlossaryId,
                Name = glossary.Name,
                BrandName = glossary.BrandName,
                LowStockThreshold = glossary.LowStockThreshold,
                IsDeleted = glossary.IsDeleted,
                ProductSku = product.ProductSku
            };
            _logger.LogInformation("Created glossary with ID: {GlossaryId}", glossary.GlossaryId);
            return CreatedAtAction(nameof(CreateGlossary), new { id = glossary.GlossaryId }, responseDto);
        }
        catch (Exception)
        {
            _logger.LogError("Error occurred while creating glossary with ID: {GlossaryId}. Rolling back transaction.", newGlossaryId);
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while creating the glossary.");
        }
    }

    //GET: api/Glossary/{id}
    [RequirePermission("glossary:view")]
    [HttpGet("{id}")]
    public async Task<ActionResult<GlossaryDto>> GetGlossary(string id)
    {
        var glossary = await _context.Glossaries
              .FirstOrDefaultAsync(g => g.GlossaryId == id);

        if (glossary == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.GlossaryId == id);

        var glossaryDto = new GlossaryDto
        {
            GlossaryId = glossary.GlossaryId,
            Name = glossary.Name,
            BrandName = glossary.BrandName,
            LowStockThreshold = glossary.LowStockThreshold,
            IsDeleted = glossary.IsDeleted,
            ProductSku = product?.ProductSku
        };

        return Ok(glossaryDto);
    }

    // PUT: api/Glossary/{id}
    [RequirePermission("glossary:update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGlossary(string id, GlossaryUpdateDto dto)
    {
        _logger.LogInformation("Updating glossary with ID: {GlossaryId}, data: {@GlossaryUpdateDto}", id, dto);

        if (dto == null)
        {
            _logger.LogWarning("Invalid glossary update request payload.");
            return BadRequest("Invalid request payload.");
        }

        // Check for duplicate name (excluding current glossary)
        if (await _context.Glossaries.AnyAsync(g => g.Name == dto.Name && g.GlossaryId != id && !g.IsDeleted))
        {
            _logger.LogWarning("Duplicate glossary name found: {Name}", dto.Name);
            return Conflict("A glossary with the same name already exists.");
        }

        var existingGlossary = await _context.Glossaries.FindAsync(id);

        if (existingGlossary == null)
        {
            _logger.LogWarning("Glossary with ID {GlossaryId} not found.", id);
            return NotFound($"Glossary with ID {id} not found.");
        }

        // Apply updates
        existingGlossary.Name = dto.Name;
        existingGlossary.BrandName = dto.BrandName ?? string.Empty;
        existingGlossary.LowStockThreshold = dto.LowStockThreshold;

        _context.Entry(existingGlossary).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Glossary with ID {GlossaryId} updated successfully.", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Glossaries.Any(e => e.GlossaryId == id))
            {
                _logger.LogWarning("Glossary with ID {GlossaryId} was deleted during update.", id);
                return NotFound();
            }

            _logger.LogError("Concurrency exception while updating glossary with ID {GlossaryId}.", id);
            throw;
        }

        return NoContent();
    }

    //SOFT DELETE: api/Glossary/{id}
    [RequirePermission("glossary:delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGlossary(string id)
    {
        var glossary = await _context.Glossaries.FindAsync(id);
        if (glossary == null)
        {
            return NotFound();
        }

        glossary.IsDeleted = true;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.GlossaryId == id);

        if (product != null)
        {
            product.IsDeleted = true;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    //restore: api/Glossary/restore/{id}
    //soft restore
    [RequirePermission("glossary:restore")]
    [HttpPost("restore/{id}")]
    public async Task<IActionResult> RestoreGlossary(string id)
    {
        var glossary = await _context.Glossaries.FindAsync(id);
        if (glossary == null)
        {
            return NotFound();
        }

        glossary.IsDeleted = false;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.GlossaryId == id);

        if (product != null)
        {
            product.IsDeleted = false;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // GET: api/Glossaries?page=1&pageSize=10&sortBy=name&sortDirection=asc&name=&brandName=&productSku=&glossaryId=&minLowStockThreshold=&maxLowStockThreshold=
    [RequirePermission("glossary:view")]
    [HttpGet]
    public async Task<IActionResult> GetAllGlossaries(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? sortDirection = null,
        string? name = null,
        string? brandName = null,
        string? productSku = null,
        string? glossaryId = null,
        int? minLowStockThreshold = null,
        int? maxLowStockThreshold = null)
    {
        // Pagination validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation($"Fetching glossaries with filters - page: {page}, pageSize: {pageSize}, sortBy: {sortBy}, sortDirection: {sortDirection}");

        // Base query with includes - exclude deleted items
        var query = _context.Glossaries
            .Where(g => !g.IsDeleted)
            .Include(g => g.Products)
            .AsNoTracking()
            .AsQueryable();

        // ============================================
        // FILTERING
        // ============================================

        // Filter by GlossaryId (Contains search)
        if (!string.IsNullOrWhiteSpace(glossaryId))
        {
            glossaryId = glossaryId.Trim();
            query = query.Where(g => g.GlossaryId.Contains(glossaryId));
        }

        // Filter by Name (Contains search)
        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
            query = query.Where(g => g.Name.Contains(name));
        }

        // Filter by BrandName (Contains search)
        if (!string.IsNullOrWhiteSpace(brandName))
        {
            brandName = brandName.Trim();
            query = query.Where(g => g.BrandName.Contains(brandName));
        }

        // Filter by ProductSku (search in related Products)
        if (!string.IsNullOrWhiteSpace(productSku))
        {
            productSku = productSku.Trim();
            query = query.Where(g => g.Products.Any(p => p.ProductSku.Contains(productSku) && !p.IsDeleted));
        }

        // Filter by LowStockThreshold Range
        if (minLowStockThreshold.HasValue)
        {
            query = query.Where(g => g.LowStockThreshold >= minLowStockThreshold.Value);
        }

        if (maxLowStockThreshold.HasValue)
        {
            query = query.Where(g => g.LowStockThreshold <= maxLowStockThreshold.Value);
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
                    ? query.OrderBy(g => g.GlossaryId)
                    : query.OrderByDescending(g => g.GlossaryId),

                "name" => isAscending
                    ? query.OrderBy(g => g.Name)
                    : query.OrderByDescending(g => g.Name),

                "brandname" => isAscending
                    ? query.OrderBy(g => g.BrandName)
                    : query.OrderByDescending(g => g.BrandName),

                "lowstockthreshold" => isAscending
                    ? query.OrderBy(g => g.LowStockThreshold)
                    : query.OrderByDescending(g => g.LowStockThreshold),

                _ => query.OrderBy(g => g.Name) // Default: name asc
            };
        }
        else
        {
            // Default sorting: name ascending
            query = query.OrderBy(g => g.Name);
        }

        // ============================================
        // COUNT TOTAL ITEMS (before pagination)
        // ============================================
        var totalItems = await query.CountAsync();

        // ============================================
        // PAGINATION
        // ============================================
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var glossaries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ============================================
        // MAPPING TO DTO
        // ============================================
        var glossaryDtos = glossaries.Select(g => new GlossaryDto
        {
            GlossaryId = g.GlossaryId,
            Name = g.Name,
            BrandName = g.BrandName,
            LowStockThreshold = g.LowStockThreshold,
            IsDeleted = g.IsDeleted,
            // Pick 1 product SKU if exists
            ProductSku = g.Products.FirstOrDefault()?.ProductSku
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
            data = glossaryDtos
        });
    }

    // GET: api/Glossary/deleted
    [RequirePermission("glossary:view_deleted")]
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedGlossaries()
    {
        _logger.LogInformation("Fetching all deleted glossaries");

        var deletedGlossaries = await _context.Glossaries
            .Where(g => g.IsDeleted)
            .Include(g => g.Products)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync();

        var glossaryDtos = deletedGlossaries.Select(g => new GlossaryDto
        {
            GlossaryId = g.GlossaryId,
            Name = g.Name,
            BrandName = g.BrandName,
            LowStockThreshold = g.LowStockThreshold,
            IsDeleted = g.IsDeleted,
            ProductSku = g.Products.FirstOrDefault()?.ProductSku
        }).ToList();

        return Ok(glossaryDtos);
    }

}