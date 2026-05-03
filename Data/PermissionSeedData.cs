namespace pharmacyPOS.API.Data;

/// <summary>
/// Canonical permission rows (id, display name, module, description, API route pattern, HTTP verb).
/// Kept in sync with <see cref="Authorization.RequirePermissionAttribute"/> usages and UI matrix.
/// </summary>
public static class PermissionSeedData
{
    public sealed record Row(
        string PermissionId,
        string PermissionName,
        string Module,
        string Description,
        string Endpoint,
        string HttpMethod);

    public static IReadOnlyList<Row> All { get; } = new Row[]
    {
        new("customer:create", "Create Customer", "Customer", "Allows creating a new customer", "/api/Customer", "POST"),
        new("customer:delete", "Delete Customer", "Customer", "Allows soft deleting a customer", "/api/Customer/{id}", "DELETE"),
        new("customer:restore", "Restore Customer", "Customer", "Allows restoring a deleted customer", "/api/Customer/restore/{id}", "POST"),
        new("customer:search", "Search Customers", "Customer", "Allows searching for customers", "/api/Customer/search", "GET"),
        new("customer:update", "Update Customer", "Customer", "Allows updating customer information", "/api/Customer/{id}", "PUT"),
        new("customer:view", "View Customer", "Customer", "Allows viewing customer details", "/api/Customer/{id}", "GET"),
        new("customer:view_all", "View All Customers", "Customer", "Allows viewing list of all customers", "/api/Customer", "GET"),

        new("employee:change_password", "Change Employee Password", "Employee", "Allows changing employee password", "/api/Employee/{id}/change-password", "POST"),
        new("employee:create", "Create Employee", "Employee", "Allows creating a new employee", "/api/Employee", "POST"),
        new("employee:delete", "Delete Employee", "Employee", "Allows soft deleting an employee", "/api/Employee/{id}", "DELETE"),
        new("employee:update", "Update Employee", "Employee", "Allows updating employee information", "/api/Employee/{id}", "PUT"),
        new("employee:view", "View Employee", "Employee", "Allows viewing employee details", "/api/Employee/{id}", "GET"),
        new("employee:view_all", "View All Employees", "Employee", "Allows viewing list of all employees", "/api/Employee", "GET"),
        new("employee:view_permissions", "View Employee Permissions", "Employee", "Allows viewing permissions assigned to an employee", "/api/Employee/{id}/with-permissions", "GET"),

        new("finance:view_reports", "View Finance Reports", "Finance", "Allows viewing financial reports and analytics", "/api/Finance/*", "GET"),

        new("glossary:create", "Create Glossary", "Glossary", "Allows creating a new glossary item", "/api/Glossary", "POST"),
        new("glossary:delete", "Delete Glossary", "Glossary", "Allows soft deleting a glossary item", "/api/Glossary/{id}", "DELETE"),
        new("glossary:restore", "Restore Glossary", "Glossary", "Allows restoring a deleted glossary item", "/api/Glossary/restore/{id}", "POST"),
        new("glossary:update", "Update Glossary", "Glossary", "Allows updating glossary information", "/api/Glossary/{id}", "PUT"),
        new("glossary:view", "View Glossary", "Glossary", "Allows viewing glossary details", "/api/Glossary/{id}", "GET"),
        new("glossary:view_deleted", "View Deleted Glossary", "Glossary", "Allows viewing soft-deleted glossary items", "/api/Glossary/deleted", "GET"),

        new("inventory:view_all", "View All Inventory", "Inventory", "Allows viewing all inventory items", "/api/Inventory/all", "GET"),
        new("inventory:view_details", "View Item Details", "Inventory", "Allows viewing detailed item information including stock", "/api/Inventory/ItemDetails/{sku}", "GET"),
        new("inventory:view_list", "View Inventory List", "Inventory", "Allows viewing list of inventory items", "/api/Inventory/list", "GET"),

        new("medicine:create", "Create Medicine", "Medicine", "Allows creating a new medicine", "/api/Medicine", "POST"),
        new("medicine:delete", "Delete Medicine", "Medicine", "Allows soft deleting a medicine", "/api/Medicine/{id}", "DELETE"),
        new("medicine:restore", "Restore Medicine", "Medicine", "Allows restoring a deleted medicine", "/api/Medicine/restore/{id}", "POST"), // API uses POST
        new("medicine:update", "Update Medicine", "Medicine", "Allows updating medicine information", "/api/Medicine/{id}", "PUT"),
        new("medicine:view", "View Medicine", "Medicine", "Allows viewing medicine details", "/api/Medicine/{id}", "GET"),
        new("medicine:view_all", "View All Medicines", "Medicine", "Allows viewing list of all medicines", "/api/Medicine", "GET"),
        new("medicine:view_deleted", "View Deleted Medicines", "Medicine", "Allows viewing soft-deleted medicines", "/api/Medicine/deleted", "GET"),
        new("medicine:view_summary", "View Medicine Summary", "Medicine", "Allows viewing medicine summary and statistics", "/api/Medicine/summary", "GET"),

        new("permission:assign", "Assign Permissions", "Permission", "Allows assigning new permissions to an employee", "/api/Permission/employee/{employeeId}/assign", "POST"),
        new("permission:remove", "Remove All Permissions", "Permission", "Allows removing all permissions from an employee", "/api/Permission/employee/{employeeId}/remove-all", "DELETE"),
        new("permission:update", "Update Employee Permissions", "Permission", "Allows updating (replacing) all permissions for an employee", "/api/Permission/employee/{employeeId}/update", "PUT"),
        new("permission:view", "View Employee Permissions", "Permission", "Allows viewing permissions assigned to a specific employee", "/api/Permission/employee/{employeeId}", "GET"),
        new("permission:view_all", "View All Permissions", "Permission", "Allows viewing all available permissions in the system", "/api/Permission/all", "GET"),

        new("purchasing:create", "Create Purchase", "Purchasing", "Allows creating a new purchase order", "/api/Purchasing", "POST"),
        new("purchasing:search_products", "Search Products for Purchase", "Purchasing", "Allows searching products when creating purchases", "/api/Purchasing/search", "GET"),
        new("purchasing:update_payment_status", "Update Payment Status", "Purchasing", "Allows updating payment status of a purchase", "/api/Purchasing/{id}/payment-status", "PUT"),
        new("purchasing:view", "View Purchase", "Purchasing", "Allows viewing purchase details", "/api/Purchasing/{id}", "GET"),
        new("purchasing:view_all", "View All Purchases", "Purchasing", "Allows viewing list of all purchases", "/api/Purchasing", "GET"),
        new("purchasing:view_summary", "View Purchase Summary", "Purchasing", "Allows viewing purchase summary and reports", "/api/Purchasing/summary", "GET"),

        new("sales:cancel_receipt", "Cancel Receipt", "Sales", "Allows canceling a sales receipt", "/api/Sales/cancel/{receiptNumber}", "PUT"),
        new("sales:complete_paylater", "Complete Pay Later", "Sales", "Allows completing payment for pay-later sales", "/api/Sales/paylater/complete/{receiptNumber}", "PUT"),
        new("sales:create_receipt", "Create Receipt", "Sales", "Allows creating a new sales receipt", "/api/Sales/create-receipt-with-items", "POST"),
        new("sales:finalize_sale", "Finalize Sale", "Sales", "Allows finalizing a draft sale", "/api/Sales/finalize/{receiptNumber}", "PUT"),
        new("sales:view_list", "View Sales List", "Sales", "Allows viewing list of all sales", "/api/Sales", "GET"),
        new("sales:view_paylater_list", "View Pay Later List", "Sales", "Allows viewing list of pay-later sales", "/api/Sales/paylater/list", "GET"),
        new("sales:view_paylater_receipt", "View Pay Later Receipt", "Sales", "Allows viewing a pay-later receipt", "/api/Sales/paylater/{receiptNumber}", "GET"),
        new("sales:view_receipt", "View Receipt", "Sales", "Allows viewing a sales receipt by receipt number", "/api/Sales/receipt/{receiptNumber}", "GET"),
        new("sales:view_summary", "View Sales Summary", "Sales", "Allows viewing sales summary and reports", "/api/Sales/summary", "GET"),

        new("supplier:create", "Create Supplier", "Supplier", "Allows creating a new supplier", "/api/Supplier", "POST"),
        new("supplier:search", "Search Suppliers", "Supplier", "Allows searching for suppliers", "/api/Supplier/search", "GET"),
        new("supplier:update", "Update Supplier", "Supplier", "Allows updating supplier information", "/api/Supplier/{id}", "PUT"),
        new("supplier:view", "View Supplier", "Supplier", "Allows viewing supplier details", "/api/Supplier/{id}", "GET"),
        new("supplier:view_all", "View All Suppliers", "Supplier", "Allows viewing list of all suppliers", "/api/Supplier", "GET"),
    };
}
