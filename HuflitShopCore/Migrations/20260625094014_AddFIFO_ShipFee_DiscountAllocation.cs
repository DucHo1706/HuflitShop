using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    /// <inheritdoc />
    public partial class AddFIFO_ShipFee_DiscountAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAllocation",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductVariantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StockReceivedDetailId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLots_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryLots_StockReceivedDetails_StockReceivedDetailId",
                        column: x => x.StockReceivedDetailId,
                        principalTable: "StockReceivedDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetailLots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderDetailId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InventoryLotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetailLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetailLots_InventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "InventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderDetailLots_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ProductVariantId",
                table: "InventoryLots",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_StockReceivedDetailId",
                table: "InventoryLots",
                column: "StockReceivedDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailLots_InventoryLotId",
                table: "OrderDetailLots",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailLots_OrderDetailId",
                table: "OrderDetailLots",
                column: "OrderDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetailLots");

            migrationBuilder.DropTable(
                name: "InventoryLots");

            migrationBuilder.DropColumn(
                name: "DiscountAllocation",
                table: "OrderDetails");
        }
    }
}
