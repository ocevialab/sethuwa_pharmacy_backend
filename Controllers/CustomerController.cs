using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    private readonly SethuwaPharmacyDbContext _context;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(SethuwaPharmacyDbContext context, ILogger<CustomerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ⭐ CREATE CUSTOMER
    [RequirePermission("customer:create")]
    [HttpPost]
    public async Task<IActionResult> CreateCustomer(CustomerCreationDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid payload");

        // Check duplicates by phone number (business rule)
        if (await _context.Customers.AnyAsync(c => c.ContactNumber == dto.ContactNumber))
            return Conflict("A customer with this contact number already exists.");

        // Generate new sequential ID
        var newCustomerId = await _context.Customers
            .GenerateNextSequentialId("CUS", "CustomerId");

        var customer = new Customer
        {
            CustomerId = newCustomerId,
            CustomerName = dto.CustomerName,
            ContactNumber = dto.ContactNumber,
            EmailAddress = dto.EmailAddress,
            Address = dto.Address,
            Discount = dto.Discount ?? 0,
            CustomerStatus = dto.CustomerStatus
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var response = new CustomerResponseDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            ContactNumber = customer.ContactNumber,
            EmailAddress = customer.EmailAddress,
            Address = customer.Address,
            Discount = customer.Discount,
            CustomerStatus = customer.CustomerStatus
        };

        return CreatedAtAction(nameof(GetCustomerById), new { id = customer.CustomerId }, response);
    }

    // ⭐ GET CUSTOMER BY ID
    [RequirePermission("customer:view")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerById(string id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound("Customer not found.");

        var response = new CustomerResponseDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            ContactNumber = customer.ContactNumber,
            EmailAddress = customer.EmailAddress,
            Address = customer.Address,
            Discount = customer.Discount,
            CustomerStatus = customer.CustomerStatus
        };

        return Ok(response);
    }

    // ⭐ GET ALL CUSTOMERS
    [RequirePermission("customer:view_all")]
    [HttpGet]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await _context.Customers
            .ToListAsync();

        var response = customers.Select(c => new CustomerResponseDto
        {
            CustomerId = c.CustomerId,
            CustomerName = c.CustomerName,
            ContactNumber = c.ContactNumber,
            EmailAddress = c.EmailAddress,
            Address = c.Address,
            Discount = c.Discount,
            CustomerStatus = c.CustomerStatus
        });

        return Ok(response);
    }

    // ⭐ UPDATE CUSTOMER — FIXED VERSION
    [RequirePermission("customer:update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(string id, CustomerUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound("Customer not found.");

        // Normalize input
        var newName = dto.CustomerName.Trim();
        var newPhone = dto.ContactNumber.Trim();
        var newEmail = dto.EmailAddress?.Trim();
        var newAddress = dto.Address?.Trim();

        // ⭐ FIX: Prevent false conflicts by using normalized comparison
        if (!string.Equals(newPhone, customer.ContactNumber.Trim(), StringComparison.Ordinal))
        {
            bool exists = await _context.Customers.AnyAsync(c =>
                c.CustomerId != id &&
                c.ContactNumber.Trim() == newPhone);

            if (exists)
                return Conflict("Another customer already has this phone number.");
        }

        // Apply updates
        customer.CustomerName = newName;
        customer.ContactNumber = newPhone;
        customer.EmailAddress = newEmail;
        customer.Address = newAddress;
        customer.Discount = dto.Discount;
        customer.CustomerStatus = dto.CustomerStatus;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ⭐ SOFT DELETE CUSTOMER
    [RequirePermission("customer:delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDeleteCustomer(string id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound("Customer not found.");

        customer.CustomerStatus = "Inactive";
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ⭐ RESTORE CUSTOMER
    [RequirePermission("customer:restore")]
    [HttpPost("restore/{id}")]
    public async Task<IActionResult> RestoreCustomer(string id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound("Customer not found.");

        customer.CustomerStatus = "Active";
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ⭐ SEARCH (name or phone)
    [RequirePermission("customer:search")]
    [HttpGet("search")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query is required");

        var customers = await _context.Customers
            .Where(c =>
                c.CustomerStatus == "Active" &&
                (c.CustomerName.Contains(query) ||
                 c.ContactNumber.Contains(query))
            )
            .Select(c => new CustomerSearchResultDto
            {
                CustomerId = c.CustomerId,
                CustomerName = c.CustomerName,
                ContactNumber = c.ContactNumber,
                Discount = c.Discount
            })
            .ToListAsync();

        return Ok(customers);
    }

    //TEST CUSTOMER CONTROLLER
    [RequirePermission("customer:view")]
    [HttpGet("test")]
    public IActionResult TestCustomerController()
    {
        return Ok("Customer controller is working");
    }
}
