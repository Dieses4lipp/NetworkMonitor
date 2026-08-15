using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkMonitor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTypeAndVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFingerprintedAt",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlatformType",
                table: "Devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Vendor",
                table: "Devices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFingerprintedAt",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PlatformType",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "Devices");
        }
    }
}
