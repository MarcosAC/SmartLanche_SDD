using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLanche.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameObservacoesToNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "PendingPayments",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "PendingPaymentHistory",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "Payments",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "Orders",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "InventoryMovements",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "InventoryItems",
                newName: "Notes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "PendingPayments",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "PendingPaymentHistory",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Payments",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Orders",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "InventoryMovements",
                newName: "Observacoes");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "InventoryItems",
                newName: "Observacoes");
        }
    }
}
