using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Api.Features.Invoices;


public sealed record GetInvoiceKudeRequest(string Number);


/// <remarks>KuDE (#183, ADR-0035) - 409 if the invoice has no CDC yet (not transmitted or contingency-issued).</remarks>
public class GetInvoiceKudeEndpoint(
	IInvoiceServiceProvider invoiceServiceProvider,
	ISettingsService settingsService,
	IKudeVerificationUrlBuilder verificationUrlBuilder,
	IKudeQrCodeGenerator qrCodeGenerator,
	IKudeHtmlBuilder htmlBuilder) : Endpoint<GetInvoiceKudeRequest, Results<ContentHttpResult, NotFound, Conflict<string>>>
{
	private readonly IInvoiceServiceProvider _invoiceServiceProvider = invoiceServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly IKudeVerificationUrlBuilder _verificationUrlBuilder = verificationUrlBuilder;
	private readonly IKudeQrCodeGenerator _qrCodeGenerator = qrCodeGenerator;
	private readonly IKudeHtmlBuilder _htmlBuilder = htmlBuilder;


	public override void Configure()
	{
		this.Get("/invoices/{Number}/kude");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<Results<ContentHttpResult, NotFound, Conflict<string>>> ExecuteAsync(GetInvoiceKudeRequest request, CancellationToken cancellationToken)
	{
		if (await _invoiceServiceProvider.GetInvoiceAync(request.Number, cancellationToken) is not Invoice invoice) return TypedResults.NotFound();

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);

		DteInvoiceData data;
		try
		{
			data = KudeDataMapper.ToInvoiceData(invoice, companySettings);
		}
		catch (InvalidOperationException ex)
		{
			return TypedResults.Conflict(ex.Message);
		}

		var qrDataUri = _qrCodeGenerator.GenerateDataUri(_verificationUrlBuilder.BuildForInvoice(data));
		return TypedResults.Text(_htmlBuilder.BuildInvoice(data, qrDataUri), "text/html");
	}
}
