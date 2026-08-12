using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkMonitor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixConstraintNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Devices",
                table: "MonitoringJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Metrics_Jobs",
                table: "RawMetrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NetworkScans",
                table: "NetworkScans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceHistories",
                table: "DeviceHistories");

            migrationBuilder.RenameIndex(
                name: "IX_Devices_AgentId1",
                table: "RawMetrics",
                newName: "IX_RawMetrics_JobId");

            migrationBuilder.AddPrimaryKey(
                name: "NetworkScans_pkey",
                table: "NetworkScans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "DeviceHistories_pkey",
                table: "DeviceHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MonitoringJobs_Devices",
                table: "MonitoringJobs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RawMetrics_MonitoringJobs",
                table: "RawMetrics",
                column: "JobId",
                principalTable: "MonitoringJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonitoringJobs_Devices",
                table: "MonitoringJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_RawMetrics_MonitoringJobs",
                table: "RawMetrics");

            migrationBuilder.DropPrimaryKey(
                name: "NetworkScans_pkey",
                table: "NetworkScans");

            migrationBuilder.DropPrimaryKey(
                name: "DeviceHistories_pkey",
                table: "DeviceHistories");

            migrationBuilder.RenameIndex(
                name: "IX_RawMetrics_JobId",
                table: "RawMetrics",
                newName: "IX_Devices_AgentId1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NetworkScans",
                table: "NetworkScans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceHistories",
                table: "DeviceHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Devices",
                table: "MonitoringJobs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Metrics_Jobs",
                table: "RawMetrics",
                column: "JobId",
                principalTable: "MonitoringJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
