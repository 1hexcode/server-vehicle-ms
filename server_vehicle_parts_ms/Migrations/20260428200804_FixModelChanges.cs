using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server_vehicle_parts_ms.Migrations
{
    /// <inheritdoc />
    public partial class FixModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "VehicleParts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleParts_VendorId",
                table: "VehicleParts",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleParts_Vendors_VendorId",
                table: "VehicleParts",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleParts_Vendors_VendorId",
                table: "VehicleParts");

            migrationBuilder.DropIndex(
                name: "IX_VehicleParts_VendorId",
                table: "VehicleParts");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "VehicleParts");
        }
    }
}
