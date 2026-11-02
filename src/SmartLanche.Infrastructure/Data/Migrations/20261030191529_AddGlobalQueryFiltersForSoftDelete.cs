using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLanche.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalQueryFiltersForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Products_ProdutoId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ProdutoId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "ProdutoId",
                table: "InventoryItems",
                newName: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems",
                column: "ProductId",
                unique: true,
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Products_ProductId",
                table: "InventoryItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Products_ProductId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "InventoryItems",
                newName: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProdutoId",
                table: "InventoryItems",
                column: "ProdutoId",
                unique: true,
                filter: "[ProdutoId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Products_ProdutoId",
                table: "InventoryItems",
                column: "ProdutoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
