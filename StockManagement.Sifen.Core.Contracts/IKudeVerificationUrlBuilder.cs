namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Builds the URL a KuDE's QR code points to (#183, ADR-0035).
/// </summary>
/// <remarks>
/// DNIT's published scheme adds a <c>cHashQR</c> computed from a contributor-specific CSC (Código de Seguridad
/// del Contribuyente) secret Kora doesn't hold or model yet - distinct from <c>CdcInput.SecurityCode</c>, which is
/// just a CDC field. Until that's wired in, this is a best-effort URL carrying only the fields honestly known
/// (CDC, dates, amounts): a reader can see what's on the document but can't yet confirm it against DNIT's own
/// checker. Flagged in ADR-0035, not faked - same pattern as every other unverified SIFEN constant in this project.
/// </remarks>
public interface IKudeVerificationUrlBuilder
{
	string BuildForInvoice(DteInvoiceData data);
	string BuildForRemision(DteRemisionData data);
}
