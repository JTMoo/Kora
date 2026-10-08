using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class RemissionNoteConfiguration : IEntityTypeConfiguration<RemissionNote>
{
	public void Configure(EntityTypeBuilder<RemissionNote> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasIndex(remissionNote => remissionNote.Number).IsUnique();
		builder.HasOne(remissionNote => remissionNote.Customer).WithMany().IsRequired();

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(remissionNote => remissionNote.Date).HasColumnType("timestamp without time zone");

		builder.OwnsMany(remissionNote => remissionNote.Items, item =>
		{
			item.WithOwner().HasForeignKey("RemissionNoteId");
			item.Property<Guid>("Id").ValueGeneratedOnAdd();
			item.HasKey("Id");
			item.HasOne(remissionNoteItem => remissionNoteItem.StockItem).WithMany().IsRequired().OnDelete(DeleteBehavior.Restrict);
			item.Navigation(remissionNoteItem => remissionNoteItem.StockItem).AutoInclude();
			item.ToTable("RemissionNoteItems");
		});
		builder.Navigation(remissionNote => remissionNote.Items).AutoInclude();
		builder.Navigation(remissionNote => remissionNote.Customer).AutoInclude();
	}
}
