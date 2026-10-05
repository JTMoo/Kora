using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class PendingDebitNoteTransmissionConfiguration : IEntityTypeConfiguration<PendingDebitNoteTransmission>
{
	public void Configure(EntityTypeBuilder<PendingDebitNoteTransmission> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasOne(transmission => transmission.DebitNote).WithMany().IsRequired();
		builder.Navigation(transmission => transmission.DebitNote).AutoInclude();

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(transmission => transmission.NextAttemptAt).HasColumnType("timestamp without time zone");
	}
}
