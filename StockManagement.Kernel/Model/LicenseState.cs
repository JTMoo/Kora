namespace StockManagement.Kernel.Model;


/// <summary>
/// Single row of licensing state (trial + current activation). Same singleton-row pattern as <see cref="AppSettings"/>.
/// </summary>
public class LicenseState : Database.BaseDocument
{
	private DateTime trialStartedAtUtc;
	private string activatedLicenseKey = "";
	private DateTime? activatedAtUtc;


	/// <summary>
	/// Set once, the first time license state is ever read (fresh install or an existing DB upgraded onto licensing)
	/// </summary>
	public DateTime TrialStartedAtUtc
	{
		get { return this.trialStartedAtUtc; }
		set { this.SetField(ref this.trialStartedAtUtc, value); }
	}

	/// <summary>
	/// The last license key that verified successfully; re-verified (not trusted as-is) on every status read
	/// </summary>
	public string ActivatedLicenseKey
	{
		get { return this.activatedLicenseKey; }
		set { this.SetField(ref this.activatedLicenseKey, value); }
	}

	public DateTime? ActivatedAtUtc
	{
		get { return this.activatedAtUtc; }
		set { this.SetField(ref this.activatedAtUtc, value); }
	}
}
