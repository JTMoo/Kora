using StockManagement.Api.Security;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class JwtSigningKeyProviderTests
{
	[TestMethod]
	public void Resolve_NonPlaceholderKey_ReturnsItUnchanged()
	{
		// Act
		var key = JwtSigningKeyProvider.Resolve("a-real-configured-key", isDevelopment: false);

		// Assert
		Assert.AreEqual("a-real-configured-key", key);
	}

	[TestMethod]
	public void Resolve_PlaceholderInDevelopment_ReturnsPlaceholder()
	{
		// Act
		var key = JwtSigningKeyProvider.Resolve(JwtSigningKeyProvider.PlaceholderKey, isDevelopment: true);

		// Assert
		Assert.AreEqual(JwtSigningKeyProvider.PlaceholderKey, key);
	}

	[TestMethod]
	public void Resolve_PlaceholderOutsideDevelopment_GeneratesPerInstallKey()
	{
		// Act
		var key = JwtSigningKeyProvider.Resolve(JwtSigningKeyProvider.PlaceholderKey, isDevelopment: false);

		// Assert
		Assert.AreNotEqual(JwtSigningKeyProvider.PlaceholderKey, key);
		Assert.IsFalse(string.IsNullOrEmpty(key));
	}

	[TestMethod]
	public void Resolve_PlaceholderOutsideDevelopment_IsStableAcrossCalls()
	{
		// Act
		var first = JwtSigningKeyProvider.Resolve(JwtSigningKeyProvider.PlaceholderKey, isDevelopment: false);
		var second = JwtSigningKeyProvider.Resolve(JwtSigningKeyProvider.PlaceholderKey, isDevelopment: false);

		// Assert
		Assert.AreEqual(first, second);
	}

	[TestMethod]
	public void Resolve_MissingKey_Throws()
	{
		Assert.ThrowsException<InvalidOperationException>(() => JwtSigningKeyProvider.Resolve(null, isDevelopment: false));
	}
}
