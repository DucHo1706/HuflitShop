using System;
using HuflitShopCore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuflitShopCore.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260723120000_AddOrderStatusTimestamps")]
    public partial class AddOrderStatusTimestamps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackingStartedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippingStartedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ApprovedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "PackingStartedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "ShippingStartedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "CompletedAt", table: "Orders");
        }
    }
}
