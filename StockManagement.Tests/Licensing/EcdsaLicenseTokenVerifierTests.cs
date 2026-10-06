using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockManagement.Licensing.Core;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Tests.Licensing;


[TestClass]
public sealed class EcdsaLicenseTokenVerifierTests
{
	private readonly ECDsa _signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);


	[TestMethod]
	public void TryVerify_ValidSignature_ReturnsDecodedToken()
	{
		// Arrange
		var expiresAtUtc = DateTime.UtcNow.AddDays(30);
		var key = this.Sign("Acme S.A.", LicensePlan.Monthly, expiresAtUtc);

		// Act
		var verified = this.CreateVerifier().TryVerify(key, out var token);

		// Assert
		Assert.IsTrue(verified);
		Assert.AreEqual("Acme S.A.", token!.Licensee);
		Assert.AreEqual(LicensePlan.Monthly, token.Plan);
	}

	[TestMethod]
	public void TryVerify_TamperedPayload_Fails()
	{
		// Arrange
		var key = this.Sign("Acme S.A.", LicensePlan.Monthly, DateTime.UtcNow.AddDays(30));
		var tamperedPayload = Base64Url.EncodeToString("""{"Licensee":"Attacker","Plan":"Yearly","IssuedAtUtc":"2026-01-01","ExpiresAtUtc":"2099-01-01"}"""u8);
		var tampered = $"{tamperedPayload}.{key.Split('.')[1]}";

		// Act
		var verified = this.CreateVerifier().TryVerify(tampered, out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	[TestMethod]
	public void TryVerify_SignedByAnotherKey_Fails()
	{
		// Arrange
		using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { Licensee = "Acme", Plan = LicensePlan.Monthly, IssuedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(30) });
		var signature = otherKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
		var key = $"{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";

		// Act
		var verified = this.CreateVerifier().TryVerify(key, out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	[TestMethod]
	public void TryVerify_MalformedKey_Fails()
	{
		// Act
		var verified = this.CreateVerifier().TryVerify("not-a-valid-key", out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	[TestMethod]
	public void TryVerify_NoPublicKeyConfigured_Fails()
	{
		// Arrange
		var key = this.Sign("Acme S.A.", LicensePlan.Monthly, DateTime.UtcNow.AddDays(30));
		var verifier = new EcdsaLicenseTokenVerifier(Options.Create(new LicensingOptions { PublicKey = "" }));

		// Act
		var verified = verifier.TryVerify(key, out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	private string Sign(string licensee, LicensePlan plan, DateTime expiresAtUtc)
	{
		var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { Licensee = licensee, Plan = plan, IssuedAtUtc = DateTime.UtcNow, ExpiresAtUtc = expiresAtUtc });
		var signature = _signingKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
		return $"{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";
	}

	private EcdsaLicenseTokenVerifier CreateVerifier()
	{
		var publicKey = Convert.ToBase64String(_signingKey.ExportSubjectPublicKeyInfo());
		return new EcdsaLicenseTokenVerifier(Options.Create(new LicensingOptions { PublicKey = publicKey }));
	}
}
