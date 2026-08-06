using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalTimetables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OwnerEmployeeId",
                table: "WorkSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_OwnerEmployeeId",
                table: "WorkSchedules",
                column: "OwnerEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_OwnerEmployeeId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "OwnerEmployeeId",
                table: "WorkSchedules");
        }
    }
}
