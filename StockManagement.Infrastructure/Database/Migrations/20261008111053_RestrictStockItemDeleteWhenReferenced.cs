using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RestrictStockItemDeleteWhenReferenced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsImportDocumentItems_StockItems_StockItemId",
                table: "GoodsImportDocumentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_StockItems_StockItemId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RemissionNoteItems_StockItems_StockItemId",
                table: "RemissionNoteItems");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsImportDocumentItems_StockItems_StockItemId",
                table: "GoodsImportDocumentItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_StockItems_StockItemId",
                table: "InvoiceItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RemissionNoteItems_StockItems_StockItemId",
                table: "RemissionNoteItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsImportDocumentItems_StockItems_StockItemId",
                table: "GoodsImportDocumentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_StockItems_StockItemId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RemissionNoteItems_StockItems_StockItemId",
                table: "RemissionNoteItems");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsImportDocumentItems_StockItems_StockItemId",
                table: "GoodsImportDocumentItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_StockItems_StockItemId",
                table: "InvoiceItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RemissionNoteItems_StockItems_StockItemId",
                table: "RemissionNoteItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
