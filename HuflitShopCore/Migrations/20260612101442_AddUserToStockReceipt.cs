using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToStockReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "StockReceived",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("UPDATE dbo.StockReceived SET UserId = 'ID_CUA_ADMIN_USER' WHERE 1=1");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceived_UserId",
                table: "StockReceived",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockReceived_Users_UserId",
                table: "StockReceived",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockReceived_Users_UserId",
                table: "StockReceived");

            migrationBuilder.DropIndex(
                name: "IX_StockReceived_UserId",
                table: "StockReceived");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "StockReceived");
        }
    }
}
