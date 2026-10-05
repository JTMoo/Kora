using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class CashRegisterSessionConfiguration : IEntityTypeConfiguration<CashRegisterSession>
{
	public void Configure(EntityTypeBuilder<CashRegisterSession> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.Property(session => session.OpeningFloat).HasPrecision(18, 2);
		builder.Property(session => session.CountedAmount).HasPrecision(18, 2);

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(session => session.OpenedAt).HasColumnType("timestamp without time zone");
		builder.Property(session => session.ClosedAt).HasColumnType("timestamp without time zone");

		builder.OwnsMany(session => session.Movements, movement =>
		{
			movement.WithOwner().HasForeignKey("CashRegisterSessionId");
			movement.Property<Guid>("Id").ValueGeneratedOnAdd();
			movement.HasKey("Id");
			movement.Property(m => m.Amount).HasPrecision(18, 2);
			movement.Property(m => m.Date).HasColumnType("timestamp without time zone");
			movement.ToTable("CashMovements");
		});
		builder.Navigation(session => session.Movements).AutoInclude();
	}
}
