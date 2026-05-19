using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server_vehicle_parts_ms.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderScheduleCustomMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomMessage",
                table: "ReminderSchedules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomMessage",
                table: "ReminderSchedules");
        }
    }
}
