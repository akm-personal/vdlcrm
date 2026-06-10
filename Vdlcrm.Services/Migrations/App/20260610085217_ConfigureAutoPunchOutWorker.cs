using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vdlcrm.Services.Migrations.App
{
    /// <inheritdoc />
    public partial class ConfigureAutoPunchOutWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AttendanceRadius",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3745));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutDayEnd",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3753));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutDayStart",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3752));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3746));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutWorkerEnabled",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3748));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutWorkerIntervalHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3749));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutWorkerMode",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3750));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "LibraryLatitude",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3555));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "LibraryLongitude",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 10, 8, 52, 16, 384, DateTimeKind.Utc).AddTicks(3743));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AttendanceRadius",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5191));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutDayEnd",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5199));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutDayStart",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5198));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5193));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutWorkerEnabled",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5194));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutWorkerIntervalHours",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5195));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "AutoPunchOutWorkerMode",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5197));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "LibraryLatitude",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(4896));

            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Key",
                keyValue: "LibraryLongitude",
                column: "UpdatedAt",
                value: new DateTime(2026, 6, 9, 19, 54, 30, 844, DateTimeKind.Utc).AddTicks(5189));
        }
    }
}
