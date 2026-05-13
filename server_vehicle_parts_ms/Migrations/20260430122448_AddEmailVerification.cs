using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server_vehicle_parts_ms.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw idempotent SQL so this migration is safe to re-run
            // even if the columns were added outside of EF migrations.
            migrationBuilder.Sql("""
                ALTER TABLE "Users"
                    ADD COLUMN IF NOT EXISTS "EmailVerificationToken"       text,
                    ADD COLUMN IF NOT EXISTS "EmailVerificationTokenExpiry" timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS "IsEmailVerified"              boolean NOT NULL DEFAULT false;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_EmailVerificationToken"
                    ON "Users" ("EmailVerificationToken")
                    WHERE "EmailVerificationToken" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");
        }
    }
}
