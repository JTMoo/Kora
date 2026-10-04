using StockManagement.Sifen.Core;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class KudeQrCodeGeneratorTests
{
	[TestMethod]
	public void GenerateDataUri_AnyUrl_ReturnsPngDataUri()
	{
		// Act
		var dataUri = new KudeQrCodeGenerator().GenerateDataUri("https://ekuatia.set.gov.py/consultas-kude/qr?Id=123");

		// Assert
		StringAssert.StartsWith(dataUri, "data:image/png;base64,");
		Assert.IsTrue(dataUri.Length > "data:image/png;base64,".Length);
	}
}
