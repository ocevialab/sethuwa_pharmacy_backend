using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF's DropTable/CreateTable sequence can make SqlServerMigrationsSqlGenerator emit
            // AlterColumn (IDENTITY) operations that SQL Server rejects. Use explicit T-SQL instead.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Stock', N'Selling_Price') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Stock] ADD [Selling_Price] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_Stock_Selling_Price_InitialSync] DEFAULT (0);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Purchase_Items', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Purchase_Items];
                IF OBJECT_ID(N'dbo.Purchases', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Purchases];
                """);

            migrationBuilder.Sql("""
                CREATE TABLE [dbo].[Purchases] (
                    [Purchase_ID] varchar(20) NOT NULL,
                    [Invoice_Number] varchar(50) NOT NULL,
                    [Invoice_Date] date NOT NULL,
                    [Payment_Status] varchar(50) NOT NULL,
                    [Payment_Due_Date] date NULL,
                    [Payment_Method] varchar(50) NULL,
                    [Payment_Completed_At] datetime NULL,
                    [PaymentCompletedAt] datetime NULL,
                    [Created_At] datetime NOT NULL CONSTRAINT [DF_Purchases_Created_At_InitialSync] DEFAULT (getdate()),
                    [Total_Amount] decimal(18,2) NOT NULL,
                    [Supplier_ID] varchar(50) NOT NULL,
                    CONSTRAINT [PK__Purchase__543E6DA34F2568D7] PRIMARY KEY ([Purchase_ID]),
                    CONSTRAINT [FK__Purchases__Suppl__4F7CD00D] FOREIGN KEY ([Supplier_ID])
                        REFERENCES [dbo].[Suppliers] ([Supplier_ID])
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE [dbo].[Purchase_Items] (
                    [Purchase_Item_ID] bigint NOT NULL IDENTITY(1,1),
                    [Cost_Price] decimal(18,2) NOT NULL,
                    [Selling_Price] decimal(18,2) NOT NULL,
                    [Quantity] int NOT NULL,
                    [Expire_Date] date NOT NULL,
                    [Purchase_ID] varchar(20) NOT NULL,
                    [Product_SKU] varchar(50) NOT NULL,
                    CONSTRAINT [PK__Purchase__4CEA41E2080BD38C] PRIMARY KEY ([Purchase_Item_ID]),
                    CONSTRAINT [FK__Purchase___Produ__534D60F1] FOREIGN KEY ([Product_SKU])
                        REFERENCES [dbo].[Products] ([Product_SKU]),
                    CONSTRAINT [FK__Purchase___Purch__52593CB8] FOREIGN KEY ([Purchase_ID])
                        REFERENCES [dbo].[Purchases] ([Purchase_ID])
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX [IX_Purchase_Items_Product_SKU] ON [dbo].[Purchase_Items] ([Product_SKU]);
                CREATE INDEX [IX_Purchase_Items_Purchase_ID] ON [dbo].[Purchase_Items] ([Purchase_ID]);
                CREATE INDEX [IX_Purchases_Supplier_ID] ON [dbo].[Purchases] ([Supplier_ID]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Stock', N'Selling_Price') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Stock] DROP CONSTRAINT [DF_Stock_Selling_Price_InitialSync];
                    ALTER TABLE [dbo].[Stock] DROP COLUMN [Selling_Price];
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Purchase_Items', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Purchase_Items];
                IF OBJECT_ID(N'dbo.Purchases', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Purchases];
                """);

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Purchase_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Invoice_Number = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Invoice_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Payment_Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Payment_Due_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Payment_Method = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Payment_Completed_At = table.Column<DateTime>(type: "datetime", nullable: true),
                    Created_At = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Total_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Supplier_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Purchase__543E6DA34F2568D7", x => x.Purchase_ID);
                    table.ForeignKey(
                        name: "FK__Purchases__Suppl__4F7CD00D",
                        column: x => x.Supplier_ID,
                        principalTable: "Suppliers",
                        principalColumn: "Supplier_ID");
                });

            migrationBuilder.CreateTable(
                name: "Purchase_Items",
                columns: table => new
                {
                    Purchase_Item_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cost_Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Selling_Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Expire_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Purchase_ID = table.Column<long>(type: "bigint", nullable: false),
                    Product_SKU = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Purchase__4CEA41E2080BD38C", x => x.Purchase_Item_ID);
                    table.ForeignKey(
                        name: "FK__Purchase___Produ__534D60F1",
                        column: x => x.Product_SKU,
                        principalTable: "Products",
                        principalColumn: "Product_SKU");
                    table.ForeignKey(
                        name: "FK__Purchase___Purch__52593CB8",
                        column: x => x.Purchase_ID,
                        principalTable: "Purchases",
                        principalColumn: "Purchase_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_Items_Product_SKU",
                table: "Purchase_Items",
                column: "Product_SKU");

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_Items_Purchase_ID",
                table: "Purchase_Items",
                column: "Purchase_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Supplier_ID",
                table: "Purchases",
                column: "Supplier_ID");
        }
    }
}
