using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


/// <summary>
/// Proforma/PO for goods bought abroad: Incoterm plus the customs broker/DUA references, distinct from a
/// domestic purchase. Posting one checks in every <see cref="Items"/> line's <see cref="GoodsImportDocumentItem.Amount"/>
/// as stock, feeding the existing landed-cost allocation (#120/#143 follow-up, #164). DNA Ventanilla Única /
/// Marangatu are not integrated - only their resulting reference numbers are recorded here.
/// </summary>
public class GoodsImportDocument : BaseDocument
{
	private string proformaNumber = "";
	private Incoterm incoterm;
	private string brokerName = "";
	private string duaReference = "";
	private DateTime date;


	public GoodsImportDocument()
	{
	}


	/// <summary>
	/// Proforma/PO reference, business key
	/// </summary>
	public string ProformaNumber
	{
		get { return this.proformaNumber; }
		set { this.SetField(ref this.proformaNumber, value); }
	}

	public Incoterm Incoterm
	{
		get { return this.incoterm; }
		set { this.SetField(ref this.incoterm, value); }
	}

	/// <summary>
	/// Despachante de aduanas; legally required for a customs import
	/// </summary>
	public string BrokerName
	{
		get { return this.brokerName; }
		set { this.SetField(ref this.brokerName, value); }
	}

	/// <summary>
	/// DUA (Declaración Única de Aduanas) reference number
	/// </summary>
	public string DuaReference
	{
		get { return this.duaReference; }
		set { this.SetField(ref this.duaReference, value); }
	}

	public DateTime Date
	{
		get { return this.date; }
		set { this.SetField(ref this.date, value); }
	}

	public Supplier Supplier { get; set; } = null!;

	public List<GoodsImportDocumentItem> Items { get; set; } = [];
}
