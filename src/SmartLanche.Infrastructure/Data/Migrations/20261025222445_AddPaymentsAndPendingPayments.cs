using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLanche.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndPendingPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FormaPagamento = table.Column<int>(type: "int", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Troco = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Parcelas = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Estornado = table.Column<bool>(type: "bit", nullable: false),
                    EstornoDataHora = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstornoMotivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ValorOriginal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusPagamento = table.Column<int>(type: "int", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingPaymentHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PendingPaymentId = table.Column<int>(type: "int", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FormaPagamento = table.Column<int>(type: "int", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPaymentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingPaymentHistory_PendingPayments_PendingPaymentId",
                        column: x => x.PendingPaymentId,
                        principalTable: "PendingPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPaymentHistory_PendingPaymentId",
                table: "PendingPaymentHistory",
                column: "PendingPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPayments_CustomerId",
                table: "PendingPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPayments_OrderId",
                table: "PendingPayments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingPayments_StatusPagamento",
                table: "PendingPayments",
                column: "StatusPagamento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PendingPaymentHistory");

            migrationBuilder.DropTable(
                name: "PendingPayments");
        }
    }
}
