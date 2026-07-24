using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGhnShippingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeightGrams",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShippingDistrictId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShippingProvinceId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShippingWard",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingWardCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "Address",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "Address",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "Address",
                type: "nvarchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WardCode",
                table: "Address",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarrierOrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuotedFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CodAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeightGrams = table.Column<int>(type: "int", nullable: false),
                    LengthCm = table.Column<int>(type: "int", nullable: false),
                    WidthCm = table.Column<int>(type: "int", nullable: false),
                    HeightCm = table.Column<int>(type: "int", nullable: false),
                    ExpectedDeliveryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CarrierCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierOrderCode",
                table: "Shipments",
                column: "CarrierOrderCode",
                unique: true,
                filter: "[CarrierOrderCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropColumn(
                name: "WeightGrams",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "ShippingDistrictId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingProvinceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingWard",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingWardCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "Address");
        }
    }
}
