using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.GoodsImportDocuments;


public sealed record CreateGoodsImportDocumentRequest(string ProformaNumber, string SupplierId, Incoterm Incoterm, string BrokerName, string DuaReference, DateTime? Date, IReadOnlyList<GoodsImportDocumentItemRequest> Items);


public sealed record GoodsImportDocumentItemRequest(string Code, int Amount);


public class CreateGoodsImportDocumentValidator : Validator<CreateGoodsImportDocumentRequest>
{
	public CreateGoodsImportDocumentValidator()
	{
		this.RuleFor(request => request.ProformaNumber).NotEmpty().WithMessage("proformaNumberRequired");
		this.RuleFor(request => request.SupplierId).NotEmpty().WithMessage("supplierRequired");
		this.RuleFor(request => request.Incoterm).IsInEnum().WithMessage("incotermInvalid");
		this.RuleFor(request => request.BrokerName).NotEmpty().WithMessage("brokerNameRequired");
		this.RuleFor(request => request.DuaReference).NotEmpty().WithMessage("duaReferenceRequired");
		this.RuleFor(request => request.Items).NotEmpty().WithMessage("itemsRequired");
		this.RuleForEach(request => request.Items).ChildRules(item =>
		{
			item.RuleFor(line => line.Code).NotEmpty().WithMessage("codeRequired");
			item.RuleFor(line => line.Amount).GreaterThan(0).WithMessage("amountNotPositive");
		});
	}
}


public sealed record DuplicateGoodsImportDocumentProformaNumberResponse(string ProformaNumber);


/// <remarks>
/// Posts a stock check-in for every line - a goods receipt from import, distinct from a domestic purchase -
/// then the usual landed-cost allocation (#120/#143) runs against the same stock items afterwards.
/// </remarks>
public class CreateGoodsImportDocumentEndpoint(IGoodsImportDocumentServiceProvider goodsImportDocumentServiceProvider, ISupplierServiceProvider supplierServiceProvider, IStockItemServiceProvider stockItemServiceProvider)
	: Endpoint<CreateGoodsImportDocumentRequest, Results<Created<GoodsImportDocumentResponse>, Conflict<DuplicateGoodsImportDocumentProformaNumberResponse>, NotFound>>
{
	public const string SupplierNotFound = "supplierNotFound";
	public const string StockItemNotFound = "stockItemNotFound";

	private readonly IGoodsImportDocumentServiceProvider _goodsImportDocumentServiceProvider = goodsImportDocumentServiceProvider;
	private readonly ISupplierServiceProvider _supplierServiceProvider = supplierServiceProvider;
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;


	public override void Configure()
	{
		this.Post("/goods-import-documents");
		this.Permissions(Permission.GoodsImportsWrite);
	}

	public override async Task<Results<Created<GoodsImportDocumentResponse>, Conflict<DuplicateGoodsImportDocumentProformaNumberResponse>, NotFound>> ExecuteAsync(CreateGoodsImportDocumentRequest request, CancellationToken cancellationToken)
	{
		if (await _supplierServiceProvider.GetSupplierByIdAsync(request.SupplierId, cancellationToken) is not Supplier supplier) return TypedResults.NotFound();

		List<GoodsImportDocumentItem> items = [];
		foreach (var line in request.Items)
		{
			if (await _stockItemServiceProvider.GetStockItemAsync(line.Code, cancellationToken) is not StockItem stockItem) return TypedResults.NotFound();
			items.Add(new GoodsImportDocumentItem(stockItem) { Amount = line.Amount });
		}

		var document = new GoodsImportDocument
		{
			ProformaNumber = request.ProformaNumber,
			Supplier = supplier,
			Incoterm = request.Incoterm,
			BrokerName = request.BrokerName,
			DuaReference = request.DuaReference,
			Date = request.Date ?? DateTime.Now,
			Items = items,
		};

		try
		{
			await _goodsImportDocumentServiceProvider.AddGoodsImportDocumentAsync(document, cancellationToken);
		}
		catch (GoodsImportDocumentProformaNumberAlreadyExistsException)
		{
			return TypedResults.Conflict(new DuplicateGoodsImportDocumentProformaNumberResponse(request.ProformaNumber));
		}

		return TypedResults.Created($"/api/goods-import-documents/{document.Id}", GoodsImportDocumentResponse.From(document));
	}
}
