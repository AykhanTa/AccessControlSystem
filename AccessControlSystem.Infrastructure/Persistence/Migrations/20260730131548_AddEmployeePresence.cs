using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentPresence",
                table: "Employees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EmployeeId",
                table: "AccessEvents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_EmployeeId",
                table: "AccessEvents",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessEvents_Employees_EmployeeId",
                table: "AccessEvents",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessEvents_Employees_EmployeeId",
                table: "AccessEvents");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_EmployeeId",
                table: "AccessEvents");

            migrationBuilder.DropColumn(
                name: "CurrentPresence",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AccessEvents");
        }
    }
}
