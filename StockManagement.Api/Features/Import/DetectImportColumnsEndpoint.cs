using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Import.Core.Contracts;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.Import;


public sealed class DetectImportColumnsRequest
{
	public ImportTarget Target { get; set; }

	public IFormFile File { get; set; } = default!;
}


public class DetectImportColumnsValidator : Validator<DetectImportColumnsRequest>
{
	public DetectImportColumnsValidator()
	{
		this.RuleFor(request => request.File).Must(file => file is { Length: > 0 }).WithMessage("fileRequired");
		this.RuleFor(request => request.File).Must(ImportFileValidation.IsAllowedSize).WithMessage("fileTooLarge");
		this.RuleFor(request => request.File).Must(ImportFileValidation.IsAllowedType).WithMessage("fileTypeNotAllowed");
	}
}


public sealed record DetectedColumnsResponse(string SheetName, IReadOnlyList<DetectedColumn> Columns, IReadOnlyList<ImportField> Fields);


/// <remarks>Reads only the header row, for the web column-mapping step ahead of preview (docs/adr/0028-import-column-mapping.md).</remarks>
public class DetectImportColumnsEndpoint(IImportBatchService importBatchService, ILogger<DetectImportColumnsEndpoint> logger)
	: Endpoint<DetectImportColumnsRequest, Results<Ok<DetectedColumnsResponse>, BadRequest<InvalidExcelFileResponse>>>
{
	private readonly IImportBatchService _importBatchService = importBatchService;
	private readonly ILogger<DetectImportColumnsEndpoint> _logger = logger;


	public override void Configure()
	{
		this.Post("/import/batches/columns");
		this.AllowFileUploads();
		this.Permissions(Permission.StockItemsWrite, Permission.CustomersWrite, Permission.SalesWrite);
	}

	public override async Task<Results<Ok<DetectedColumnsResponse>, BadRequest<InvalidExcelFileResponse>>> ExecuteAsync(DetectImportColumnsRequest request, CancellationToken cancellationToken)
	{
		await using var stream = request.File.OpenReadStream();

		try
		{
			var (sheetName, columns) = await _importBatchService.DetectColumnsAsync(request.Target, stream, cancellationToken);
			return TypedResults.Ok(new DetectedColumnsResponse(sheetName, columns, _importBatchService.GetFields(request.Target)));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Could not read columns of uploaded {Target} import file {FileName}", request.Target, request.File.FileName);
			return TypedResults.BadRequest(new InvalidExcelFileResponse(request.File.FileName));
		}
	}
}
