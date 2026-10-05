namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Renders a KuDE's QR code as an embeddable image (#183, ADR-0035).
/// </summary>
public interface IKudeQrCodeGenerator
{
	/// <param name="verificationUrl">The URL encoded into the QR, see <see cref="IKudeVerificationUrlBuilder"/>.</param>
	/// <returns>A <c>data:image/png;base64,...</c> URI.</returns>
	string GenerateDataUri(string verificationUrl);
}
