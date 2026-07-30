using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCenters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CenterId",
                table: "Floors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FloorNumber",
                table: "Floors",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Centers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    WorkingHoursStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    WorkingHoursEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Centers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Floors_CenterId",
                table: "Floors",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Centers_Code",
                table: "Centers",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Floors_Centers_CenterId",
                table: "Floors",
                column: "CenterId",
                principalTable: "Centers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Floors_Centers_CenterId",
                table: "Floors");

            migrationBuilder.DropTable(
                name: "Centers");

            migrationBuilder.DropIndex(
                name: "IX_Floors_CenterId",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "CenterId",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "FloorNumber",
                table: "Floors");
        }
    }
}
