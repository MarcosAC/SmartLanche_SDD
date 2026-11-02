using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLanche.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPreparationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparationStartedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PreparationStartedAt",
                table: "Orders",
                column: "PreparationStartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PreparationStartedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PreparationStartedAt",
                table: "Orders");
        }
    }
}
