# Database Connection Troubleshooting Guide
## Pharmacy Management System - SQL Server Connection Issues

---

## 🔍 Problem Analysis

You encountered this error:
```
Microsoft.Data.SqlClient.SqlException: A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible.
```

### **Root Causes Identified:**

1. **Connection String Format**: Using machine name instead of `localhost` can cause connection issues with Named Pipes
2. **DbContext Configuration Conflict**: Hardcoded connection string in `OnConfiguring` method could potentially override DI configuration
3. **SQL Server Instance Access**: Named Pipes connection might be blocked or not properly configured

---

## ✅ Fixes Applied

### **1. Fixed DbContext Configuration** (`Models/ThilankaPharmacyDbContext.cs`)

**Before:**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSqlServer("Server=localhost;Database=ThilankaPharmacyDB;...");
```

**After:**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Only configure if options are not already set (when using dependency injection)
    if (!optionsBuilder.IsConfigured)
    {
        // Fallback connection string - should not be used when DbContext is configured via DI
        optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=ThilankaPharmacyDB;...");
    }
}
```

**Why:** When using `AddDbContext` in `Program.cs`, options are pre-configured. The `OnConfiguring` method should only run as a fallback.

### **2. Updated Connection String** (`appsettings.json`)

**Before:**
```json
"DefaultConnection": "Server=DESKTOP-CMESGKD\\SQLEXPRESS;Database=ThilankaPharmacyDB;..."
```

**After:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ThilankaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

**Why:** Using `localhost` is more reliable than machine name, especially for local development.

---

## 🔧 Alternative Connection String Formats

If the current connection string doesn't work, try these alternatives:

### **Option 1: Using Period (.)**
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=ThilankaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

### **Option 2: Using (local)**
```json
"DefaultConnection": "Server=(local)\\SQLEXPRESS;Database=ThilankaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

### **Option 3: Using TCP/IP (If Named Pipes Fails)**
```json
"DefaultConnection": "Server=localhost,1433;Database=ThilankaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

### **Option 4: Using SQL Server Authentication (If Windows Auth Fails)**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ThilankaPharmacyDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

---

## 🧪 Testing the Connection

### **Method 1: Test via Command Line**
```powershell
# Test SQL Server connection
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

### **Method 2: Test via .NET**
Create a simple test console app or use this in your Program.cs temporarily:
```csharp
try
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();
    Console.WriteLine("✅ Database connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection failed: {ex.Message}");
}
```

### **Method 3: Use SQL Server Management Studio (SSMS)**
- Connect to: `localhost\SQLEXPRESS`
- Verify database `ThilankaPharmacyDB` exists

---

## 🚨 Common Issues & Solutions

### **Issue 1: SQL Server Service Not Running**
**Symptom:** "Cannot connect to SQL Server"

**Solution:**
```powershell
# Check SQL Server service status
Get-Service -Name "*SQL*"

# Start SQL Server if stopped
Start-Service -Name "MSSQL$SQLEXPRESS"
```

### **Issue 2: Named Pipes Disabled**
**Symptom:** Named Pipes Provider error

**Solution:**
1. Open SQL Server Configuration Manager
2. Go to "SQL Server Network Configuration" → "Protocols for SQLEXPRESS"
3. Enable "Named Pipes" and "TCP/IP"
4. Restart SQL Server service

### **Issue 3: SQL Server Browser Service Not Running**
**Symptom:** Cannot connect to named instance

**Solution:**
```powershell
# Start SQL Server Browser
Start-Service -Name "SQLBrowser"
```

### **Issue 4: Firewall Blocking Connection**
**Symptom:** Connection timeout

**Solution:**
- Allow SQL Server through Windows Firewall
- Or use `localhost` instead of machine name

### **Issue 5: Database Doesn't Exist**
**Symptom:** "Cannot open database"

**Solution:**
```powershell
# Create database using Entity Framework migrations
dotnet ef database update
```

### **Issue 6: Wrong Instance Name**
**Symptom:** "Instance not found"

**Solution:**
Check actual instance name:
```powershell
# List SQL Server instances
Get-Service -Name "*SQL*" | Where-Object {$_.Status -eq "Running"}
```

Common instance names:
- `SQLEXPRESS` (Express edition)
- `MSSQLSERVER` (Default instance)
- `MSSQLSERVER2019` (Version-specific)

---

## 📋 Verification Checklist

- [ ] SQL Server service is running (`MSSQL$SQLEXPRESS`)
- [ ] SQL Server Browser service is running (if using named instances)
- [ ] Connection string uses correct server name (`localhost\\SQLEXPRESS`)
- [ ] Database exists (or can be created via migrations)
- [ ] Windows Authentication is working (if using `Trusted_Connection=True`)
- [ ] Firewall allows SQL Server connections
- [ ] Named Pipes and/or TCP/IP protocols are enabled

---

## 🔄 Step-by-Step Recovery

1. **Verify SQL Server is Running:**
   ```powershell
   Get-Service -Name "MSSQL$SQLEXPRESS"
   ```

2. **Check Connection String in appsettings.json:**
   - Should use `localhost\\SQLEXPRESS`
   - Verify database name matches

3. **Test Connection:**
   ```powershell
   sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT 1"
   ```

4. **Apply Database Migrations:**
   ```powershell
   dotnet ef database update
   ```

5. **Run Application:**
   ```powershell
   dotnet run
   ```

---

## 🛠️ Advanced Troubleshooting

### **Enable SQL Server Logging**
Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### **Check SQL Server Error Logs**
```powershell
# Location: C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\Log\
Get-Content "C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\Log\ERRORLOG" -Tail 50
```

### **Use Connection String Builder**
```csharp
var builder = new SqlConnectionStringBuilder
{
    DataSource = "localhost\\SQLEXPRESS",
    InitialCatalog = "ThilankaPharmacyDB",
    IntegratedSecurity = true,
    TrustServerCertificate = true,
    MultipleActiveResultSets = true
};
var connectionString = builder.ConnectionString;
```

---

## 📞 Quick Reference

**Current Connection String Format:**
```
Server=localhost\SQLEXPRESS;Database=ThilankaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

**Components:**
- `Server=localhost\SQLEXPRESS` - Server and instance name
- `Database=ThilankaPharmacyDB` - Database name
- `Trusted_Connection=True` - Windows Authentication
- `TrustServerCertificate=True` - Bypass SSL certificate validation
- `MultipleActiveResultSets=True` - Allow multiple result sets

---

## ✅ Expected Behavior After Fix

After applying the fixes:
1. Application should start without connection errors
2. Database operations should work correctly
3. Migrations should apply successfully
4. API endpoints should respond properly

---

**Last Updated:** After fixing connection string conflicts
**Status:** Fixed - Connection string updated and DbContext properly configured


