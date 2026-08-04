using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitCompanyFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Visits_CompanyId",
                table: "Visits",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Companies_CompanyId",
                table: "Visits",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Companies_CompanyId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_CompanyId",
                table: "Visits");
        }
    }
}
