using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class SupplierController : ControllerBase
{
    private readonly SethsuwaPharmacyDbContext _context;
    private readonly ILogger<SupplierController> _logger;

    public SupplierController(SethsuwaPharmacyDbContext context, ILogger<SupplierController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // POST: api/Supplier
    [RequirePermission("supplier:create")]
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(SupplierCreationDto dto)
    {
        _logger.LogInformation("Creating a new supplier with data: {@SupplierCreationDto}", dto);
        //validate dto
        if (dto == null)
        {
            _logger.LogWarning("Invalid supplier creation request payload.");
            return BadRequest("Invalid request payload.");
        }

        //model validation
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Supplier creation request model validation failed: {@ModelState}", ModelState);
            return BadRequest(ModelState);
        }


        // Generate sequential ID
        var newSupplierId = await _context.Suppliers
            .GenerateNextSequentialId("SUP", "SupplierId");

        var supplier = new Supplier
        {
            SupplierId = newSupplierId,
            SupplierName = dto.SupplierName,
            ContactPerson = dto.ContactPerson,
            ContactNumber = dto.ContactNumber,
            EmailAddress = dto.EmailAddress,
            Address = dto.Address,
            BankName = dto.BankName,
            BankAccountName = dto.BankAccountName,
            BankAccountNumber = dto.BankAccountNumber,
            BankBranchName = dto.BankBranchName
        };
        _logger.LogInformation("Created a new supplier with ID: {SupplierId}", newSupplierId);
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var supplierDto = new SupplierDto
        {
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            ContactNumber = supplier.ContactNumber,
            EmailAddress = supplier.EmailAddress,
            Address = supplier.Address,
            BankName = supplier.BankName,
            BankAccountName = supplier.BankAccountName,
            BankAccountNumber = supplier.BankAccountNumber,
            BankBranchName = supplier.BankBranchName
        };

        _logger.LogInformation("Supplier creation response prepared: {@SupplierDto}", supplierDto);

        return CreatedAtAction(nameof(CreateSupplier), new { id = supplier.SupplierId }, supplierDto);
    }

    // PUT: api/Supplier/{id}
    [RequirePermission("supplier:update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(string id, SupplierUpdateDto dto)
    {
        _logger.LogInformation("Updating supplier {Id} with data: {@SupplierUpdateDto}", id, dto);
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
        {
            _logger.LogWarning("Supplier with ID {Id} not found for update.", id);
            return NotFound("Supplier not found.");
        }


        supplier.SupplierName = dto.SupplierName;
        supplier.ContactPerson = dto.ContactPerson;
        supplier.ContactNumber = dto.ContactNumber;
        supplier.EmailAddress = dto.EmailAddress;
        supplier.Address = dto.Address;
        supplier.BankName = dto.BankName;
        supplier.BankAccountName = dto.BankAccountName;
        supplier.BankAccountNumber = dto.BankAccountNumber;
        supplier.BankBranchName = dto.BankBranchName;

        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [RequirePermission("supplier:view")]
    //GET: api/Supplier/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(string id)
    {
        _logger.LogInformation("Fetching supplier details for ID: {Id}", id);
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
        {
            _logger.LogWarning("Supplier with ID {Id} not found.", id);
            return NotFound("Supplier not found.");
        }


        var supplierDto = new SupplierDto
        {
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            ContactNumber = supplier.ContactNumber,
            EmailAddress = supplier.EmailAddress,
            Address = supplier.Address,
            BankName = supplier.BankName,
            BankAccountName = supplier.BankAccountName,
            BankAccountNumber = supplier.BankAccountNumber,
            BankBranchName = supplier.BankBranchName
        };

        return Ok(supplierDto);
    }

    // GET: api/Supplier/search?q=hemas&limit=20
    [RequirePermission("supplier:search")]
    [HttpGet("search")]
    public async Task<IActionResult> SearchSuppliers([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<SupplierSearchResultDto>());

        q = q.Trim().ToLower();

        _logger.LogInformation($"Fast supplier search: {q}");

        // Optimized search - searches multiple fields
        var suppliers = await _context.Suppliers
            .Where(s =>
                s.SupplierName.ToLower().Contains(q) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(q)) ||
                s.ContactNumber.ToLower().Contains(q) ||
                (s.EmailAddress != null && s.EmailAddress.ToLower().Contains(q)) ||
                (s.Address != null && s.Address.ToLower().Contains(q)) ||
                s.SupplierId.ToLower().Contains(q)
            )
            .OrderBy(s => s.SupplierName)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        var result = suppliers.Select(s => new SupplierSearchResultDto
        {
            SupplierId = s.SupplierId,
            SupplierName = s.SupplierName,
            ContactPerson = s.ContactPerson,
            ContactNumber = s.ContactNumber,
            EmailAddress = s.EmailAddress
        }).ToList();

        return Ok(result);
    }

    // GET: api/Supplier?page=1&pageSize=10&sortBy=name&sortDirection=asc&supplierId=&supplierName=&contactPerson=&contactNumber=&emailAddress=&address=&bankName=
    [RequirePermission("supplier:view_all")]
    [HttpGet]
    public async Task<IActionResult> GetAllSuppliers(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? sortDirection = null,
        string? supplierId = null,
        string? supplierName = null,
        string? contactPerson = null,
        string? contactNumber = null,
        string? emailAddress = null,
        string? address = null,
        string? bankName = null)
    {
        // Pagination validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation($"Fetching suppliers with filters - page: {page}, pageSize: {pageSize}, sortBy: {sortBy}, sortDirection: {sortDirection}");

        // Base query
        var query = _context.Suppliers
            .AsNoTracking()
            .AsQueryable();

        // ============================================
        // FILTERING
        // ============================================

        // Filter by SupplierId (Contains search)
        if (!string.IsNullOrWhiteSpace(supplierId))
        {
            supplierId = supplierId.Trim();
            query = query.Where(s => s.SupplierId.Contains(supplierId));
        }

        // Filter by SupplierName (Contains search)
        if (!string.IsNullOrWhiteSpace(supplierName))
        {
            supplierName = supplierName.Trim();
            query = query.Where(s => s.SupplierName.Contains(supplierName));
        }

        // Filter by ContactPerson (Contains search)
        if (!string.IsNullOrWhiteSpace(contactPerson))
        {
            contactPerson = contactPerson.Trim();
            query = query.Where(s => s.ContactPerson != null && s.ContactPerson.Contains(contactPerson));
        }

        // Filter by ContactNumber (Contains search)
        if (!string.IsNullOrWhiteSpace(contactNumber))
        {
            contactNumber = contactNumber.Trim();
            query = query.Where(s => s.ContactNumber.Contains(contactNumber));
        }

        // Filter by EmailAddress (Contains search)
        if (!string.IsNullOrWhiteSpace(emailAddress))
        {
            emailAddress = emailAddress.Trim();
            query = query.Where(s => s.EmailAddress != null && s.EmailAddress.Contains(emailAddress));
        }

        // Filter by Address (Contains search)
        if (!string.IsNullOrWhiteSpace(address))
        {
            address = address.Trim();
            query = query.Where(s => s.Address != null && s.Address.Contains(address));
        }

        // Filter by BankName (Contains search)
        if (!string.IsNullOrWhiteSpace(bankName))
        {
            bankName = bankName.Trim();
            query = query.Where(s => s.BankName != null && s.BankName.Contains(bankName));
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
                    ? query.OrderBy(s => s.SupplierId)
                    : query.OrderByDescending(s => s.SupplierId),

                "name" => isAscending
                    ? query.OrderBy(s => s.SupplierName)
                    : query.OrderByDescending(s => s.SupplierName),

                "contactperson" => isAscending
                    ? query.OrderBy(s => s.ContactPerson ?? string.Empty)
                    : query.OrderByDescending(s => s.ContactPerson ?? string.Empty),

                "contactnumber" => isAscending
                    ? query.OrderBy(s => s.ContactNumber)
                    : query.OrderByDescending(s => s.ContactNumber),

                "emailaddress" => isAscending
                    ? query.OrderBy(s => s.EmailAddress ?? string.Empty)
                    : query.OrderByDescending(s => s.EmailAddress ?? string.Empty),

                _ => query.OrderBy(s => s.SupplierName) // Default: name asc
            };
        }
        else
        {
            // Default sorting: supplier name ascending
            query = query.OrderBy(s => s.SupplierName);
        }

        // ============================================
        // COUNT TOTAL ITEMS (before pagination)
        // ============================================
        var totalItems = await query.CountAsync();

        // ============================================
        // PAGINATION
        // ============================================
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var suppliers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ============================================
        // MAPPING TO DTO
        // ============================================
        var supplierDtos = suppliers.Select(s => new
        {
            SupplierId = s.SupplierId,
            SupplierName = s.SupplierName,
            ContactPerson = s.ContactPerson,
            ContactNumber = s.ContactNumber,
            EmailAddress = s.EmailAddress,
            Address = s.Address,
            BankName = s.BankName,
            BankAccountName = s.BankAccountName,
            BankAccountNumber = s.BankAccountNumber,
            BankBranchName = s.BankBranchName
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
            data = supplierDtos
        });
    }

}

