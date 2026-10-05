using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManagement.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDebitNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DebitNotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    InvoiceId = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Cdc = table.Column<string>(type: "text", nullable: false),
                    TransmissionStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebitNotes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebitNoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    VatRatePercent = table.Column<int>(type: "integer", nullable: false),
                    DebitNoteId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitNoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebitNoteItems_DebitNotes_DebitNoteId",
                        column: x => x.DebitNoteId,
                        principalTable: "DebitNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingDebitNoteTransmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DebitNoteId = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingDebitNoteTransmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingDebitNoteTransmissions_DebitNotes_DebitNoteId",
                        column: x => x.DebitNoteId,
                        principalTable: "DebitNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DebitNoteItems_DebitNoteId",
                table: "DebitNoteItems",
                column: "DebitNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_InvoiceId",
                table: "DebitNotes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_Number",
                table: "DebitNotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingDebitNoteTransmissions_DebitNoteId",
                table: "PendingDebitNoteTransmissions",
                column: "DebitNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebitNoteItems");

            migrationBuilder.DropTable(
                name: "PendingDebitNoteTransmissions");

            migrationBuilder.DropTable(
                name: "DebitNotes");
        }
    }
}
