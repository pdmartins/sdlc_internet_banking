using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoBank.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePhoneColumnLengthNoCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsAnomalous = table.Column<bool>(type: "bit", nullable: false),
                    AnomalyReasons = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    ResponseAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypicalIpAddresses = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TypicalLocations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TypicalDevices = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TypicalLoginHours = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TypicalDaysOfWeek = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreferredTimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstLoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalSuccessfulLogins = table.Column<int>(type: "int", nullable: false),
                    TotalFailedLogins = table.Column<int>(type: "int", nullable: false),
                    LocationRiskThreshold = table.Column<int>(type: "int", nullable: false),
                    TimeRiskThreshold = table.Column<int>(type: "int", nullable: false),
                    DeviceRiskThreshold = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginPatterns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnomalyDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnomalyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponseAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyDetections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnomalyDetections_LoginAttempts_LoginAttemptId",
                        column: x => x.LoginAttemptId,
                        principalTable: "LoginAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnomalyDetections_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnomalyDetections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DeliveryMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAction = table.Column<bool>(type: "bit", nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActionText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionTakenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoginAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnomalyDetectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_AnomalyDetections_AnomalyDetectionId",
                        column: x => x.AnomalyDetectionId,
                        principalTable: "AnomalyDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_LoginAttempts_LoginAttemptId",
                        column: x => x.LoginAttemptId,
                        principalTable: "LoginAttempts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SecurityAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_AnomalyType",
                table: "AnomalyDetections",
                column: "AnomalyType");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_DetectedAt",
                table: "AnomalyDetections",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_IsResolved",
                table: "AnomalyDetections",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_LoginAttemptId",
                table: "AnomalyDetections",
                column: "LoginAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_ResolvedByUserId",
                table: "AnomalyDetections",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_Severity",
                table: "AnomalyDetections",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyDetections_UserId",
                table: "AnomalyDetections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_DeviceFingerprint",
                table: "LoginAttempts",
                column: "DeviceFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email",
                table: "LoginAttempts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpAddress",
                table: "LoginAttempts",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IsAnomalous",
                table: "LoginAttempts",
                column: "IsAnomalous");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IsSuccessful",
                table: "LoginAttempts",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId",
                table: "LoginAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AlertType",
                table: "SecurityAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AnomalyDetectionId",
                table: "SecurityAlerts",
                column: "AnomalyDetectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_CreatedAt",
                table: "SecurityAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_IsRead",
                table: "SecurityAlerts",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_LoginAttemptId",
                table: "SecurityAlerts",
                column: "LoginAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Severity",
                table: "SecurityAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status",
                table: "SecurityAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UserId",
                table: "SecurityAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginPatterns_LastUpdatedAt",
                table: "UserLoginPatterns",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginPatterns_UserId",
                table: "UserLoginPatterns",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAlerts");

            migrationBuilder.DropTable(
                name: "UserLoginPatterns");

            migrationBuilder.DropTable(
                name: "AnomalyDetections");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
