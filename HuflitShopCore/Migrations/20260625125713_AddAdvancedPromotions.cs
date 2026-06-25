using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicableProductId",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComboProductIds",
                table: "Promotions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoApply",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicableProductId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ComboProductIds",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsAutoApply",
                table: "Promotions");
        }
    }
}
