using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Sifen;


public sealed record NumberVoidListResponse(IReadOnlyList<NumberVoidResponse> Items);


/// <remarks>SIFEN Inutilización events requested so far (#206), pending or terminal</remarks>
public class ListNumberVoidsEndpoint(IInvoiceNumberVoidServiceProvider invoiceNumberVoidServiceProvider) : EndpointWithoutRequest<NumberVoidListResponse>
{
	private readonly IInvoiceNumberVoidServiceProvider _invoiceNumberVoidServiceProvider = invoiceNumberVoidServiceProvider;


	public override void Configure()
	{
		this.Get("/sifen/number-voids");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<NumberVoidListResponse> ExecuteAsync(CancellationToken cancellationToken)
	{
		var numberVoids = await _invoiceNumberVoidServiceProvider.GetAllAsync(cancellationToken);
		return new(numberVoids.Select(NumberVoidResponse.From).ToList());
	}
}
