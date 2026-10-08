using System.Security.Cryptography;

namespace StockManagement.Api.Security;


/// <summary>
/// Per-install JWT signing key, generated once and persisted outside git (same pattern as
/// <c>StockManagement.Feedback.Core.InstallId</c>), so every desktop install signs tokens with its own key (#243).
/// </summary>
internal static class GeneratedSigningKeyStore
{
	private static string Path => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kora", "jwt-signing-key");


	/// <exception cref="InvalidOperationException">The key file could not be read or created</exception>
	public static string LoadOrCreate()
	{
		try
		{
			if (File.Exists(Path))
			{
				var existing = File.ReadAllText(Path).Trim();
				if (!string.IsNullOrEmpty(existing)) return existing;
			}

			var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
			File.WriteAllText(Path, key);
			return key;
		}
		catch (IOException ex)
		{
			throw new InvalidOperationException("Jwt:SigningKey is still the default placeholder and no per-install key could be generated.", ex);
		}
	}
}
