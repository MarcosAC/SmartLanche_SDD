using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLanche.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComboIdToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComboId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ComboId",
                table: "OrderItems",
                column: "ComboId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Combos_ComboId",
                table: "OrderItems",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Combos_ComboId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ComboId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ComboId",
                table: "OrderItems");
        }
    }
}
