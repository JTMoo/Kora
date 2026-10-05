using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Sifen;


public sealed record CancellationListResponse(IReadOnlyList<CancellationRequestResponse> Items);


/// <remarks>SIFEN Cancelación events requested so far (#206), pending or terminal</remarks>
public class ListCancellationsEndpoint(ICancellationRequestServiceProvider cancellationRequestServiceProvider) : EndpointWithoutRequest<CancellationListResponse>
{
	private readonly ICancellationRequestServiceProvider _cancellationRequestServiceProvider = cancellationRequestServiceProvider;


	public override void Configure()
	{
		this.Get("/sifen/cancellations");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<CancellationListResponse> ExecuteAsync(CancellationToken cancellationToken)
	{
		var requests = await _cancellationRequestServiceProvider.GetAllAsync(cancellationToken);
		return new(requests.Select(CancellationRequestResponse.From).ToList());
	}
}
