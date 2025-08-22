using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRateLimitEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateLimitEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AttemptType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    SuccessfulCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    FirstAttempt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttempt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitEntries_BlockedUntil",
                table: "RateLimitEntries",
                column: "BlockedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitEntries_ClientIdentifier_AttemptType",
                table: "RateLimitEntries",
                columns: new[] { "ClientIdentifier", "AttemptType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitEntries_IsBlocked",
                table: "RateLimitEntries",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitEntries_LastAttempt",
                table: "RateLimitEntries",
                column: "LastAttempt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateLimitEntries");
        }
    }
}
