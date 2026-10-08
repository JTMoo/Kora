using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Auth.Core.Contracts;


/// <summary>
/// Per-module Read/Write permission strings put on the JWT and checked with FastEndpoints' <c>Permissions()</c>. See ADR-0017.
/// </summary>
public static class Permission
{
	public const string UsersManage = "Users.Manage";
	public const string CustomersRead = "Customers.Read";
	public const string CustomersWrite = "Customers.Write";
	public const string StockItemsRead = "StockItems.Read";
	public const string StockItemsWrite = "StockItems.Write";
	public const string SalesRead = "Sales.Read";
	public const string SalesWrite = "Sales.Write";
	public const string SettingsRead = "Settings.Read";
	public const string SettingsWrite = "Settings.Write";
	public const string SuppliersRead = "Suppliers.Read";
	public const string SuppliersWrite = "Suppliers.Write";
	public const string GoodsImportsRead = "GoodsImports.Read";
	public const string GoodsImportsWrite = "GoodsImports.Write";
	public const string ReportsRead = "Reports.Read";
	public const string PayablesRead = "Payables.Read";
	public const string PayablesWrite = "Payables.Write";
	public const string CashRegisterRead = "CashRegister.Read";
	public const string CashRegisterWrite = "CashRegister.Write";

	public static readonly IReadOnlyList<string> CatalogAll =
	[
		UsersManage,
		CustomersRead, CustomersWrite,
		StockItemsRead, StockItemsWrite,
		SalesRead, SalesWrite,
		SettingsRead, SettingsWrite,
		SuppliersRead, SuppliersWrite,
		GoodsImportsRead, GoodsImportsWrite,
		ReportsRead,
		PayablesRead, PayablesWrite,
		CashRegisterRead, CashRegisterWrite
	];

	/// <returns>Every permission for <see cref="UserRole.Admin"/>, otherwise <paramref name="user"/>'s stored <see cref="User.Permissions"/></returns>
	public static IReadOnlyList<string> Effective(User user)
	{
		return user.Role == UserRole.Admin ? CatalogAll : user.Permissions;
	}

	/// <returns>The write permission required to import into <paramref name="target"/></returns>
	public static string RequiredForImportTarget(ImportTarget target)
	{
		return target switch
		{
			ImportTarget.StockItems or ImportTarget.OpeningStock => StockItemsWrite,
			ImportTarget.Customers => CustomersWrite,
			ImportTarget.OpenInvoices => SalesWrite,
			_ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
		};
	}
}
