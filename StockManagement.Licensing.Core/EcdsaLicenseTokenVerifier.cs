using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Licensing.Core;


/// <summary>
/// Verifies license keys signed by the (future, out of scope) license server's ECDsa P-256 private key.
/// Key format: <c>kid.base64url(payloadJson).base64url(signature)</c>, ES256-style. <c>kid</c> selects which
/// configured public key verifies the signature, so the server can rotate keys (see ADR-0044) without
/// invalidating keys already issued under an older one.
/// </summary>
/// <remarks>ECDsa over Ed25519: both are in the BCL (<see cref="System.Security.Cryptography"/>) on every platform Kora ships on; Ed25519 needs a third-party library.</remarks>
internal sealed class EcdsaLicenseTokenVerifier(IOptions<LicensingOptions> options) : ILicenseTokenVerifier
{
	private readonly LicensingOptions _options = options.Value;


	public bool TryVerify(string licenseKey, out LicenseToken? token)
	{
		token = null;

		var parts = licenseKey.Split('.');
		if (parts.Length != 3) return false;
		var (kid, encodedPayload, encodedSignature) = (parts[0], parts[1], parts[2]);

		if (!_options.PublicKeys.TryGetValue(kid, out var publicKey) || string.IsNullOrEmpty(publicKey)) return false;
		if (!TryDecodeBase64Url(encodedPayload, out var payloadBytes) || !TryDecodeBase64Url(encodedSignature, out var signatureBytes)) return false;

		using var ecdsa = ECDsa.Create();
		try
		{
			ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
		}
		catch (CryptographicException)
		{
			return false;
		}
		catch (FormatException)
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
			if (payload.DiscountPercent is < 0 or > 100) return false;

			token = new LicenseToken(payload.Licensee, payload.Plan, payload.IssuedAtUtc, payload.ExpiresAtUtc, payload.MachineId, payload.DiscountPercent, payload.EffectivePricePyg);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private sealed record TokenPayload(string Licensee, LicensePlan Plan, DateTime IssuedAtUtc, DateTime ExpiresAtUtc, string? MachineId = null, int? DiscountPercent = null, int? EffectivePricePyg = null);
}
