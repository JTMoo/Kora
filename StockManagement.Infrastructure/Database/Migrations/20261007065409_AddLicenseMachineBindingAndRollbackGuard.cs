using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseMachineBindingAndRollbackGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HighWaterMarkUtc",
                table: "LicenseStates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "LicenseStates",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighWaterMarkUtc",
                table: "LicenseStates");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "LicenseStates");
        }
    }
}
