using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NetworkMonitor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHostedWorkloadsAndServiceUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostedWorkloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("HostedWorkloads_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostedWorkloads_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    UnitName = table.Column<string>(type: "text", nullable: false),
                    ActiveState = table.Column<string>(type: "text", nullable: false),
                    SubState = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ServiceUnits_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceUnits_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostedWorkloads_DeviceId",
                table: "HostedWorkloads",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_HostedWorkloads_DeviceId_ExternalId_Source",
                table: "HostedWorkloads",
                columns: new[] { "DeviceId", "ExternalId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUnits_DeviceId",
                table: "ServiceUnits",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUnits_DeviceId_UnitName_Source",
                table: "ServiceUnits",
                columns: new[] { "DeviceId", "UnitName", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostedWorkloads");

            migrationBuilder.DropTable(
                name: "ServiceUnits");
        }
    }
}
