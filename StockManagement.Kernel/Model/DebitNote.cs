using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


/// <summary>
/// SIFEN Nota de Débito Electrónica (#184) - increases a previously issued invoice (interest, surcharges, price
/// corrections upward), the counterpart to <see cref="CreditNote"/>. Unlike <see cref="CreditNote"/>, this has a
/// full SIFEN transmission path (own CDC, own outbox) since the acceptance criteria require one.
/// </summary>
public class DebitNote : BaseDocument
{
	private string number = "";
	private DateTime date;
	private string reason = "";
	private decimal total;
	private decimal tax;
	private string cdc = "";
	private TransmissionStatus transmissionStatus = TransmissionStatus.Pending;


	public DebitNote()
	{
	}


	/// <summary>
	/// DNIT composite number (establishment-pointOfSale-sequence), own sequence scoped separately from
	/// <see cref="Invoice.Number"/>; see <see cref="Util.InvoiceNumber"/>.
	/// </summary>
	public string Number
	{
		get { return this.number; }
		set { this.SetField(ref this.number, value); }
	}

	public DateTime Date
	{
		get { return this.date; }
		set { this.SetField(ref this.date, value); }
	}

	public string Reason
	{
		get { return this.reason; }
		set { this.SetField(ref this.reason, value); }
	}

	/// <summary>
	/// The invoice this debit note increases; referenced in the DE as <c>gCamDEAsoc</c>
	/// </summary>
	public Invoice Invoice { get; set; } = null!;

	public List<DebitNoteItem> Items { get; set; } = [];

	public decimal Total
	{
		get { return this.total; }
		set { this.SetField(ref this.total, value); }
	}

	public decimal Tax
	{
		get { return this.tax; }
		set { this.SetField(ref this.tax, value); }
	}

	/// <summary>
	/// 44-digit SIFEN control code; empty until the first transmission attempt builds the DE
	/// </summary>
	public string Cdc
	{
		get { return this.cdc; }
		set { this.SetField(ref this.cdc, value); }
	}

	public TransmissionStatus TransmissionStatus
	{
		get { return this.transmissionStatus; }
		set { this.SetField(ref this.transmissionStatus, value); }
	}
}
