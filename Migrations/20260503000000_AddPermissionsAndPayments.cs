using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payment_Completed_At",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Payment_Method",
                table: "Sales");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    PermissionName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Module = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Endpoint = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    HttpMethod = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getutcdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Permissi__EFA6FB2F54018878", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Payment_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sales_ID = table.Column<long>(type: "bigint", nullable: false),
                    Payment_Method = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Payment_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Payment_Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Payment_ID);
                    table.ForeignKey(
                        name: "FK_Payments_Sales",
                        column: x => x.Sales_ID,
                        principalTable: "Sales",
                        principalColumn: "Sales_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePermissions",
                columns: table => new
                {
                    EmployeePermissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    PermissionId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getutcdate())"),
                    GrantedBy = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Employee__E736E75DED98206C", x => x.EmployeePermissionId);
                    table.ForeignKey(
                        name: "FK_EmployeePermissions_Employee",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Employee_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePermissions_Permission",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePermissions_EmployeeId",
                table: "EmployeePermissions",
                column: "EmployeeId",
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePermissions_PermissionId",
                table: "EmployeePermissions",
                column: "PermissionId",
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeePermission",
                table: "EmployeePermissions",
                columns: new[] { "EmployeeId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module",
                table: "Permissions",
                column: "Module",
                filter: "([IsActive]=(1))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmployeePermissions");
            migrationBuilder.DropTable(name: "Payments");
            migrationBuilder.DropTable(name: "Permissions");

            migrationBuilder.AddColumn<DateTime>(
                name: "Payment_Completed_At",
                table: "Sales",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payment_Method",
                table: "Sales",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);
        }
    }
}
