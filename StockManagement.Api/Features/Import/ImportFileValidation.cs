using Microsoft.AspNetCore.Http;

namespace StockManagement.Api.Features.Import;


/// <summary>Shared size/type guard for uploaded import files, ahead of <c>ClosedXML</c> loading the workbook into memory (#254).</summary>
public static class ImportFileValidation
{
	public const long MaxFileSizeBytes = 20 * 1024 * 1024;

	public static bool IsAllowedSize(IFormFile? file)
	{
		return file is null || file.Length <= MaxFileSizeBytes;
	}

	public static bool IsAllowedType(IFormFile? file)
	{
		if (file is null) return true;
		return Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
			&& file.ContentType is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" or "application/octet-stream";
	}
}
