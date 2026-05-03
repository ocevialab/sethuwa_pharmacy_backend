using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Authorization;
using pharmacyPOS.API.Data;
using Serilog;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Reverse proxy (nginx): TLS termination, correct scheme for redirects and links
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Controllers
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pharmacy Management System API",
        Version = "v1",
        Description = "API for Pharmacy Management System - Sethsuwa Pharmacy POS",
        Contact = new OpenApiContact
        {
            Name = "Pharmacy Management System",
            Email = "support@pharmacypos.com"
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token (without 'Bearer' prefix) in the text input below.\n\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SethsuwaPharmacyDbContext>(options =>
    options.UseSqlServer(connectionString));

//loging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/app_log_.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30, // Keep only last 30 days
                                    // buffered: true,
        shared: true
    )
    .CreateLogger();

builder.Host.UseSerilog();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.")))
            // ClockSkew defaults to 5 minutes, which is fine
        };

        // Add event handlers for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
                if (context.Exception is SecurityTokenExpiredException)
                {
                    Console.WriteLine("Token has expired");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"JWT Challenge error: {context.Error} - {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

// Authorization Configuration
builder.Services.AddAuthorization();

// Permission-based Authorization
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174",
                "https://pharmacy-management-system-66cp.vercel.app",
                "https://www.pharmacy-management-system-66cp.vercel.app",
                "https://pharmacy-management-system-7w4u.vercel.app",
                "https://ocevialabphramacy.netlify.app",
                "https://www.ocevialabphramacy.netlify.app",
                "https://pharmacy.ocevialab.com",
                "https://www.pharmacy.ocevialab.com",
                "https://sethsuwa-phama.ocevialab.com",
                "http://sethsuwa-phama.ocevialab.com",
                "http://sethsuwa-phama-qa.ocevialab.com",
                "https://sethsuwa-phama-qa.ocevialab.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction() || app.Environment.IsEnvironment("QA"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmacy Management System API v1");
        c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
    });
}

// CORS must be configured BEFORE UseAuthentication and UseAuthorization
app.UseCors("AllowFrontend");

// Behind reverse proxy Kestrel is often HTTP-only. Redirecting OPTIONS breaks CORS preflight
// when X-Forwarded-Proto is missing on that hop.
app.UseWhen(
    static ctx => !HttpMethods.IsOptions(ctx.Request.Method),
    static branch => branch.UseHttpsRedirection());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsProduction() || app.Environment.IsEnvironment("QA"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<SethsuwaPharmacyDbContext>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DeploymentSeeder");
    await DeploymentSeeder.SeedAsync(db, seedLogger);
}

await app.RunAsync();
