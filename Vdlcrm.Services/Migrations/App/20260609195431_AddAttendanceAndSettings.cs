using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vdlcrm.Services.Migrations.App
{
    /// <inheritdoc />
    public partial class AddAttendanceAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VdlId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    PunchInTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PunchInLatitude = table.Column<double>(type: "REAL", nullable: true),
                    PunchInLongitude = table.Column<double>(type: "REAL", nullable: true),
                    PunchOutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PunchOutLatitude = table.Column<double>(type: "REAL", nullable: true),
                    PunchOutLongitude = table.Column<double>(type: "REAL", nullable: true),
                    IsAutoPunchedOut = table.Column<bool>(type: "INTEGER", nullable: false),
                    OvertimeMinutes = table.Column<double>(type: "REAL", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Key", "Description", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { "AttendanceRadius", "Allowed radius for punch in/out in meters", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5191), "50" },
                    { "AutoPunchOutDayEnd", "Day shift end time (HH:mm)", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5199), "20:00" },
                    { "AutoPunchOutDayStart", "Day shift start time (HH:mm)", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5198), "08:00" },
                    { "AutoPunchOutHours", "Hours after which a student is auto-punched out", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5193), "8" },
                    { "AutoPunchOutWorkerEnabled", "Enable background auto punch out job (true/false)", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5194), "true" },
                    { "AutoPunchOutWorkerIntervalHours", "How often background job runs (hours)", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5195), "5" },
                    { "AutoPunchOutWorkerMode", "When to run (Day/Night/Both)", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5197), "Day" },
                    { "LibraryLatitude", "Library exact latitude (Default: Delhi)", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(4896), "28.6139" },
                    { "LibraryLongitude", "Library exact longitude", new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5189), "77.2090" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "attendance_records");
        }
    }
}
