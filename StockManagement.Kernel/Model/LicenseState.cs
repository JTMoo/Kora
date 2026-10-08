namespace StockManagement.Kernel.Model;


/// <summary>
/// Single row of licensing state (trial + current activation). Same singleton-row pattern as <see cref="AppSettings"/>.
/// </summary>
public class LicenseState : Database.BaseDocument
{
	private DateTime trialStartedAtUtc;
	private string activatedLicenseKey = "";
	private DateTime? activatedAtUtc;
	private string machineId = "";
	private DateTime highWaterMarkUtc;


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

	/// <summary>
	/// Generated once on first read (see <see cref="TrialStartedAtUtc"/>); identifies this install for machine-bound
	/// activation keys (ADR-0044). Shown to the owner so they can quote it to the license issuer.
	/// </summary>
	public string MachineId
	{
		get { return this.machineId; }
		set { this.SetField(ref this.machineId, value); }
	}

	/// <summary>
	/// Latest UTC instant this install has ever observed; status computation clamps <c>now</c> to this value so
	/// rolling the system clock back cannot resurrect a lapsed trial or expired subscription (ADR-0044)
	/// </summary>
	public DateTime HighWaterMarkUtc
	{
		get { return this.highWaterMarkUtc; }
		set { this.SetField(ref this.highWaterMarkUtc, value); }
	}
}
