-- Manual script for AddProductBarcode (20260620120000_AddProductBarcode)
-- Run in SQL Server Management Studio / Azure Data Studio against your pharmacy database.

-- 1) Add Barcode column to Products (skip if already exists)
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Products')
      AND name = N'Barcode'
)
BEGIN
    ALTER TABLE dbo.Products
    ADD Barcode varchar(50) NULL;
END
GO

-- 2) Unique index: no two products may share the same non-null barcode
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Products')
      AND name = N'IX_Products_Barcode'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Products_Barcode
    ON dbo.Products (Barcode)
    WHERE Barcode IS NOT NULL;
END
GO

-- 3) Tell EF Core this migration was applied (optional but recommended)
IF NOT EXISTS (
    SELECT 1
    FROM dbo.[__EFMigrationsHistory]
    WHERE MigrationId = N'20260620120000_AddProductBarcode'
)
BEGIN
    INSERT INTO dbo.[__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES (N'20260620120000_AddProductBarcode', N'8.0.0');
END
GO

-- Verify
SELECT c.name AS ColumnName, t.name AS DataType, c.max_length, c.is_nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Products')
  AND c.name = N'Barcode';

SELECT name, is_unique, has_filter, filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Products')
  AND name = N'IX_Products_Barcode';
