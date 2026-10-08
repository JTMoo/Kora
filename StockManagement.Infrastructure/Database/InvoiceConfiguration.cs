using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
	public void Configure(EntityTypeBuilder<Invoice> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasIndex(invoice => invoice.Number).IsUnique();
		builder.HasOne(invoice => invoice.Customer).WithMany().IsRequired();
		builder.Property(invoice => invoice.Total).HasPrecision(18, 2);
		builder.Property(invoice => invoice.Tax).HasPrecision(18, 2);

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(invoice => invoice.Date).HasColumnType("timestamp without time zone");
		builder.Property(invoice => invoice.ExpirationDate).HasColumnType("timestamp without time zone");
		builder.Property(invoice => invoice.AcceptedAt).HasColumnType("timestamp without time zone");

		builder.OwnsMany(invoice => invoice.Items, item =>
		{
			item.WithOwner().HasForeignKey("InvoiceId");
			item.Property<Guid>("Id").ValueGeneratedOnAdd();
			item.HasKey("Id");
			item.HasOne(cartItem => cartItem.StockItem).WithMany().IsRequired().OnDelete(DeleteBehavior.Restrict);
			item.Navigation(cartItem => cartItem.StockItem).AutoInclude();
			item.ToTable("InvoiceItems");
		});
		builder.OwnsMany(invoice => invoice.Payments, payment =>
		{
			payment.WithOwner().HasForeignKey("InvoiceId");
			payment.Property<Guid>("Id").ValueGeneratedOnAdd();
			payment.HasKey("Id");
			payment.Property(p => p.Amount).HasPrecision(18, 2);
			payment.Property(p => p.Date).HasColumnType("timestamp without time zone");
			payment.ToTable("Payments");
		});
		builder.Navigation(invoice => invoice.Items).AutoInclude();
		builder.Navigation(invoice => invoice.Payments).AutoInclude();
		builder.Navigation(invoice => invoice.Customer).AutoInclude();
	}
}
