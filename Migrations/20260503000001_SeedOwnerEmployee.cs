using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedOwnerEmployee : Migration
    {
        private const string EmployeeId = "EMP-1";
        // Default password: Admin@1234
        private const string PasswordHash = "$2a$11$RV2BLPwe7/46rGXv5O80GuuIFm0WelU/kGFgKCCo/HUJ1F2.0G5pi";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert OWNER employee
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Employees WHERE Employee_ID = '{EmployeeId}')
                INSERT INTO Employees (Employee_ID, Employee_Name, Role, Contact_Number, Employee_Status, Password_Hash)
                VALUES ('{EmployeeId}', 'Owner', 'OWNER', '0000000000', 'ACTIVE', '{PasswordHash}');
            ");

            // Insert all permissions
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionId = 'customer:create')
                INSERT INTO Permissions (PermissionId, PermissionName, Module, IsActive) VALUES
                  ('customer:create',              'Create Customer',             'customer',   1),
                  ('customer:view',                'View Customer',               'customer',   1),
                  ('customer:view_all',            'View All Customers',          'customer',   1),
                  ('customer:update',              'Update Customer',             'customer',   1),
                  ('customer:delete',              'Delete Customer',             'customer',   1),
                  ('customer:restore',             'Restore Customer',            'customer',   1),
                  ('customer:search',              'Search Customers',            'customer',   1),
                  ('employee:create',              'Create Employee',             'employee',   1),
                  ('employee:update',              'Update Employee',             'employee',   1),
                  ('employee:delete',              'Delete Employee',             'employee',   1),
                  ('employee:view',                'View Employee',               'employee',   1),
                  ('employee:view_all',            'View All Employees',          'employee',   1),
                  ('employee:view_permissions',    'View Employee Permissions',   'employee',   1),
                  ('employee:change_password',     'Change Employee Password',    'employee',   1),
                  ('supplier:create',              'Create Supplier',             'supplier',   1),
                  ('supplier:update',              'Update Supplier',             'supplier',   1),
                  ('supplier:view',                'View Supplier',               'supplier',   1),
                  ('supplier:search',              'Search Suppliers',            'supplier',   1),
                  ('supplier:view_all',            'View All Suppliers',          'supplier',   1),
                  ('permission:view',              'View Permission',             'permission', 1),
                  ('permission:assign',            'Assign Permission',           'permission', 1),
                  ('permission:update',            'Update Permission',           'permission', 1),
                  ('permission:remove',            'Remove Permission',           'permission', 1),
                  ('permission:view_all',          'View All Permissions',        'permission', 1),
                  ('inventory:view_details',       'View Inventory Details',      'inventory',  1),
                  ('inventory:view_list',          'View Inventory List',         'inventory',  1),
                  ('inventory:view_all',           'View All Inventory',          'inventory',  1),
                  ('medicine:create',              'Create Medicine',             'medicine',   1),
                  ('medicine:update',              'Update Medicine',             'medicine',   1),
                  ('medicine:delete',              'Delete Medicine',             'medicine',   1),
                  ('medicine:view',                'View Medicine',               'medicine',   1),
                  ('medicine:view_all',            'View All Medicines',          'medicine',   1),
                  ('medicine:view_deleted',        'View Deleted Medicines',      'medicine',   1),
                  ('medicine:restore',             'Restore Medicine',            'medicine',   1),
                  ('medicine:view_summary',        'View Medicine Summary',       'medicine',   1),
                  ('glossary:create',              'Create Glossary',             'glossary',   1),
                  ('glossary:view',                'View Glossary',               'glossary',   1),
                  ('glossary:update',              'Update Glossary',             'glossary',   1),
                  ('glossary:delete',              'Delete Glossary',             'glossary',   1),
                  ('glossary:restore',             'Restore Glossary',            'glossary',   1),
                  ('glossary:view_deleted',        'View Deleted Glossaries',     'glossary',   1),
                  ('sales:create_receipt',         'Create Receipt',              'sales',      1),
                  ('sales:view_receipt',           'View Receipt',                'sales',      1),
                  ('sales:finalize_sale',          'Finalize Sale',               'sales',      1),
                  ('sales:complete_paylater',      'Complete Pay Later',          'sales',      1),
                  ('sales:view_paylater_list',     'View Pay Later List',         'sales',      1),
                  ('sales:view_paylater_receipt',  'View Pay Later Receipt',      'sales',      1),
                  ('sales:cancel_receipt',         'Cancel Receipt',              'sales',      1),
                  ('sales:view_summary',           'View Sales Summary',          'sales',      1),
                  ('sales:view_list',              'View Sales List',             'sales',      1),
                  ('purchasing:create',            'Create Purchase',             'purchasing', 1),
                  ('purchasing:view',              'View Purchase',               'purchasing', 1),
                  ('purchasing:view_all',          'View All Purchases',          'purchasing', 1),
                  ('purchasing:view_summary',      'View Purchasing Summary',     'purchasing', 1),
                  ('purchasing:search_products',   'Search Products for Purchase','purchasing', 1),
                  ('purchasing:update_payment_status', 'Update Payment Status',  'purchasing', 1),
                  ('finance:view_reports',         'View Finance Reports',        'finance',    1);
            ");

            // Assign all permissions to EMP-1
            migrationBuilder.Sql($@"
                INSERT INTO EmployeePermissions (EmployeeId, PermissionId, GrantedBy, IsActive)
                SELECT '{EmployeeId}', PermissionId, '{EmployeeId}', 1
                FROM Permissions
                WHERE IsActive = 1
                  AND NOT EXISTS (
                    SELECT 1 FROM EmployeePermissions ep
                    WHERE ep.EmployeeId = '{EmployeeId}' AND ep.PermissionId = Permissions.PermissionId
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM EmployeePermissions WHERE EmployeeId = '{EmployeeId}';");
            migrationBuilder.Sql(@"
                DELETE FROM Permissions WHERE PermissionId IN (
                  'customer:create','customer:view','customer:view_all','customer:update',
                  'customer:delete','customer:restore','customer:search',
                  'employee:create','employee:update','employee:delete','employee:view',
                  'employee:view_all','employee:view_permissions','employee:change_password',
                  'supplier:create','supplier:update','supplier:view','supplier:search','supplier:view_all',
                  'permission:view','permission:assign','permission:update','permission:remove','permission:view_all',
                  'inventory:view_details','inventory:view_list','inventory:view_all',
                  'medicine:create','medicine:update','medicine:delete','medicine:view','medicine:view_all',
                  'medicine:view_deleted','medicine:restore','medicine:view_summary',
                  'glossary:create','glossary:view','glossary:update','glossary:delete',
                  'glossary:restore','glossary:view_deleted',
                  'sales:create_receipt','sales:view_receipt','sales:finalize_sale','sales:complete_paylater',
                  'sales:view_paylater_list','sales:view_paylater_receipt','sales:cancel_receipt',
                  'sales:view_summary','sales:view_list',
                  'purchasing:create','purchasing:view','purchasing:view_all','purchasing:view_summary',
                  'purchasing:search_products','purchasing:update_payment_status',
                  'finance:view_reports'
                );
            ");
            migrationBuilder.Sql($"DELETE FROM Employees WHERE Employee_ID = '{EmployeeId}';");
        }
    }
}
