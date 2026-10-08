namespace StockManagement.Api.Security;


/// <summary>
/// Resolves the key used to sign/verify JWTs. appsettings.json ships a known placeholder (fine for local dev);
/// outside Development that placeholder is never used as the real key (#243, every install would share one key) -
/// a per-install key is generated and persisted instead, via <see cref="GeneratedSigningKeyStore"/>.
/// </summary>
public static class JwtSigningKeyProvider
{
	public const string PlaceholderKey = "local-dev-only-signing-key-override-me-in-appsettings-local-json";


	/// <exception cref="InvalidOperationException">Missing, or still the placeholder outside Development and no per-install key could be generated</exception>
	public static string Resolve(string? configuredKey, bool isDevelopment)
	{
		if (string.IsNullOrEmpty(configuredKey)) throw new InvalidOperationException("Jwt:SigningKey is missing.");
		if (configuredKey != PlaceholderKey || isDevelopment) return configuredKey;

		return GeneratedSigningKeyStore.LoadOrCreate();
	}
}
