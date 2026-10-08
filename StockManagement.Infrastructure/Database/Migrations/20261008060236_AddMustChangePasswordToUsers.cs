using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePasswordToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // The seeded admin (AddUsers) must rotate its known-from-source password before using anything else (#244)
            migrationBuilder.Sql("UPDATE \"Users\" SET \"MustChangePassword\" = true WHERE \"Id\" = '00000000-0000-0000-0000-000000000001'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");
        }
    }
}
