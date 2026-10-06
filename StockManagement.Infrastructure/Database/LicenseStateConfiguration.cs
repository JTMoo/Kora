using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class LicenseStateConfiguration : IEntityTypeConfiguration<LicenseState>
{
	public void Configure(EntityTypeBuilder<LicenseState> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
	}
}
