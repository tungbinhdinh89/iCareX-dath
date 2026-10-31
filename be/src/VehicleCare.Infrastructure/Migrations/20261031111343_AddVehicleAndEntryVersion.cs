using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAndEntryVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "vehicles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "user_settings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "maintenance_plans",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "entries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "maintenance_plans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "accounts");
        }
    }
}
