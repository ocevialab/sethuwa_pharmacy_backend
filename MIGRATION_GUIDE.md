# Entity Framework Core Migration Guide
## Pharmacy Management System - Backend API

---

## 📋 Project Analysis

### **Project Overview**
- **Framework**: ASP.NET Core 9.0 (Web API)
- **Database**: SQL Server
- **ORM**: Entity Framework Core 8.0.0
- **DbContext**: `ThilankaPharmacyDbContext`
- **Namespace**: `pharmacyPOS.API.Models`

### **Key Components**

#### **Database Context**
- **File**: `Models/ThilankaPharmacyDbContext.cs`
- **Connection String**: Configured in `appsettings.json` under `ConnectionStrings:DefaultConnection`
- **Current Database**: `ThilankaPharmacyDB`

#### **Entity Models**
The project contains the following entities:
- `Customer` - Customer information
- `CustomerRecurrentItem` - Recurring items for customers
- `Employee` - Employee/User accounts
- `Glossary` - Product glossary entries
- `Medicine` - Medicine master data
- `Product` - Products (can be Medicine or Glossary based)
- `Purchase` - Purchase orders
- `PurchaseItem` - Purchase line items
- `Sale` - Sales transactions
- `SalesItem` - Sales line items
- `Stock` - Inventory stock levels
- `Supplier` - Supplier information
- `StockMovement` - Stock movement history

#### **Existing Migrations**
The project already has migrations:
- `20251121070637_AddSoftDeleteColumns.cs`
- `20251123030642_AddPaymentCompletedAtColumn.cs`
- `20251210055134_InitialSync.cs`

---

## 🚀 How to Create Migrations

### **Prerequisites**
1. **SQL Server** must be installed and running
2. **Connection String** must be correctly configured in `appsettings.json`
3. **Entity Framework Core Tools** are already installed (included in `.csproj`)

### **Step-by-Step Guide**

#### **Step 1: Verify Connection String**
Check your `appsettings.json` file:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ThilankaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

#### **Step 2: Make Changes to Models**
Before creating a migration, make changes to your entity models in the `Models/` folder:
- Add new properties
- Modify existing properties
- Add new entities
- Remove entities
- Change relationships

**Example**: Adding a new property to `Customer` model:
```csharp
public class Customer
{
    // ... existing properties
    public string? Notes { get; set; }  // New property
}
```

#### **Step 3: Create Migration**
Open **PowerShell** or **Command Prompt** in the project root directory and run:

```powershell
dotnet ef migrations add MigrationName --project pharmacyPOS.API.csproj
```

**Replace `MigrationName`** with a descriptive name, for example:
- `AddNotesToCustomer`
- `AddStockMovementTable`
- `UpdateEmployeeSchema`

**Full Command Example**:
```powershell
dotnet ef migrations add AddNotesToCustomer
```

#### **Step 4: Review Generated Migration**
After creating the migration, review the generated file in the `Migrations/` folder:
- **File Format**: `YYYYMMDDHHMMSS_MigrationName.cs`
- Check the `Up()` method to ensure it contains the correct changes
- Check the `Down()` method to ensure rollback is correct

#### **Step 5: Apply Migration to Database**
Apply the migration to update your database:

```powershell
dotnet ef database update --project pharmacyPOS.API.csproj
```

Or apply a specific migration:
```powershell
dotnet ef database update MigrationName --project pharmacyPOS.API.csproj
```

---

## 📝 Common Migration Scenarios

### **Scenario 1: Adding a New Column**
```csharp
// In your model (e.g., Customer.cs)
public string? EmailAddress { get; set; }
```

**Migration Command**:
```powershell
dotnet ef migrations add AddEmailToCustomer
dotnet ef database update
```

### **Scenario 2: Adding a New Entity**
1. Create new model class in `Models/` folder
2. Add `DbSet<T>` to `ThilankaPharmacyDbContext.cs`
3. Configure entity in `OnModelCreating()` method

**Migration Command**:
```powershell
dotnet ef migrations add AddNewEntityTable
dotnet ef database update
```

### **Scenario 3: Modifying Existing Column**
```csharp
// Change property type or constraints
public decimal? Discount { get; set; }  // Changed from non-nullable to nullable
```

**Migration Command**:
```powershell
dotnet ef migrations add ModifyCustomerDiscount
dotnet ef database update
```

### **Scenario 4: Removing a Column**
1. Remove property from model
2. Create migration
3. Review and apply

**Migration Command**:
```powershell
dotnet ef migrations add RemoveColumnFromEntity
dotnet ef database update
```

### **Scenario 5: Adding Foreign Key Relationship**
```csharp
// In your model
public string? SupplierId { get; set; }
public Supplier? Supplier { get; set; }
```

**Migration Command**:
```powershell
dotnet ef migrations add AddSupplierForeignKey
dotnet ef database update
```

---

## 🔧 Advanced Commands

### **List All Migrations**
```powershell
dotnet ef migrations list
```

### **Remove Last Migration** (if not applied)
```powershell
dotnet ef migrations remove
```

### **Generate SQL Script** (without applying)
```powershell
dotnet ef migrations script
```

### **Generate SQL Script for Specific Migration**
```powershell
dotnet ef migrations script FromMigration ToMigration
```

### **Rollback to Specific Migration**
```powershell
dotnet ef database update PreviousMigrationName
```

### **Drop Entire Database** (⚠️ Use with caution)
```powershell
dotnet ef database drop
```

---

## ⚠️ Important Notes

### **1. Always Review Migrations**
- Check generated migration files before applying
- Ensure `Up()` and `Down()` methods are correct
- Verify data type changes won't cause data loss

### **2. Backup Database**
- Always backup your database before applying migrations in production
- Test migrations in development environment first

### **3. Migration Naming Convention**
- Use descriptive names: `AddColumnNameToTableName`
- Use PascalCase
- Be specific about what the migration does

### **4. Connection String**
- The connection string in `appsettings.json` is used during migration
- Ensure it points to the correct database
- For different environments, use `appsettings.Development.json` or environment variables

### **5. Model Changes**
- Changes to models must be reflected in `OnModelCreating()` if needed
- Foreign keys and relationships must be properly configured

---

## 🐛 Troubleshooting

### **Error: "No DbContext was found"**
**Solution**: Ensure you're in the project root directory and specify the project:
```powershell
dotnet ef migrations add MigrationName --project pharmacyPOS.API.csproj
```

### **Error: "Cannot connect to database"**
**Solution**: 
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure database exists or can be created

### **Error: "Migration already exists"**
**Solution**: 
1. Remove the migration if not applied: `dotnet ef migrations remove`
2. Or use a different migration name

### **Error: "Pending model changes"**
**Solution**: 
1. Apply pending migrations: `dotnet ef database update`
2. Then create new migration

### **Error: "Column already exists"**
**Solution**: 
1. Check if migration was partially applied
2. Manually fix the database state
3. Or remove and recreate the migration

---

## 📚 Additional Resources

- **Entity Framework Core Documentation**: https://learn.microsoft.com/en-us/ef/core/
- **Migrations Overview**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **EF Core Tools**: https://learn.microsoft.com/en-us/ef/core/cli/dotnet

---

## 📋 Quick Reference Commands

```powershell
# Create new migration
dotnet ef migrations add MigrationName

# Apply migration to database
dotnet ef database update

# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

---

## ✅ Best Practices

1. **One Migration Per Feature**: Create separate migrations for different features
2. **Descriptive Names**: Use clear, descriptive migration names
3. **Test First**: Always test migrations in development
4. **Review Changes**: Always review generated migration code
5. **Version Control**: Commit migration files to source control
6. **Backup**: Backup database before applying migrations
7. **Documentation**: Document complex migrations in code comments

---

**Last Updated**: Based on project structure analysis
**Project**: Pharmacy Management System - Backend API
**Framework**: .NET 9.0 with Entity Framework Core 8.0.0

