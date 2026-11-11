using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerce.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableStock = table.Column<int>(type: "integer", nullable: false),
                    PromoPercent = table.Column<int>(type: "integer", nullable: true),
                    PromoValidUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "AvailableStock", "Description", "Name", "Price", "PromoPercent", "PromoValidUntil" },
                values: new object[,]
                {
                    { 1, 50, "Hot-swappable RGB keyboard", "Mechanical Keyboard", 119.99m, 30, new DateOnly(2026, 11, 15) },
                    { 2, 120, "Ergonomic 8k DPI mouse", "Wireless Mouse", 49.50m, null, null },
                    { 3, 25, "27-inch IPS display", "4K Monitor", 329.00m, null, null },
                    { 4, 200, "7-in-1 docking hub", "USB-C Hub", 39.99m, null, null }
                });

            // No manual sequence reset is needed here: the Npgsql migrations generator appends a
            // setval after InsertData rows with explicit ids (next value = MAX("Id") + 1).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
