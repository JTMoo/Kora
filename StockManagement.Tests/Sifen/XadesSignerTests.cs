using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class XadesSignerTests
{
	private const string Ns = "http://ekuatia.set.gov.py/sifen/xsd";
	private const string DsNs = "http://www.w3.org/2000/09/xmldsig#";
	private const string XadesNs = "http://uri.etsi.org/01903/v1.3.2#";
	private static readonly string ValidCdc = new('7', 44);

	private static X509Certificate2 CreateSelfSignedCertificate()
	{
		using var rsa = RSA.Create(2048);
		var request = new CertificateRequest("CN=Kora Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
		return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null);
	}

	private static System.Xml.Linq.XDocument BuildUnsignedDocument()
	{
		var emisor = new DteEmisor("1946520", 3, "Acme S.A.", "001", "001", "Avda. Mcal. López 1234", "12345678", new DateOnly(2026, 1, 1));
		var data = new DteInvoiceData(ValidCdc, emisor, new DteReceptor("Juan Perez", "80012345", 6, null),
			new DateTime(2026, 10, 2, 9, 30, 0), 123, [new DteItem("SKU-1", "Widget", 2, 55000m, 10m)], 0);
		return new DteXmlBuilder().BuildInvoice(data);
	}

	[TestMethod]
	public void Sign_ValidDocument_ProducesACryptographicallyValidSignature()
	{
		// Arrange
		using var certificate = CreateSelfSignedCertificate();
		var unsigned = BuildUnsignedDocument();

		// Act
		var signed = new XadesSigner().Sign(unsigned, certificate);

		// Assert
		var xmlDoc = new XmlDocument { PreserveWhitespace = true };
		xmlDoc.LoadXml(signed.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
		var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature", DsNs)[0]!;

		var signedXml = new SignedXml(xmlDoc);
		signedXml.LoadXml(signatureNode);
		Assert.IsTrue(signedXml.CheckSignature(certificate, true));
	}

	[TestMethod]
	public void Sign_ValidDocument_SignedPropertiesCarrySigningCertificateDigest()
	{
		// Arrange
		using var certificate = CreateSelfSignedCertificate();

		// Act
		var signed = new XadesSigner().Sign(BuildUnsignedDocument(), certificate);

		// Assert
		var digestValue = signed.Descendants(System.Xml.Linq.XName.Get("DigestValue", DsNs))
			.First(e => e.Parent!.Name.LocalName == "CertDigest").Value;
		var expectedDigest = Convert.ToBase64String(SHA256.HashData(certificate.RawData));
		Assert.AreEqual(expectedDigest, digestValue);
	}

	[TestMethod]
	public void Sign_ValidDocument_SignatureIsEnvelopedUnderDeElement()
	{
		// Arrange
		using var certificate = CreateSelfSignedCertificate();

		// Act
		var signed = new XadesSigner().Sign(BuildUnsignedDocument(), certificate);

		// Assert
		var de = signed.Root!.Element(System.Xml.Linq.XName.Get("DE", Ns))!;
		Assert.IsTrue(de.Elements(System.Xml.Linq.XName.Get("Signature", DsNs)).Any());
	}

	[TestMethod]
	public void Sign_TamperedAmountAfterSigning_SignatureNoLongerValidates()
	{
		// Arrange
		using var certificate = CreateSelfSignedCertificate();
		var signed = new XadesSigner().Sign(BuildUnsignedDocument(), certificate);
		var xmlDoc = new XmlDocument { PreserveWhitespace = true };
		xmlDoc.LoadXml(signed.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));

		var tampered = (XmlElement)xmlDoc.GetElementsByTagName("dTotOpe", Ns)[0]!;
		tampered.InnerText = "999999";

		// Act
		var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature", DsNs)[0]!;
		var signedXml = new SignedXml(xmlDoc);
		signedXml.LoadXml(signatureNode);

		// Assert
		Assert.IsFalse(signedXml.CheckSignature(certificate, true));
	}
}
