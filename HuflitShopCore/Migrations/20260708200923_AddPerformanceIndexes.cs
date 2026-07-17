using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StaffRequests_Status_CreatedAt",
                table: "StaffRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate_OrderStatus",
                table: "Orders",
                columns: new[] { "OrderDate", "OrderStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TransactionDate",
                table: "InventoryTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ReceivedDate",
                table: "InventoryLots",
                column: "ReceivedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffRequests_Status_CreatedAt",
                table: "StaffRequests");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate_OrderStatus",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_TransactionDate",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLots_ReceivedDate",
                table: "InventoryLots");
        }
    }
}
