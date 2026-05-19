using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server_vehicle_parts_ms.Migrations
{
    /// <inheritdoc />
    public partial class AddHotDeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HotDeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotDeals_VehicleParts_PartId",
                        column: x => x.PartId,
                        principalTable: "VehicleParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HotDeals_IsActive_StartsAt_EndsAt",
                table: "HotDeals",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HotDeals_PartId",
                table: "HotDeals",
                column: "PartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HotDeals");
        }
    }
}
