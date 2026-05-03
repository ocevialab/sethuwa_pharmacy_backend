using System;
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
            migrationBuilder.AddColumn<decimal>(
                name: "Selling_Price",
                table: "Stock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // SQL Server cannot ALTER an IDENTITY column. Drop and recreate Purchase_Items
            // (references Purchases) then Purchases with varchar(20) PK.
            migrationBuilder.DropTable(name: "Purchase_Items");
            migrationBuilder.DropTable(name: "Purchases");

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Purchase_ID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Invoice_Number = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Invoice_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Payment_Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Payment_Due_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Payment_Method = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Payment_Completed_At = table.Column<DateTime>(type: "datetime", nullable: true),
                    PaymentCompletedAt = table.Column<DateTime>(type: "datetime", nullable: true),
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
                    Purchase_ID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Selling_Price",
                table: "Stock");

            migrationBuilder.DropTable(name: "Purchase_Items");
            migrationBuilder.DropTable(name: "Purchases");

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
