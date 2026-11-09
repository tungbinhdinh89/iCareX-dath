using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iCareX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewTypedVehicleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "vehicles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "fuel_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Odo = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Liters = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_entries", x => x.Id);
                    table.CheckConstraint("ck_fuel_entry_numbers", "\"Odo\" BETWEEN 0 AND 9999999 AND \"Cost\" > 0 AND \"Liters\" > 0 AND \"Liters\" <= 500");
                    table.ForeignKey(
                        name: "FK_fuel_entries_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fuel_entries_AspNetUsers_DriverUserId",
                        column: x => x.DriverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fuel_entries_vehicles_AccountId_VehicleId",
                        columns: x => new { x.AccountId, x.VehicleId },
                        principalTable: "vehicles",
                        principalColumns: new[] { "AccountId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Odo = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_records", x => x.Id);
                    table.CheckConstraint("ck_maintenance_record_numbers", "\"Odo\" BETWEEN 0 AND 9999999 AND \"Cost\" > 0");
                    table.ForeignKey(
                        name: "FK_maintenance_records_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_records_AspNetUsers_DriverUserId",
                        column: x => x.DriverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_records_maintenance_plans_AccountId_VehicleId_P~",
                        columns: x => new { x.AccountId, x.VehicleId, x.PlanId },
                        principalTable: "maintenance_plans",
                        principalColumns: new[] { "AccountId", "VehicleId", "Id" });
                    table.ForeignKey(
                        name: "FK_maintenance_records_vehicles_AccountId_VehicleId",
                        columns: x => new { x.AccountId, x.VehicleId },
                        principalTable: "vehicles",
                        principalColumns: new[] { "AccountId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "odometer_readings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Odo = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_odometer_readings", x => x.Id);
                    table.CheckConstraint("ck_odometer_reading_odo", "\"Odo\" BETWEEN 0 AND 9999999");
                    table.ForeignKey(
                        name: "FK_odometer_readings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odometer_readings_AspNetUsers_DriverUserId",
                        column: x => x.DriverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_odometer_readings_vehicles_AccountId_VehicleId",
                        columns: x => new { x.AccountId, x.VehicleId },
                        principalTable: "vehicles",
                        principalColumns: new[] { "AccountId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_members", x => x.Id);
                    table.CheckConstraint("ck_vehicle_member_role", "\"Role\" IN ('Owner', 'Manager', 'Contributor', 'Viewer')");
                    table.ForeignKey(
                        name: "FK_vehicle_members_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_members_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_CreatedByUserId",
                table: "vehicles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_entries_AccountId_VehicleId_Date_Odo",
                table: "fuel_entries",
                columns: new[] { "AccountId", "VehicleId", "Date", "Odo" });

            migrationBuilder.CreateIndex(
                name: "IX_fuel_entries_CreatedByUserId",
                table: "fuel_entries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_entries_DriverUserId",
                table: "fuel_entries",
                column: "DriverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_records_AccountId_VehicleId_Date_Odo",
                table: "maintenance_records",
                columns: new[] { "AccountId", "VehicleId", "Date", "Odo" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_records_AccountId_VehicleId_PlanId",
                table: "maintenance_records",
                columns: new[] { "AccountId", "VehicleId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_records_CreatedByUserId",
                table: "maintenance_records",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_records_DriverUserId",
                table: "maintenance_records",
                column: "DriverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_odometer_readings_AccountId_VehicleId_Date_Odo",
                table: "odometer_readings",
                columns: new[] { "AccountId", "VehicleId", "Date", "Odo" });

            migrationBuilder.CreateIndex(
                name: "IX_odometer_readings_CreatedByUserId",
                table: "odometer_readings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_odometer_readings_DriverUserId",
                table: "odometer_readings",
                column: "DriverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_members_UserId",
                table: "vehicle_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_members_VehicleId",
                table: "vehicle_members",
                column: "VehicleId",
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL AND \"Role\" = 'Owner'");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_members_VehicleId_UserId",
                table: "vehicle_members",
                columns: new[] { "VehicleId", "UserId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");

            // Preserve every legacy row before the source table is removed.
            migrationBuilder.Sql("""
                UPDATE "vehicles" SET "CreatedByUserId" = "AccountId";
                INSERT INTO "vehicle_members" ("Id", "VehicleId", "UserId", "Role", "JoinedAtUtc", "RevokedAtUtc", "CreatedAtUtc", "Version")
                SELECT gen_random_uuid(), v."Id", v."AccountId", 'Owner', v."CreatedAtUtc", NULL, v."CreatedAtUtc", 0
                FROM "vehicles" v;
                INSERT INTO "odometer_readings"
                    ("Id", "AccountId", "VehicleId", "CreatedByUserId", "DriverUserId", "Date", "Odo", "Note", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                SELECT "Id", "AccountId", "VehicleId", "AccountId", NULL, "Date", "Odo", "Note", "CreatedAtUtc", "UpdatedAtUtc", "Version"
                FROM "entries" WHERE "Kind" = 'Odo';
                INSERT INTO "fuel_entries"
                    ("Id", "AccountId", "VehicleId", "CreatedByUserId", "DriverUserId", "Date", "Odo", "Cost", "Liters", "Note", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                SELECT "Id", "AccountId", "VehicleId", "AccountId", NULL, "Date", "Odo", "Cost", "Liters", "Note", "CreatedAtUtc", "UpdatedAtUtc", "Version"
                FROM "entries" WHERE "Kind" = 'Fuel';
                INSERT INTO "maintenance_records"
                    ("Id", "AccountId", "VehicleId", "CreatedByUserId", "DriverUserId", "PlanId", "Title", "Date", "Odo", "Cost", "Note", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                SELECT "Id", "AccountId", "VehicleId", "AccountId", NULL, "PlanId", "Title", "Date", "Odo", "Cost", "Note", "CreatedAtUtc", "UpdatedAtUtc", "Version"
                FROM "entries" WHERE "Kind" = 'Service';
                DROP TABLE "entries";

                CREATE OR REPLACE FUNCTION ensure_exactly_one_active_vehicle_owner()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE vehicle_id uuid;
                BEGIN
                    vehicle_id := COALESCE(NEW."VehicleId", OLD."VehicleId");
                    IF EXISTS (SELECT 1 FROM "vehicles" WHERE "Id" = vehicle_id)
                       AND (SELECT count(*) FROM "vehicle_members"
                            WHERE "VehicleId" = vehicle_id
                              AND "RevokedAtUtc" IS NULL AND "Role" = 'Owner') <> 1
                    THEN
                        RAISE EXCEPTION 'vehicle must have exactly one active Owner';
                    END IF;
                    RETURN NEW;
                END;
                $$;
                CREATE CONSTRAINT TRIGGER vehicle_members_exactly_one_owner
                AFTER INSERT OR UPDATE OR DELETE ON "vehicle_members"
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION ensure_exactly_one_active_vehicle_owner();
                CREATE CONSTRAINT TRIGGER vehicles_exactly_one_owner
                AFTER INSERT OR UPDATE ON "vehicles"
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION ensure_exactly_one_active_vehicle_owner();
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_AspNetUsers_CreatedByUserId",
                table: "vehicles",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS vehicle_members_exactly_one_owner ON "vehicle_members";
                DROP TRIGGER IF EXISTS vehicles_exactly_one_owner ON "vehicles";
                DROP FUNCTION IF EXISTS ensure_exactly_one_active_vehicle_owner();
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_vehicles_AspNetUsers_CreatedByUserId",
                table: "vehicles");

            migrationBuilder.DropTable(
                name: "fuel_entries");

            migrationBuilder.DropTable(
                name: "maintenance_records");

            migrationBuilder.DropTable(
                name: "odometer_readings");

            migrationBuilder.DropTable(
                name: "vehicle_members");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_CreatedByUserId",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "vehicles");

            migrationBuilder.CreateTable(
                name: "entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Liters = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Odo = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entries", x => x.Id);
                    table.CheckConstraint("ck_entry_kind", "(\"Kind\" = 'Fuel' AND \"Cost\" > 0 AND \"Liters\" > 0 AND \"PlanId\" IS NULL) OR (\"Kind\" = 'Service' AND \"Cost\" > 0 AND \"Liters\" = 0) OR (\"Kind\" = 'Odo' AND \"Cost\" = 0 AND \"Liters\" = 0 AND \"PlanId\" IS NULL)");
                    table.CheckConstraint("ck_entry_numbers", "\"Odo\" BETWEEN 0 AND 9999999 AND \"Cost\" BETWEEN 0 AND 10000000000 AND \"Liters\" BETWEEN 0 AND 500");
                    table.ForeignKey(
                        name: "FK_entries_maintenance_plans_AccountId_VehicleId_PlanId",
                        columns: x => new { x.AccountId, x.VehicleId, x.PlanId },
                        principalTable: "maintenance_plans",
                        principalColumns: new[] { "AccountId", "VehicleId", "Id" });
                    table.ForeignKey(
                        name: "FK_entries_vehicles_AccountId_VehicleId",
                        columns: x => new { x.AccountId, x.VehicleId },
                        principalTable: "vehicles",
                        principalColumns: new[] { "AccountId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entries_AccountId_PlanId_Date_Odo",
                table: "entries",
                columns: new[] { "AccountId", "PlanId", "Date", "Odo" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_AccountId_VehicleId_Date_Odo",
                table: "entries",
                columns: new[] { "AccountId", "VehicleId", "Date", "Odo" });

            migrationBuilder.CreateIndex(
                name: "IX_entries_AccountId_VehicleId_PlanId",
                table: "entries",
                columns: new[] { "AccountId", "VehicleId", "PlanId" });
        }
    }
}
