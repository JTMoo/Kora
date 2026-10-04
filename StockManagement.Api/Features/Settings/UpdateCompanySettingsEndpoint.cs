using FastEndpoints;
using FluentValidation;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Util;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.Settings;


public sealed record UpdateCompanySettingsRequest(string CompanyName, string TaxId, string Currency, decimal VatRatePercent, int PaymentTermInDays, int FirstInvoiceNumber, int FirstCustomerId, int CurrencyDecimalDigits,
	string Ruc = "", string TimbradoNumber = "", DateTime? TimbradoValidFrom = null, DateTime? TimbradoValidTo = null, string EstablishmentCode = "001", string PointOfSaleCode = "001", string EstablishmentAddress = "");


public class UpdateCompanySettingsValidator : Validator<UpdateCompanySettingsRequest>
{
	public UpdateCompanySettingsValidator()
	{
		this.RuleFor(request => request.VatRatePercent).InclusiveBetween(0, 100).WithMessage("vatRateOutOfRange");
		this.RuleFor(request => request.PaymentTermInDays).GreaterThanOrEqualTo(0).WithMessage("paymentTermNegative");
		this.RuleFor(request => request.FirstInvoiceNumber).GreaterThan(0).WithMessage("firstInvoiceNumberNotPositive");
		this.RuleFor(request => request.FirstCustomerId).GreaterThan(0).WithMessage("firstCustomerIdNotPositive");
		this.RuleFor(request => request.CurrencyDecimalDigits).InclusiveBetween(0, 4).WithMessage("currencyDecimalDigitsOutOfRange");
		this.RuleFor(request => request.Ruc).Must(ruc => string.IsNullOrEmpty(ruc) || RucValidator.TryNormalize(ruc, out _)).WithMessage("rucInvalid");
		this.RuleFor(request => request.EstablishmentCode).Matches(@"^\d{3}$").WithMessage("establishmentCodeInvalid");
		this.RuleFor(request => request.PointOfSaleCode).Matches(@"^\d{3}$").WithMessage("pointOfSaleCodeInvalid");
		this.RuleFor(request => request.TimbradoValidTo).GreaterThanOrEqualTo(request => request.TimbradoValidFrom).When(request => request.TimbradoValidFrom is not null && request.TimbradoValidTo is not null).WithMessage("timbradoDateRangeInvalid");
	}
}


public class UpdateCompanySettingsEndpoint(ISettingsService settingsService) : Endpoint<UpdateCompanySettingsRequest, CompanySettingsResponse>
{
	private readonly ISettingsService _settingsService = settingsService;


	public override void Configure()
	{
		this.Put("/company-settings");
		this.Permissions(Permission.SettingsWrite);
	}

	public override async Task<CompanySettingsResponse> ExecuteAsync(UpdateCompanySettingsRequest request, CancellationToken cancellationToken)
	{
		var settings = new CompanySettings(request.CompanyName, request.TaxId, request.Currency, request.VatRatePercent, request.PaymentTermInDays, request.FirstInvoiceNumber, request.FirstCustomerId, request.CurrencyDecimalDigits,
			request.Ruc, request.TimbradoNumber, request.TimbradoValidFrom, request.TimbradoValidTo, request.EstablishmentCode, request.PointOfSaleCode, request.EstablishmentAddress);
		await _settingsService.SetCompanySettingsAsync(settings, cancellationToken);
		return CompanySettingsResponse.From(settings);
	}
}
