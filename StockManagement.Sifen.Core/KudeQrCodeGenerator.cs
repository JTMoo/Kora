using QRCoder;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// <see cref="IKudeQrCodeGenerator"/> on QRCoder (MIT, pure managed, no native deps) - PNG embedded as a
/// <c>data:</c> URI, never written to disk (#183, ADR-0035).
/// </summary>
public sealed class KudeQrCodeGenerator : IKudeQrCodeGenerator
{
	public string GenerateDataUri(string verificationUrl)
	{
		ArgumentException.ThrowIfNullOrEmpty(verificationUrl);

		using var qrGenerator = new QRCodeGenerator();
		using var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.M);
		var pngBytes = new PngByteQRCode(qrCodeData).GetGraphic(10);

		return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
	}
}
