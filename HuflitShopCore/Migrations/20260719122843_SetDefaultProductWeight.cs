using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultProductWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WeightGrams",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 300,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("UPDATE [Product] SET [WeightGrams] = 300 WHERE [WeightGrams] <= 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WeightGrams",
                table: "Product",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 300);
        }
    }
}
