using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Sales.Core;


/// <summary>
/// <see cref="IPaymentLinkGateway"/> talking directly to Bancard's vPOS "single buy" (QR) API (#150).
/// </summary>
/// <remarks>
/// The request/response shape below follows Bancard's publicly documented vPOS integration guide (single_buys +
/// confirmations, token = md5(private_key + shop_process_id + amount + currency)) but has not been exercised
/// against a real or sandbox Bancard endpoint in this session (no network access to bancard.com.py) - unverified,
/// same flag as <see cref="StockManagement.Sifen.Core"/>'s DNIT gateway. Verify against Bancard's sandbox before
/// relying on this for production collection.
/// </remarks>
public sealed class BancardPaymentLinkGateway(IHttpClientFactory httpClientFactory, IOptions<BancardGatewayOptions> options, ILogger<BancardPaymentLinkGateway> logger) : IPaymentLinkGateway
{
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly BancardGatewayOptions _options = options.Value;
	private readonly ILogger<BancardPaymentLinkGateway> _logger = logger;


	public async Task<PaymentLinkGatewayResult> CreateAsync(string invoiceNumber, decimal amount, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

		try
		{
			var shopProcessId = invoiceNumber;
			var amountText = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
			var token = Md5Hex($"{_options.PrivateKey}{shopProcessId}{amountText}{_options.Currency}");

			var request = new SingleBuyRequest(_options.PublicKey, new SingleBuyOperation(token, shopProcessId, amountText, _options.Currency, $"Invoice {invoiceNumber}"));

			using var client = _httpClientFactory.CreateClient(nameof(BancardPaymentLinkGateway));
			using var response = await client.PostAsJsonAsync($"{_options.BaseUrl}/vpos/api/0.3/single_buys", request, cancellationToken);
			var body = await response.Content.ReadFromJsonAsync<SingleBuyResponse>(cancellationToken: cancellationToken);

			if (!response.IsSuccessStatusCode || body?.ProcessId is null)
				return PaymentLinkGatewayResult.Failure(body?.Messages?.FirstOrDefault()?.Description ?? $"Bancard returned {(int)response.StatusCode}.");

			return PaymentLinkGatewayResult.Success(shopProcessId, body.QrUrl ?? "");
		}
		catch (Exception ex) when (ex is not ArgumentException)
		{
			return PaymentLinkGatewayResult.Failure(ex.Message);
		}
	}

	public async Task<PaymentLinkStatus> GetStatusAsync(string externalId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

		try
		{
			var token = Md5Hex($"{_options.PrivateKey}{externalId}get_confirmation");

			using var client = _httpClientFactory.CreateClient(nameof(BancardPaymentLinkGateway));
			using var response = await client.GetAsync($"{_options.BaseUrl}/vpos/api/0.3/single_buys/{externalId}/confirmations?token={token}", cancellationToken);
			if (!response.IsSuccessStatusCode) return PaymentLinkStatus.Pending;

			var body = await response.Content.ReadFromJsonAsync<ConfirmationResponse>(cancellationToken: cancellationToken);
			return body?.Status switch
			{
				"approved" => PaymentLinkStatus.Paid,
				"rejected" => PaymentLinkStatus.Failed,
				"rolled_back" => PaymentLinkStatus.Cancelled,
				_ => PaymentLinkStatus.Pending
			};
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Bancard status poll failed for {ExternalId}.", externalId);
			return PaymentLinkStatus.Pending;
		}
	}

	private static string Md5Hex(string value)
	{
		return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
	}


	private sealed record SingleBuyRequest([property: JsonPropertyName("public_key")] string PublicKey, [property: JsonPropertyName("operation")] SingleBuyOperation Operation);

	private sealed record SingleBuyOperation(
		[property: JsonPropertyName("token")] string Token,
		[property: JsonPropertyName("shop_process_id")] string ShopProcessId,
		[property: JsonPropertyName("amount")] string Amount,
		[property: JsonPropertyName("currency")] string Currency,
		[property: JsonPropertyName("description")] string Description);

	private sealed record SingleBuyResponse(
		[property: JsonPropertyName("process_id")] string? ProcessId,
		[property: JsonPropertyName("qr_url")] string? QrUrl,
		[property: JsonPropertyName("messages")] List<BancardMessage>? Messages);

	private sealed record ConfirmationResponse([property: JsonPropertyName("status")] string? Status);

	private sealed record BancardMessage([property: JsonPropertyName("description")] string? Description);
}
