using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControlSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorsDevicesEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessNumber",
                table: "Visits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    UseHttps = table.Column<bool>(type: "bit", nullable: false),
                    DoorNo = table.Column<int>(type: "int", nullable: false),
                    FloorId = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitFloors",
                columns: table => new
                {
                    VisitId = table.Column<long>(type: "bigint", nullable: false),
                    FloorId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitFloors", x => new { x.VisitId, x.FloorId });
                    table.ForeignKey(
                        name: "FK_VisitFloors_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitFloors_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEnrollments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    AccessNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEnrollments_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceEnrollments_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_AccessNumber",
                table: "Visits",
                column: "AccessNumber",
                filter: "[AccessNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnrollments_DeviceId",
                table: "DeviceEnrollments",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnrollments_Status",
                table: "DeviceEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnrollments_VisitId_DeviceId",
                table: "DeviceEnrollments",
                columns: new[] { "VisitId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_FloorId",
                table: "Devices",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Ip_Port",
                table: "Devices",
                columns: new[] { "Ip", "Port" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Floors_Name",
                table: "Floors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitFloors_FloorId",
                table: "VisitFloors",
                column: "FloorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceEnrollments");

            migrationBuilder.DropTable(
                name: "VisitFloors");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropIndex(
                name: "IX_Visits_AccessNumber",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "AccessNumber",
                table: "Visits");
        }
    }
}
