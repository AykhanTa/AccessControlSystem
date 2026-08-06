using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceTimetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AbsentAfterMinutes",
                table: "WorkSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckInEnd",
                table: "WorkSchedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckInStart",
                table: "WorkSchedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckOutEnd",
                table: "WorkSchedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckOutStart",
                table: "WorkSchedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "WorkSchedules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveGraceMinutes",
                table: "WorkSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinWorkMinutes",
                table: "WorkSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "WorkSchedules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsentAfterMinutes",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "CheckInEnd",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "CheckInStart",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "CheckOutEnd",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "CheckOutStart",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "EarlyLeaveGraceMinutes",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "MinWorkMinutes",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "WorkSchedules");
        }
    }
}
