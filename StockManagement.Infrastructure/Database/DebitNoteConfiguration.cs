using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class DebitNoteConfiguration : IEntityTypeConfiguration<DebitNote>
{
	public void Configure(EntityTypeBuilder<DebitNote> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasIndex(debitNote => debitNote.Number).IsUnique();
		builder.HasOne(debitNote => debitNote.Invoice).WithMany().IsRequired();

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(debitNote => debitNote.Date).HasColumnType("timestamp without time zone");

		builder.OwnsMany(debitNote => debitNote.Items, item =>
		{
			item.WithOwner().HasForeignKey("DebitNoteId");
			item.Property<Guid>("Id").ValueGeneratedOnAdd();
			item.HasKey("Id");
			item.ToTable("DebitNoteItems");
		});
		builder.Navigation(debitNote => debitNote.Items).AutoInclude();
		builder.Navigation(debitNote => debitNote.Invoice).AutoInclude();
	}
}
