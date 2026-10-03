using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;
using StockManagement.Kernel.Database;

namespace StockManagement.Kernel.Model;


[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.stockItem))]
public class StockItem : BaseDocument
{
	private string _code = string.Empty;
	private string _barcode = string.Empty;
	private string _description = string.Empty;
	private string _location = string.Empty;
	private string _name = string.Empty;
	private string _miscellaneous = string.Empty;
	private string _manufacturer = string.Empty;
	private decimal _price;
	private decimal _factor;
	private decimal _purchasePrice;
	private decimal _purchaseExchangeRate;
	private decimal _additionalPurchaseCost;
	private int _amount;
	private string? _supplierId;
	private int _minimumStock;
	private decimal _vatRatePercent;

	public StockItem ()
	{
	}

	public StockItem (string name, string code = "", string description = "", int amount=1, decimal price = 0, string manufacturer = "", string misc = "")
	{
		this.Name = name;
		this.Code = code;
		this.Description = description;
		this.Amount = amount;
		this.Price = price;
		this.Manufacturer = manufacturer;
		this.Miscellaneous = misc;
	}

	[Display(ResourceType = typeof(Language.Common), Name = nameof(Language.Common.name))]
	public string Name
	{
		get { return _name; }
		set { this.SetField(ref _name, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.code))]
	public string Code
	{
		get { return _code; }
		set { this.SetField(ref _code, value); }
	}

	/// <remarks>EAN-8/EAN-13/UPC-A, optional; scanned at point of sale instead of typing <see cref="Code"/> (#165)</remarks>
	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.barcode))]
	public string Barcode
	{
		get { return _barcode; }
		set { this.SetField(ref _barcode, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.amount))]
	public int Amount
	{
		get { return _amount; }
		set { this.SetField(ref _amount, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.description))]
	public string Description
	{
		get { return _description; }
		set { this.SetField(ref _description, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.location))]
	public string Location
	{
		get { return _location; }
		set { this.SetField(ref _location, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.price))]
	public decimal Price
	{
		get { return _price; }
		set { this.SetField(ref _price, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.factor))]
	public decimal Factor
	{
		get { return _factor; }
		set { this.SetField(ref _factor, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.purchasePrice))]
	public decimal PurchasePrice
	{
		get { return _purchasePrice; }
		set { this.SetField(ref _purchasePrice, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.purchaseExchangeRate))]
	public decimal PurchaseExchangeRate
	{
		get { return _purchaseExchangeRate; }
		set { this.SetField(ref _purchaseExchangeRate, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.additionalPurchaseCost))]
	public decimal AdditionalPurchaseCost
	{
		get { return _additionalPurchaseCost; }
		set { this.SetField(ref _additionalPurchaseCost, value); }
	}

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.manufacturer))]
	[BsonSerializer(typeof(ManufacturerSerializer))]
	public string Manufacturer
	{
		get { return _manufacturer; }
		set { this.SetField(ref _manufacturer, value?.Trim() ?? string.Empty); }
	}

	[Display(ResourceType = typeof(Language.Common), Name = nameof(Language.Common.miscellaneous))]
	public string Miscellaneous
	{
		get { return _miscellaneous; }
		set { this.SetField(ref _miscellaneous, value); }
	}

	[Display(ResourceType = typeof(Language.Suppliers), Name = nameof(Language.Suppliers.supplier))]
	public string? SupplierId
	{
		get { return _supplierId; }
		set { this.SetField(ref _supplierId, value); }
	}

	/// <remarks>Optional; navigation to the preferred supplier for reordering (#57)</remarks>
	public Supplier? Supplier { get; set; }

	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.minimumStock))]
	public int MinimumStock
	{
		get { return _minimumStock; }
		set { this.SetField(ref _minimumStock, value); }
	}

	/// <remarks>Per-item VAT rate (e.g. 5/10/0 for PY's basic-basket exemptions); defaults from <c>CompanySettings.VatRatePercent</c> on create (ADR-0031).</remarks>
	[Display(ResourceType = typeof(Language.StockItems), Name = nameof(Language.StockItems.vatRatePercent))]
	public decimal VatRatePercent
	{
		get { return _vatRatePercent; }
		set { this.SetField(ref _vatRatePercent, value); }
	}
}