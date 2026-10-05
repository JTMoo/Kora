using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class InvoiceNumberVoidConfiguration : IEntityTypeConfiguration<InvoiceNumberVoid>
{
	public void Configure(EntityTypeBuilder<InvoiceNumberVoid> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(numberVoid => numberVoid.RequestedAt).HasColumnType("timestamp without time zone");
		builder.Property(numberVoid => numberVoid.NextAttemptAt).HasColumnType("timestamp without time zone");
	}
}
