using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NetworkMonitor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Devices.Hostname predates this migration as untracked model drift; the column may
            // already exist in a deployed database, so converge instead of failing.
            migrationBuilder.Sql("ALTER TABLE \"Devices\" ADD COLUMN IF NOT EXISTS \"Hostname\" text;");

            migrationBuilder.CreateTable(
                name: "NetworkServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Banner = table.Column<string>(type: "text", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("NetworkServices_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkServices_Devices",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetworkServices_DeviceId",
                table: "NetworkServices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkServices_DeviceId_Port",
                table: "NetworkServices",
                columns: new[] { "DeviceId", "Port" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetworkServices");

            migrationBuilder.Sql("ALTER TABLE \"Devices\" DROP COLUMN IF EXISTS \"Hostname\";");
        }
    }
}
