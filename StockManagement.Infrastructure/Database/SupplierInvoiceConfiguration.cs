using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
	public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasIndex(invoice => invoice.Number).IsUnique();
		builder.HasOne(invoice => invoice.Supplier).WithMany().IsRequired();
		builder.Property(invoice => invoice.Total).HasPrecision(18, 2);

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(invoice => invoice.Date).HasColumnType("timestamp without time zone");
		builder.Property(invoice => invoice.ExpirationDate).HasColumnType("timestamp without time zone");

		builder.OwnsMany(invoice => invoice.Payments, payment =>
		{
			payment.WithOwner().HasForeignKey("SupplierInvoiceId");
			payment.Property<Guid>("Id").ValueGeneratedOnAdd();
			payment.HasKey("Id");
			payment.Property(p => p.Amount).HasPrecision(18, 2);
			payment.Property(p => p.Date).HasColumnType("timestamp without time zone");
			payment.ToTable("SupplierPayments");
		});
		builder.Navigation(invoice => invoice.Payments).AutoInclude();
		builder.Navigation(invoice => invoice.Supplier).AutoInclude();

		builder.OwnsMany(invoice => invoice.Items, item =>
		{
			item.WithOwner().HasForeignKey("SupplierInvoiceId");
			item.Property<Guid>("Id").ValueGeneratedOnAdd();
			item.HasKey("Id");
			item.Property(line => line.UnitPrice).HasPrecision(18, 2);
			item.HasOne(line => line.StockItem).WithMany().IsRequired();
			item.Navigation(line => line.StockItem).AutoInclude();
			item.ToTable("SupplierInvoiceItems");
		});
		builder.Navigation(invoice => invoice.Items).AutoInclude();
	}
}
