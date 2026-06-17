using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkMonitor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedOSColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "Devices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "Devices");
        }
    }
}
