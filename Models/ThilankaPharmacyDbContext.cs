using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace pharmacyPOS.API.Models;

public partial class SethuwaPharmacyDbContext : DbContext
{
    public SethuwaPharmacyDbContext()
    {
    }

    public SethuwaPharmacyDbContext(DbContextOptions<SethuwaPharmacyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerRecurrentItem> CustomerRecurrentItems { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Glossary> Glossaries { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Purchase> Purchases { get; set; }

    public virtual DbSet<PurchaseItem> PurchaseItems { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SalesItem> SalesItems { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<EmployeePermission> EmployeePermissions { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if options are not already set (when using dependency injection, options are pre-configured)
        if (!optionsBuilder.IsConfigured)
        {
            // Fallback connection string - should not be used when DbContext is configured via DI in Program.cs
            // This is only here for backwards compatibility or when using DbContext directly without DI
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=SethuwaPharmacyDB;Trusted_Connection=True;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__8CB286B9E2DDE106");

            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Customer_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Contact_Number");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Customer_Name");
            entity.Property(e => e.CustomerStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Customer_Status");
            entity.Property(e => e.Discount).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Email_Address");
        });

        modelBuilder.Entity<CustomerRecurrentItem>(entity =>
        {
            entity.HasKey(e => e.RecurrentId).HasName("PK__Customer__B535C77670592A73");

            entity.ToTable("Customer_Recurrent_Items");

            entity.Property(e => e.RecurrentId).HasColumnName("Recurrent_ID");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Customer_ID");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Product_SKU");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerRecurrentItems)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Customer___Custo__4AB81AF0");

            entity.HasOne(d => d.ProductSkuNavigation).WithMany(p => p.CustomerRecurrentItems)
                .HasForeignKey(d => d.ProductSku)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Customer___Produ__4BAC3F29");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7811348181A106AF");

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Employee_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Contact_Number");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Email_Address");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Employee_Name");
            entity.Property(e => e.EmployeeStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Employee_Status");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Password_Hash");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Glossary>(entity =>
        {
            entity.HasKey(e => e.GlossaryId).HasName("PK__Glossari__D5F2E44475609879");

            entity.HasIndex(e => e.Name, "UQ__Glossari__737584F61C326F38").IsUnique();

            entity.Property(e => e.GlossaryId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Glossary_ID");
            entity.Property(e => e.BrandName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Brand_Name");
            entity.Property(e => e.LowStockThreshold).HasColumnName("Low_Stock_Threshold");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__5F0102352C5A171D");

            entity.HasIndex(e => e.Name, "UQ__Medicine__737584F611D8362D").IsUnique();

            entity.Property(e => e.MedicineId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Medicine_ID");
            entity.Property(e => e.BrandName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Brand_Name");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GenericName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Generic_Name");
            entity.Property(e => e.LowStockThreshold).HasColumnName("Low_Stock_Threshold");
            entity.Property(e => e.Manufacture)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RequiredPrescription).HasColumnName("Required_Prescription");
            entity.Property(e => e.Strength)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductSku).HasName("PK__Products__5C44DD333C7A5A01");

            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Product_SKU");
            entity.Property(e => e.GlossaryId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Glossary_ID");
            entity.Property(e => e.MedicineId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Medicine_ID");
            entity.Property(e => e.ProductType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Product_Type");

            entity.HasOne(d => d.Glossary).WithMany(p => p.Products)
                .HasForeignKey(d => d.GlossaryId)
                .HasConstraintName("FK__Products__Glossa__4316F928");

            entity.HasOne(d => d.Medicine).WithMany(p => p.Products)
                .HasForeignKey(d => d.MedicineId)
                .HasConstraintName("FK__Products__Medici__4222D4EF");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__Purchase__543E6DA34F2568D7");

            entity.Property(e => e.PurchaseId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Purchase_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Created_At");
            entity.Property(e => e.InvoiceDate).HasColumnName("Invoice_Date");
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Invoice_Number");
            entity.Property(e => e.PaymentCompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("Payment_Completed_At");
            entity.Property(e => e.PaymentCompletedAt1)
                .HasColumnType("datetime")
                .HasColumnName("PaymentCompletedAt");
            entity.Property(e => e.PaymentDueDate).HasColumnName("Payment_Due_Date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Payment_Method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Payment_Status");
            entity.Property(e => e.SupplierId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Supplier_ID");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_Amount");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Purchases__Suppl__4F7CD00D");
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.PurchaseItemId).HasName("PK__Purchase__4CEA41E2080BD38C");

            entity.ToTable("Purchase_Items");

            entity.Property(e => e.PurchaseItemId).HasColumnName("Purchase_Item_ID");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Cost_Price");
            entity.Property(e => e.ExpireDate).HasColumnName("Expire_Date");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Product_SKU");
            entity.Property(e => e.PurchaseId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Purchase_ID");
            entity.Property(e => e.SellingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Selling_Price");

            entity.HasOne(d => d.ProductSkuNavigation).WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.ProductSku)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Purchase___Produ__534D60F1");

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Purchase___Purch__52593CB8");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SalesId).HasName("PK__Sales__32123EFAAD35C0BE");

            entity.Property(e => e.SalesId).HasColumnName("Sales_ID");
            entity.Property(e => e.BilledById)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Billed_By_ID");
            entity.Property(e => e.CustomerDiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Customer_Discount_Percent");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Customer_ID");
            entity.Property(e => e.FinalAmountDue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Final_Amount_Due");
            entity.Property(e => e.IssuedById)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Issued_By_ID");
            // PaymentMethod and PaymentCompletedAt columns removed - data now in Payments table
            entity.Ignore(e => e.PaymentMethod);
            entity.Ignore(e => e.PaymentCompletedAt);
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Receipt_Number");
            entity.Property(e => e.RoundingDiscount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Rounding_Discount");
            entity.Property(e => e.SaleStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Sale_Status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Total_Amount");

            entity.HasOne(d => d.BilledBy).WithMany(p => p.SaleBilledBies)
                .HasForeignKey(d => d.BilledById)
                .HasConstraintName("FK__Sales__Billed_By__5AEE82B9");

            entity.HasOne(d => d.Customer).WithMany(p => p.Sales)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Sales__Customer___5BE2A6F2");

            entity.HasOne(d => d.IssuedBy).WithMany(p => p.SaleIssuedBies)
                .HasForeignKey(d => d.IssuedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sales__Issued_By__59FA5E80");
        });

        modelBuilder.Entity<SalesItem>(entity =>
        {
            entity.HasKey(e => e.SalesItemId).HasName("PK__Sales_It__320F1BA37575F4B8");

            entity.ToTable("Sales_Items");

            entity.Property(e => e.SalesItemId).HasColumnName("Sales_Item_ID");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Product_SKU");
            entity.Property(e => e.SalesId).HasColumnName("Sales_ID");
            entity.Property(e => e.SellingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Selling_Price");
            entity.Property(e => e.SubTotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Sub_Total");

            entity.Property(e => e.StockId)
                .HasColumnName("Stock_ID");

            entity.HasOne(d => d.ProductSkuNavigation).WithMany(p => p.SalesItems)
                .HasForeignKey(d => d.ProductSku)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sales_Ite__Produ__5FB337D6");

            entity.HasOne(d => d.Sales).WithMany(p => p.SalesItems)
                .HasForeignKey(d => d.SalesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sales_Ite__Sales__5EBF139D");

            entity.HasOne(d => d.Stock).WithMany()
                .HasForeignKey(d => d.StockId)
                .HasConstraintName("FK_SalesItems_Stock");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK_Payments");

            entity.ToTable("Payments");

            entity.Property(e => e.PaymentId).HasColumnName("Payment_ID");
            entity.Property(e => e.SalesId).HasColumnName("Sales_ID");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Payment_Method");
            entity.Property(e => e.PaymentAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Payment_Amount");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("Payment_Date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("Created_At");

            entity.HasOne(d => d.Sale).WithMany(p => p.Payments)
                .HasForeignKey(d => d.SalesId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Payments_Sales");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.StockId).HasName("PK__Stock__EFA64EB85A10A757");

            entity.ToTable("Stock");

            entity.HasIndex(e => e.LotNumber, "UQ__Stock__B2FED760F263F927").IsUnique();

            entity.Property(e => e.StockId).HasColumnName("Stock_ID");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Cost_Price");
            entity.Property(e => e.ExpireDate).HasColumnName("Expire_Date");
            entity.Property(e => e.LotNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Lot_Number");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Product_SKU");
            entity.Property(e => e.QuantityOnHand).HasColumnName("Quantity_on_Hand");
            entity.Property(e => e.SellingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Selling_Price");

            entity.Property(e => e.SupplierId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Supplier_ID");

            entity.HasOne(d => d.ProductSkuNavigation).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.ProductSku)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Stock__Product_S__571DF1D5");

            entity.HasOne(d => d.Supplier).WithMany()
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Stock_Suppliers");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__83918D98532D7B48");

            entity.HasIndex(e => e.SupplierName, "UQ__Supplier__C919E828292C941E").IsUnique();

            entity.Property(e => e.SupplierId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Supplier_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.BankAccountName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Bank_Account_Name");
            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Bank_Account_Number");
            entity.Property(e => e.BankBranchName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Bank_Branch_Name");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Bank_Name");
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Contact_Number");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Contact_Person");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Email_Address");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Supplier_Name");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__Permissi__EFA6FB2F54018878");

            entity.HasIndex(e => e.Module, "IX_Permissions_Module").HasFilter("([IsActive]=(1))");

            entity.Property(e => e.PermissionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PermissionId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("CreatedAt");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Description");
            entity.Property(e => e.Endpoint)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Endpoint");
            entity.Property(e => e.HttpMethod)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("HttpMethod");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("IsActive");
            entity.Property(e => e.Module)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Module");
            entity.Property(e => e.PermissionName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PermissionName");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("UpdatedAt");
        });

        modelBuilder.Entity<EmployeePermission>(entity =>
        {
            entity.HasKey(e => e.EmployeePermissionId).HasName("PK__Employee__E736E75DED98206C");

            entity.HasIndex(e => e.EmployeeId, "IX_EmployeePermissions_EmployeeId").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.PermissionId, "IX_EmployeePermissions_PermissionId").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => new { e.EmployeeId, e.PermissionId }, "UQ_EmployeePermission").IsUnique();

            entity.Property(e => e.EmployeePermissionId)
                .HasColumnName("EmployeePermissionId");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EmployeeId");
            entity.Property(e => e.PermissionId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("PermissionId");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime")
                .HasColumnName("GrantedAt");
            entity.Property(e => e.GrantedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GrantedBy");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("IsActive");

            entity.HasOne(d => d.Employee)
                .WithMany(p => p.EmployeePermissions)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmployeePermissions_Employee");

            entity.HasOne(d => d.Permission)
                .WithMany(p => p.EmployeePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmployeePermissions_Permission");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
