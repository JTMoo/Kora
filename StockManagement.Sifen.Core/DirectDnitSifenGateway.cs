using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Util;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// <see cref="ISifenGateway"/> talking directly to DNIT - no PSE (#134). The signing certificate is read from a
/// local file path (<see cref="SifenGatewayOptions.CertificatePath"/>), never stored in the database.
/// </summary>
/// <remarks>
/// The SOAP envelope shape and response codes below follow DNIT's publicly documented "Sincrono de Recepcion de
/// Lotes"/"Recepcion de DE" contract but have not been exercised against a real or sandbox DNIT endpoint in this
/// session (no network access to dnit.gov.py) - unverified, same flag as <see cref="DteXmlBuilder"/> and
/// <see cref="CdcGenerator"/>. Verify against DNIT's test environment (ADR-0031's local-only manual harness)
/// before relying on this for production transmission.
/// </remarks>
public sealed class DirectDnitSifenGateway(
	ICdcGenerator cdcGenerator,
	IDteXmlBuilder xmlBuilder,
	IXadesSigner signer,
	ISettingsService settingsService,
	IHttpClientFactory httpClientFactory,
	IOptions<SifenGatewayOptions> options) : ISifenGateway
{
	private readonly ICdcGenerator _cdcGenerator = cdcGenerator;
	private readonly IDteXmlBuilder _xmlBuilder = xmlBuilder;
	private readonly IXadesSigner _signer = signer;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly SifenGatewayOptions _options = options.Value;


	public async Task<SifenTransmissionResult> SendAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(invoice);

		try
		{
			var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
			var data = BuildInvoiceData(invoice, companySettings);
			var unsignedDe = _xmlBuilder.BuildInvoice(data);

			using var certificate = LoadCertificate();
			var signedDe = _signer.Sign(unsignedDe, certificate);

			using var client = _httpClientFactory.CreateClient(nameof(DirectDnitSifenGateway));
			using var content = new StringContent(signedDe.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
			using var response = await client.PostAsync(_options.ServiceUrl, content, cancellationToken);

			return ParseResponse(await response.Content.ReadAsStringAsync(cancellationToken), data.Cdc);
		}
		catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
		{
			return SifenTransmissionResult.Error(ex.Message);
		}
	}

	/// <summary>
	/// Builds, signs and transmits a Nota de Remisión Electrónica (#162). Contingency-issued remission notes (#188)
	/// arrive here with <see cref="RemissionNote.Cdc"/> already set and are reused as-is, same as
	/// <see cref="SendAsync(Invoice, CancellationToken)"/> does for invoices.
	/// </summary>
	public async Task<SifenTransmissionResult> SendRemisionAsync(RemissionNote remissionNote, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(remissionNote);

		try
		{
			var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
			var data = BuildRemisionData(remissionNote, companySettings);
			var unsignedDe = _xmlBuilder.BuildRemision(data);

			using var certificate = LoadCertificate();
			var signedDe = _signer.Sign(unsignedDe, certificate);

			using var client = _httpClientFactory.CreateClient(nameof(DirectDnitSifenGateway));
			using var content = new StringContent(signedDe.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
			using var response = await client.PostAsync(_options.ServiceUrl, content, cancellationToken);

			return ParseResponse(await response.Content.ReadAsStringAsync(cancellationToken), data.Cdc);
		}
		catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
		{
			return SifenTransmissionResult.Error(ex.Message);
		}
	}

	/// <exception cref="InvalidOperationException">Company settings or the remission note are missing data a DE needs</exception>
	private DteRemisionData BuildRemisionData(RemissionNote remissionNote, CompanySettings companySettings)
	{
		if (!RucValidator.TryNormalize(companySettings.Ruc, out var normalizedRuc))
			throw new InvalidOperationException("Company settings RUC is missing or invalid; set it before transmitting to SIFEN.");

		var rucParts = normalizedRuc.Split('-');
		var rucBase = rucParts[0];
		var rucCheckDigit = int.Parse(rucParts[1]);

		if (companySettings.TimbradoValidFrom is not DateTime timbradoValidFrom)
			throw new InvalidOperationException("Company settings timbrado validity start is missing; set it before transmitting to SIFEN.");

		var emisor = new DteEmisor(
			rucBase,
			rucCheckDigit,
			companySettings.CompanyName,
			companySettings.EstablishmentCode,
			companySettings.PointOfSaleCode,
			companySettings.EstablishmentAddress,
			companySettings.TimbradoNumber,
			DateOnly.FromDateTime(timbradoValidFrom));

		var receptor = new DteReceptor(
			remissionNote.Customer.Display,
			RucBase: null,
			RucCheckDigit: null,
			remissionNote.Customer.IdentificationNumber);

		// A non-empty remissionNote.Cdc was already assigned locally (e.g. contingency issuance, #188) - reuse it as-is
		// rather than regenerating, so the CDC and dNumDoc below always agree on the same document number (same
		// reasoning as BuildInvoiceData's Cdc reuse).
		string cdc;
		long documentNumber;
		if (!string.IsNullOrEmpty(remissionNote.Cdc))
		{
			cdc = remissionNote.Cdc;
			documentNumber = long.Parse(cdc.AsSpan(17, 7));
		}
		else
		{
			if (InvoiceNumber.TryParseSequence(remissionNote.Number, companySettings.EstablishmentCode, companySettings.PointOfSaleCode) is not int sequence)
				throw new InvalidOperationException($"Remission note number '{remissionNote.Number}' does not match the configured establishment/point-of-sale.");

			documentNumber = sequence;
			cdc = _cdcGenerator.Generate(new CdcInput(
				SifenDocumentType.NotaDeRemisionElectronica,
				rucBase,
				rucCheckDigit,
				companySettings.EstablishmentCode,
				companySettings.PointOfSaleCode,
				documentNumber,
				TaxpayerType.Juridica,
				DateOnly.FromDateTime(remissionNote.Date),
				EmissionType.Normal,
				GenerateSecurityCode()));
		}

		var items = remissionNote.Items.Select(item => new DteRemisionItem(
			item.StockItem.Code,
			item.StockItem.Name,
			item.Amount)).ToList();

		return new DteRemisionData(cdc, emisor, receptor, remissionNote.Date, documentNumber, remissionNote.Reason, remissionNote.DestinationAddress, items);
	}

	/// <exception cref="InvalidOperationException">Company settings or the invoice are missing data a DE needs</exception>
	private DteInvoiceData BuildInvoiceData(Invoice invoice, CompanySettings companySettings)
	{
		if (!RucValidator.TryNormalize(companySettings.Ruc, out var normalizedRuc))
			throw new InvalidOperationException("Company settings RUC is missing or invalid; set it before transmitting to SIFEN.");

		var rucParts = normalizedRuc.Split('-');
		var rucBase = rucParts[0];
		var rucCheckDigit = int.Parse(rucParts[1]);

		if (companySettings.TimbradoValidFrom is not DateTime timbradoValidFrom)
			throw new InvalidOperationException("Company settings timbrado validity start is missing; set it before transmitting to SIFEN.");

		var emisor = new DteEmisor(
			rucBase,
			rucCheckDigit,
			companySettings.CompanyName,
			companySettings.EstablishmentCode,
			companySettings.PointOfSaleCode,
			companySettings.EstablishmentAddress,
			companySettings.TimbradoNumber,
			DateOnly.FromDateTime(timbradoValidFrom));

		var receptor = new DteReceptor(
			invoice.Customer.Display,
			RucBase: null,
			RucCheckDigit: null,
			invoice.Customer.IdentificationNumber);

		// A non-empty invoice.Cdc was already assigned locally (e.g. contingency issuance, #149) - reuse it as-is
		// rather than regenerating, so the CDC and dNumDoc below always agree on the same document number.
		string cdc;
		long documentNumber;
		if (!string.IsNullOrEmpty(invoice.Cdc))
		{
			cdc = invoice.Cdc;
			documentNumber = long.Parse(cdc.AsSpan(17, 7));
		}
		else
		{
			if (InvoiceNumber.TryParseSequence(invoice.Number, companySettings.EstablishmentCode, companySettings.PointOfSaleCode) is not int sequence)
				throw new InvalidOperationException($"Invoice number '{invoice.Number}' does not match the configured establishment/point-of-sale.");

			documentNumber = sequence;
			cdc = _cdcGenerator.Generate(new CdcInput(
				SifenDocumentType.FacturaElectronica,
				rucBase,
				rucCheckDigit,
				companySettings.EstablishmentCode,
				companySettings.PointOfSaleCode,
				documentNumber,
				TaxpayerType.Juridica,
				DateOnly.FromDateTime(invoice.Date),
				EmissionType.Normal,
				GenerateSecurityCode()));
		}

		var items = invoice.Items.Select(item => new DteItem(
			item.StockItem.Code,
			item.StockItem.Name,
			item.Amount,
			item.StockItem.Price,
			item.StockItem.VatRatePercent)).ToList();

		return new DteInvoiceData(cdc, emisor, receptor, invoice.Date, documentNumber, items, companySettings.CurrencyDecimalDigits);
	}

	private X509Certificate2 LoadCertificate()
	{
		if (string.IsNullOrWhiteSpace(_options.CertificatePath))
			throw new InvalidOperationException($"{SifenGatewayOptions.SectionName}:{nameof(SifenGatewayOptions.CertificatePath)} is not configured.");

		return new X509Certificate2(_options.CertificatePath, _options.CertificatePassword, X509KeyStorageFlags.EphemeralKeySet);
	}

	private static string GenerateSecurityCode()
	{
		return RandomNumberGenerator.GetInt32(1_000_000_000).ToString("D9");
	}

	/// <remarks>
	/// DNIT's synchronous response carries <c>dCodRes</c> ("0260" = aprobado) inside a SOAP body; anything else is
	/// treated as a rejection with the response body as the message. A non-success HTTP status, or a response this
	/// parser can't make sense of, is an <see cref="SifenTransmissionOutcome.Error"/> so the outbox worker retries it.
	/// </remarks>
	private static SifenTransmissionResult ParseResponse(string responseBody, string cdc)
	{
		if (string.IsNullOrWhiteSpace(responseBody)) return SifenTransmissionResult.Error("Empty response from DNIT.");

		return responseBody.Contains("<dCodRes>0260</dCodRes>", StringComparison.Ordinal)
			? SifenTransmissionResult.Accepted(cdc)
			: SifenTransmissionResult.Rejected(cdc, responseBody);
	}
}
