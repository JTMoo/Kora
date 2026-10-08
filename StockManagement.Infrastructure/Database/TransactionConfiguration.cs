using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
	public void Configure(EntityTypeBuilder<Transaction> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		// Cascade, not Restrict: DeleteStockItemAsync itself inserts a Deletion-kind Transaction in the same SaveChanges
		// call that removes the StockItem, so Restrict would make every delete fail its own audit write.
		builder.HasOne(transaction => transaction.StockItem).WithMany().IsRequired();

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(transaction => transaction.Time).HasColumnType("timestamp without time zone");

		// Invoice is not persisted yet: it still lives in Mongo (#65 follow-up)
		builder.Ignore(transaction => transaction.Invoice);
	}
}
