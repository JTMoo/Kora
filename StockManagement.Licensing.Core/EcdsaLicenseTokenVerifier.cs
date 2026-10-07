using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Licensing.Core;


/// <summary>
/// Verifies license keys signed by the (future, out of scope) license server's ECDsa P-256 private key.
/// Key format: <c>base64url(payloadJson).base64url(signature)</c>, ES256-style. See ADR-0041.
/// </summary>
/// <remarks>ECDsa over Ed25519: both are in the BCL (<see cref="System.Security.Cryptography"/>) on every platform Kora ships on; Ed25519 needs a third-party library.</remarks>
internal sealed class EcdsaLicenseTokenVerifier(IOptions<LicensingOptions> options) : ILicenseTokenVerifier
{
	private readonly LicensingOptions _options = options.Value;


	public bool TryVerify(string licenseKey, out LicenseToken? token)
	{
		token = null;
		if (string.IsNullOrEmpty(_options.PublicKey)) return false;

		var parts = licenseKey.Split('.');
		if (parts.Length != 2) return false;

		if (!TryDecodeBase64Url(parts[0], out var payloadBytes) || !TryDecodeBase64Url(parts[1], out var signatureBytes)) return false;

		using var ecdsa = ECDsa.Create();
		try
		{
			ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(_options.PublicKey), out _);
		}
		catch (CryptographicException)
		{
			return false;
		}

		if (!ecdsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256)) return false;

		return TryParsePayload(payloadBytes, out token);
	}

	private static bool TryDecodeBase64Url(string value, out byte[] bytes)
	{
		try
		{
			bytes = Base64Url.DecodeFromChars(value);
			return true;
		}
		catch (FormatException)
		{
			bytes = [];
			return false;
		}
	}

	private static bool TryParsePayload(byte[] payloadBytes, out LicenseToken? token)
	{
		token = null;
		try
		{
			var payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes);
			if (payload is null || string.IsNullOrEmpty(payload.Licensee)) return false;

			token = new LicenseToken(payload.Licensee, payload.Plan, payload.IssuedAtUtc, payload.ExpiresAtUtc);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private sealed record TokenPayload(string Licensee, LicensePlan Plan, DateTime IssuedAtUtc, DateTime ExpiresAtUtc);
}
