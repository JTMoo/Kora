using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultPrinterName",
                table: "AppSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "KudeFormat",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PrintOnSaleComplete",
                table: "AppSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptPaperWidthMm",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPrinterName",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "KudeFormat",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "PrintOnSaleComplete",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ReceiptPaperWidthMm",
                table: "AppSettings");
        }
    }
}
