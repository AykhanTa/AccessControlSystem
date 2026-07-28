using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    AccessNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Granted = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Raw = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessEvents_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AccessEvents_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_AccessNumber",
                table: "AccessEvents",
                column: "AccessNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_DeviceId",
                table: "AccessEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_OccurredAt",
                table: "AccessEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_VisitId",
                table: "AccessEvents",
                column: "VisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessEvents");
        }
    }
}
