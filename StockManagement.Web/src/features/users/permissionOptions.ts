import type { Permission } from "../../api";
import type { TextKey } from "../../i18n";

export const permissionOptions: { value: Permission; label: TextKey }[] = [
	{ value: "Customers.Read", label: "permCustomersRead" },
	{ value: "Customers.Write", label: "permCustomersWrite" },
	{ value: "StockItems.Read", label: "permStockItemsRead" },
	{ value: "StockItems.Write", label: "permStockItemsWrite" },
	{ value: "Sales.Read", label: "permSalesRead" },
	{ value: "Sales.Write", label: "permSalesWrite" },
	{ value: "Settings.Read", label: "permSettingsRead" },
	{ value: "Settings.Write", label: "permSettingsWrite" },
	{ value: "Suppliers.Read", label: "permSuppliersRead" },
	{ value: "Suppliers.Write", label: "permSuppliersWrite" },
	{ value: "Reports.Read", label: "permReportsRead" },
	{ value: "Users.Manage", label: "permUsersManage" }
];
