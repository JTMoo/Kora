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
		var key = this.Sign("1", "Acme S.A.", LicensePlan.Monthly, expiresAtUtc);

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
		var key = this.Sign("1", "Acme S.A.", LicensePlan.Monthly, DateTime.UtcNow.AddDays(30));
		var tamperedPayload = Base64Url.EncodeToString("""{"Licensee":"Attacker","Plan":"Yearly","IssuedAtUtc":"2026-01-01","ExpiresAtUtc":"2099-01-01"}"""u8);
		var tampered = $"1.{tamperedPayload}.{key.Split('.')[2]}";

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
		var key = $"1.{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";

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
		var key = this.Sign("1", "Acme S.A.", LicensePlan.Monthly, DateTime.UtcNow.AddDays(30));
		var verifier = new EcdsaLicenseTokenVerifier(Options.Create(new LicensingOptions()));

		// Act
		var verified = verifier.TryVerify(key, out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	[TestMethod]
	public void TryVerify_UnknownKeyId_Fails()
	{
		// Arrange: signed under kid "2", but only kid "1" is configured - simulates a stale client after key rotation
		var key = this.Sign("2", "Acme S.A.", LicensePlan.Monthly, DateTime.UtcNow.AddDays(30));

		// Act
		var verified = this.CreateVerifier().TryVerify(key, out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	[TestMethod]
	public void TryVerify_DiscountPercentOutOfRange_Fails()
	{
		// Arrange
		var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { Licensee = "Acme", Plan = LicensePlan.Monthly, IssuedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(30), DiscountPercent = 150 });
		var signature = _signingKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
		var key = $"1.{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";

		// Act
		var verified = this.CreateVerifier().TryVerify(key, out var token);

		// Assert
		Assert.IsFalse(verified);
		Assert.IsNull(token);
	}

	[TestMethod]
	public void TryVerify_WithMachineIdAndDiscountClaims_RoundTrips()
	{
		// Arrange
		var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { Licensee = "Acme", Plan = LicensePlan.Yearly, IssuedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(30), MachineId = "machine-123", DiscountPercent = 20, EffectivePricePyg = 1_200_000 });
		var signature = _signingKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
		var key = $"1.{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";

		// Act
		var verified = this.CreateVerifier().TryVerify(key, out var token);

		// Assert
		Assert.IsTrue(verified);
		Assert.AreEqual("machine-123", token!.MachineId);
		Assert.AreEqual(20, token.DiscountPercent);
		Assert.AreEqual(1_200_000, token.EffectivePricePyg);
	}

	private string Sign(string kid, string licensee, LicensePlan plan, DateTime expiresAtUtc)
	{
		var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new { Licensee = licensee, Plan = plan, IssuedAtUtc = DateTime.UtcNow, ExpiresAtUtc = expiresAtUtc });
		var signature = _signingKey.SignData(payloadBytes, HashAlgorithmName.SHA256);
		return $"{kid}.{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";
	}

	private EcdsaLicenseTokenVerifier CreateVerifier()
	{
		var publicKey = Convert.ToBase64String(_signingKey.ExportSubjectPublicKeyInfo());
		return new EcdsaLicenseTokenVerifier(Options.Create(new LicensingOptions { PublicKeys = new() { ["1"] = publicKey } }));
	}
}
