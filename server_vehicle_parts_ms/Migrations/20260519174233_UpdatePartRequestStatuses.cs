using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server_vehicle_parts_ms.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartRequestStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old enum: Requested, Sourced, Notified, Closed, Rejected
            // New enum: Requested, Processing, Fulfilled, Rejected
            // Status is stored as text (HasConversion<string>), so translate existing rows.
            migrationBuilder.Sql("UPDATE \"PartRequests\" SET \"Status\" = 'Processing' WHERE \"Status\" = 'Sourced';");
            migrationBuilder.Sql("UPDATE \"PartRequests\" SET \"Status\" = 'Fulfilled' WHERE \"Status\" IN ('Notified', 'Closed');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"PartRequests\" SET \"Status\" = 'Sourced' WHERE \"Status\" = 'Processing';");
            migrationBuilder.Sql("UPDATE \"PartRequests\" SET \"Status\" = 'Closed' WHERE \"Status\" = 'Fulfilled';");
        }
    }
}
