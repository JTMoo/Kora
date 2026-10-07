namespace StockManagement.Licensing.Core.Contracts;


/// <summary>
/// Trial and GracePeriod still allow full use; Locked blocks business endpoints (see enforcement middleware)
/// </summary>
public enum LicenseStatus
{
	Trial,
	Active,
	GracePeriod,
	Locked
}
