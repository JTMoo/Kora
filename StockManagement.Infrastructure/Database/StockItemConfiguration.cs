using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
	public const string BarcodeIndexName = "IX_StockItems_Barcode";


	public void Configure(EntityTypeBuilder<StockItem> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasIndex(item => item.Code).IsUnique();
		builder.HasIndex(item => item.Barcode)
			.IsUnique()
			.HasDatabaseName(BarcodeIndexName)
			.HasFilter("\"Barcode\" <> ''");
		builder.Property(item => item.Price).HasPrecision(18, 2);
		builder.Property(item => item.Factor).HasPrecision(18, 2);
		builder.Property(item => item.PurchasePrice).HasPrecision(18, 2);
		builder.Property(item => item.PurchaseExchangeRate).HasPrecision(18, 6);
		builder.Property(item => item.AdditionalPurchaseCost).HasPrecision(18, 2);
		builder.Property(item => item.VatRatePercent).HasColumnType("numeric(5,2)");
		builder.HasOne(item => item.Supplier).WithMany().HasForeignKey(item => item.SupplierId).IsRequired(false);
	}
}
