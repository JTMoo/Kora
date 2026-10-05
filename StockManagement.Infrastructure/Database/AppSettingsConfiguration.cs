using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


internal sealed class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
	public void Configure(EntityTypeBuilder<AppSettings> builder)
	{
		builder.Property<string>("Id").ValueGeneratedOnAdd();
		builder.HasKey("Id");
		builder.Property(settings => settings.VatRatePercent).HasColumnType("numeric(5,2)");
		builder.Property(settings => settings.CurrencyDecimalDigits).HasDefaultValue(0);
		builder.Property(settings => settings.EstablishmentCode).HasDefaultValue("001");
		builder.Property(settings => settings.PointOfSaleCode).HasDefaultValue("001");
		builder.Property(settings => settings.EstablishmentAddress).HasDefaultValue("");

		// DateTime.Now (Kind=Local); Npgsql only accepts UTC for "timestamp with time zone"
		builder.Property(settings => settings.TimbradoValidFrom).HasColumnType("timestamp without time zone");
		builder.Property(settings => settings.TimbradoValidTo).HasColumnType("timestamp without time zone");
	}
}
