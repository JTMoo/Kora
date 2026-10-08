namespace StockManagement.Feedback.Core;


/// <summary>
/// Stable per-install id for reports, independent of licensing. Persisted as a plain file so it survives restarts
/// without needing a database round-trip.
/// </summary>
internal static class InstallId
{
	private static readonly Lazy<string> _current = new(Load);

	public static string Current => _current.Value;

	private static string Load()
	{
		var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kora", "install-id");

		try
		{
			if (File.Exists(path)) return File.ReadAllText(path).Trim();

			var id = Guid.NewGuid().ToString();
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, id);
			return id;
		}
		catch (IOException)
		{
			return Guid.NewGuid().ToString();
		}
	}
}
