using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLanche.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Products_ProductId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_InventoryItems_InventoryItemId",
                table: "InventoryMovements");

            // Drop tables
            migrationBuilder.DropTable(name: "InventoryMovements");
            migrationBuilder.DropTable(name: "InventoryItems");

            // Recreate InventoryItems table with new schema
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProdutoId = table.Column<int>(type: "int", nullable: true),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    QuantidadeAtual = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantidadeMinima = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnidadeMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Products_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProdutoId",
                table: "InventoryItems",
                column: "ProdutoId",
                unique: true,
                filter: "[ProdutoId] IS NOT NULL");

            // Recreate InventoryMovements table with new schema
            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryItemId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ComboId = table.Column<int>(type: "int", nullable: true),
                    Fornecedor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ComboId",
                table: "InventoryMovements",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_DataHora",
                table: "InventoryMovements",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_InventoryItemId",
                table: "InventoryMovements",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Motivo",
                table: "InventoryMovements",
                column: "Motivo");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OrderId",
                table: "InventoryMovements",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Tipo",
                table: "InventoryMovements",
                column: "Tipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Products_ProdutoId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_ComboId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_DataHora",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_Motivo",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_OrderId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_Tipo",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ProdutoId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ComboId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "DataHora",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Fornecedor",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Quantidade",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "ValorUnitario",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "QuantidadeAtual",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "QuantidadeMinima",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UnidadeMedida",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ValorUnitario",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "InventoryMovements",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "Observacoes",
                table: "InventoryMovements",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "Motivo",
                table: "InventoryMovements",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InventoryItems",
                newName: "ProductId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryMovements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NewQuantity",
                table: "InventoryMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "InventoryMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PreviousQuantity",
                table: "InventoryMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "InventoryMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceId",
                table: "InventoryMovements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMovementAt",
                table: "InventoryItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinQuantity",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "InventoryItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_CreatedAt",
                table: "InventoryMovements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_MinQuantity",
                table: "InventoryItems",
                column: "MinQuantity");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Products_ProductId",
                table: "InventoryItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Products_ProductId",
                table: "InventoryMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
