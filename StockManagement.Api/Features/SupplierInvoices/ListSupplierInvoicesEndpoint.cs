using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.SupplierInvoices;


public sealed record ListSupplierInvoicesRequest(string? SupplierId, string? Cursor, int? PageSize);


/// <param name="NextCursor">Opaque cursor for the next page; <see langword="null"/> on the last page</param>
public sealed record SupplierInvoiceListResponse(IReadOnlyList<SupplierInvoiceResponse> Items, string? NextCursor);


public class ListSupplierInvoicesEndpoint(ISupplierInvoiceServiceProvider supplierInvoiceServiceProvider, ISupplierPaymentService paymentService) : Endpoint<ListSupplierInvoicesRequest, SupplierInvoiceListResponse>
{
	private readonly ISupplierInvoiceServiceProvider _supplierInvoiceServiceProvider = supplierInvoiceServiceProvider;
	private readonly ISupplierPaymentService _paymentService = paymentService;


	public override void Configure()
	{
		this.Get("/supplier-invoices");
		this.Permissions(Permission.PayablesRead);
	}

	public override async Task<SupplierInvoiceListResponse> ExecuteAsync(ListSupplierInvoicesRequest request, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
		var result = await _supplierInvoiceServiceProvider.GetSupplierInvoicesAsync(request.SupplierId, request.Cursor, pageSize, cancellationToken);
		var items = result.Items.Select(invoice => SupplierInvoiceResponse.From(invoice, _paymentService)).ToList();
		return new(items, result.NextCursor);
	}
}
