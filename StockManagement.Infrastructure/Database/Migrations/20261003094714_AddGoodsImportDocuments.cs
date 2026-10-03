using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsImportDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodsImportDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProformaNumber = table.Column<string>(type: "text", nullable: false),
                    Incoterm = table.Column<int>(type: "integer", nullable: false),
                    BrokerName = table.Column<string>(type: "text", nullable: false),
                    DuaReference = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SupplierId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsImportDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsImportDocuments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsImportDocumentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockItemId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    GoodsImportDocumentId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsImportDocumentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsImportDocumentItems_GoodsImportDocuments_GoodsImportDo~",
                        column: x => x.GoodsImportDocumentId,
                        principalTable: "GoodsImportDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsImportDocumentItems_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsImportDocumentItems_GoodsImportDocumentId",
                table: "GoodsImportDocumentItems",
                column: "GoodsImportDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsImportDocumentItems_StockItemId",
                table: "GoodsImportDocumentItems",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsImportDocuments_ProformaNumber",
                table: "GoodsImportDocuments",
                column: "ProformaNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsImportDocuments_SupplierId",
                table: "GoodsImportDocuments",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsImportDocumentItems");

            migrationBuilder.DropTable(
                name: "GoodsImportDocuments");
        }
    }
}
