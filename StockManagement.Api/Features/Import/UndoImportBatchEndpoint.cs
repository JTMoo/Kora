using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Import.Core.Contracts;
using StockManagement.Kernel.Exceptions;

namespace StockManagement.Api.Features.Import;


public sealed record UndoImportBatchRequest(string Id);


/// <remarks>Removes the entities a commit created; a batch that is not Committed (still Previewed, or already undone) is a conflict.</remarks>
public class UndoImportBatchEndpoint(IImportBatchService importBatchService)
	: Endpoint<UndoImportBatchRequest, Results<Ok<ImportBatchResponse>, NotFound, Conflict<ImportBatchStatusConflictResponse>, ForbidHttpResult>>
{
	private readonly IImportBatchService _importBatchService = importBatchService;


	public override void Configure()
	{
		this.Post("/import/batches/{Id}/undo");
		this.Permissions(Permission.StockItemsWrite, Permission.CustomersWrite, Permission.SalesWrite);
	}

	public override async Task<Results<Ok<ImportBatchResponse>, NotFound, Conflict<ImportBatchStatusConflictResponse>, ForbidHttpResult>> ExecuteAsync(UndoImportBatchRequest request, CancellationToken cancellationToken)
	{
		var batch = await _importBatchService.GetAsync(request.Id, cancellationToken);
		if (batch is null) return TypedResults.NotFound();

		var required = Permission.RequiredForImportTarget(batch.Target);
		if (!this.User.Claims.Any(claim => claim.Type == "permissions" && claim.Value == required))
		{
			return TypedResults.Forbid();
		}

		try
		{
			var undone = await _importBatchService.UndoAsync(request.Id, cancellationToken);
			return TypedResults.Ok(ImportBatchResponse.From(undone));
		}
		catch (ImportBatchNotFoundException)
		{
			return TypedResults.NotFound();
		}
		catch (InvalidImportBatchStatusException ex)
		{
			return TypedResults.Conflict(new ImportBatchStatusConflictResponse(ex.Message));
		}
	}
}
