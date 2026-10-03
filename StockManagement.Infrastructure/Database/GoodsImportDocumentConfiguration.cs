using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class GoodsImportDocumentConfiguration : IEntityTypeConfiguration<GoodsImportDocument>
{
	public void Configure(EntityTypeBuilder<GoodsImportDocument> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasIndex(document => document.ProformaNumber).IsUnique();
		builder.HasOne(document => document.Supplier).WithMany().IsRequired();

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(document => document.Date).HasColumnType("timestamp without time zone");

		builder.OwnsMany(document => document.Items, item =>
		{
			item.WithOwner().HasForeignKey("GoodsImportDocumentId");
			item.Property<Guid>("Id").ValueGeneratedOnAdd();
			item.HasKey("Id");
			item.HasOne(documentItem => documentItem.StockItem).WithMany().IsRequired();
			item.Navigation(documentItem => documentItem.StockItem).AutoInclude();
			item.ToTable("GoodsImportDocumentItems");
		});
		builder.Navigation(document => document.Items).AutoInclude();
		builder.Navigation(document => document.Supplier).AutoInclude();
	}
}
