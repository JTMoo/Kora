namespace StockManagement.Kernel.Model.Types;


/// <summary>
/// ICC Incoterms 2020 rule for a <see cref="GoodsImportDocument"/> (#164)
/// </summary>
public enum Incoterm
{
	Exw = 0,
	Fca,
	Fob,
	Cfr,
	Cif,
	Cpt,
	Cip,
	Dap,
	Ddp,
}
