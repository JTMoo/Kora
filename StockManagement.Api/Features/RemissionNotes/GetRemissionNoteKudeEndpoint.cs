using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Api.Features.RemissionNotes;


public sealed record GetRemissionNoteKudeRequest(string Number);


/// <remarks>KuDE (#183, ADR-0035) - 409 if the remission note has no CDC yet (not transmitted or contingency-issued).</remarks>
public class GetRemissionNoteKudeEndpoint(
	IRemissionNoteServiceProvider remissionNoteServiceProvider,
	ISettingsService settingsService,
	IKudeVerificationUrlBuilder verificationUrlBuilder,
	IKudeQrCodeGenerator qrCodeGenerator,
	IKudeHtmlBuilder htmlBuilder) : Endpoint<GetRemissionNoteKudeRequest, Results<ContentHttpResult, NotFound, Conflict<string>>>
{
	private readonly IRemissionNoteServiceProvider _remissionNoteServiceProvider = remissionNoteServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly IKudeVerificationUrlBuilder _verificationUrlBuilder = verificationUrlBuilder;
	private readonly IKudeQrCodeGenerator _qrCodeGenerator = qrCodeGenerator;
	private readonly IKudeHtmlBuilder _htmlBuilder = htmlBuilder;


	public override void Configure()
	{
		this.Get("/remission-notes/{Number}/kude");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<Results<ContentHttpResult, NotFound, Conflict<string>>> ExecuteAsync(GetRemissionNoteKudeRequest request, CancellationToken cancellationToken)
	{
		if (await _remissionNoteServiceProvider.GetRemissionNoteAsync(request.Number, cancellationToken) is not RemissionNote remissionNote) return TypedResults.NotFound();

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);

		DteRemisionData data;
		try
		{
			data = KudeDataMapper.ToRemisionData(remissionNote, companySettings);
		}
		catch (InvalidOperationException ex)
		{
			return TypedResults.Conflict(ex.Message);
		}

		var qrDataUri = _qrCodeGenerator.GenerateDataUri(_verificationUrlBuilder.BuildForRemision(data));
		return TypedResults.Text(_htmlBuilder.BuildRemision(data, qrDataUri), "text/html");
	}
}
