using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class CancellationRequestConfiguration : IEntityTypeConfiguration<CancellationRequest>
{
	public void Configure(EntityTypeBuilder<CancellationRequest> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.HasOne(request => request.Invoice).WithMany().IsRequired();
		builder.Navigation(request => request.Invoice).AutoInclude();

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(request => request.RequestedAt).HasColumnType("timestamp without time zone");
		builder.Property(request => request.NextAttemptAt).HasColumnType("timestamp without time zone");
	}
}
