using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DayStartBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DayStartTime",
                table: "WorkSchedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayStartTime",
                table: "WorkSchedules");

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
        }
    }
}
