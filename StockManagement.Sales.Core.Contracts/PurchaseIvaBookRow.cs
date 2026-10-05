namespace StockManagement.Sales.Core.Contracts;


/// <summary>
/// One row of the Hechauka purchases IVA book export (#186); counterpart to <see cref="IvaBookRow"/> (#123).
/// Layout is a best-effort approximation of DNIT's "Hechauka - Especificaciones Técnicas" spec for Form 211,
/// unverified (no network access to the DNIT document from this environment) - diff against the real file
/// before relying on it for filing.
/// </summary>
/// <remarks><see cref="Supplier"/> has no RUC field today; <see cref="SupplierRuc"/> is always empty until one is added.</remarks>
public sealed record PurchaseIvaBookRow(string DocumentTypeCode, string Number, DateTime Date, string SupplierRuc, string SupplierName,
	decimal Taxed10, decimal Vat10, decimal Taxed5, decimal Vat5, decimal Exempt, decimal Total);
