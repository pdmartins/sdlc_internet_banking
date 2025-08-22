using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaAndRateLimitEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MfaSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MfaSessions_Email",
                table: "MfaSessions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_MfaSessions_ExpiresAt",
                table: "MfaSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaSessions_IsUsed",
                table: "MfaSessions",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_MfaSessions_UserId",
                table: "MfaSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MfaSessions");
        }
    }
}
