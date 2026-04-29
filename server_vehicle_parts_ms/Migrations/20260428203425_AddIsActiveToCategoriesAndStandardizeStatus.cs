using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server_vehicle_parts_ms.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToCategoriesAndStandardizeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isActive",
                table: "Vendors",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "isActive",
                table: "VehicleParts",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "isActive",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PartCategories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PartCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "PartCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PartCategories");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Vendors",
                newName: "isActive");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "VehicleParts",
                newName: "isActive");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Users",
                newName: "isActive");
        }
    }
}
