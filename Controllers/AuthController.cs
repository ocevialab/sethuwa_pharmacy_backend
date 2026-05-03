using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using pharmacyPOS.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SethsuwaPharmacyDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(SethsuwaPharmacyDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);

        if (employee == null)
            return Unauthorized("Invalid EmployeeId");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
            return Unauthorized("Invalid Password");

        if (employee.EmployeeStatus != "ACTIVE")
            return Unauthorized("Employee is inactive.");

        var token = GenerateJwt(employee);

        return Ok(new LoginResponse
        {
            Token = token,
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            Role = employee.Role
        });
    }

    private string GenerateJwt(Employee employee)
    {
        var claims = new[]
        {
            new Claim("EmployeeId", employee.EmployeeId),
            new Claim(ClaimTypes.Role, employee.Role),
            new Claim(ClaimTypes.Name, employee.EmployeeName),
            new Claim("Status", employee.EmployeeStatus)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                Convert.ToDouble(_config["Jwt:ExpiresInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
