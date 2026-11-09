using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iCareX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignVehicleRegistrationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF current_setting('server_encoding') <> 'UTF8' THEN
                        RAISE EXCEPTION 'Migration stopped: Unicode NFC normalization requires UTF8 server encoding.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM vehicles WHERE "BaseOdo" <> round("BaseOdo", 2)) THEN
                        RAISE EXCEPTION 'Migration stopped: BaseOdo has precision beyond two decimals; reconcile explicitly before migration.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM vehicle_types WHERE regexp_replace(upper("Code"), '[^A-Z0-9]', '', 'g')
                        NOT IN ('CAR','MOTORCYCLE','MOTORBIKE','BICYCLE','TRUCK','BUS','VAN','BOAT','AIRCRAFT','OTHER')) THEN
                        RAISE EXCEPTION 'Migration stopped: unmapped legacy vehicle type. Map its code explicitly; no automatic Other fallback.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_vehicle_brands_vehicle_types_VehicleTypeId",
                table: "vehicle_brands");

            migrationBuilder.DropForeignKey(
                name: "FK_vehicles_vehicle_models_VehicleModelId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_AccountId_PlateNormalized",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_VehicleModelId",
                table: "vehicles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vehicle_odo",
                table: "vehicles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vehicle_year",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_vehicle_models_VehicleBrandId_Name",
                table: "vehicle_models");

            migrationBuilder.DropIndex(
                name: "IX_vehicle_brands_VehicleTypeId_Name",
                table: "vehicle_brands");

            migrationBuilder.DropCheckConstraint(
                name: "ck_odometer_reading_odo",
                table: "odometer_readings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_maintenance_record_numbers",
                table: "maintenance_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_plan_intervals",
                table: "maintenance_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fuel_entry_numbers",
                table: "fuel_entries");

            migrationBuilder.RenameColumn(name: "BaseOdo", table: "vehicles", newName: "Odometer");

            migrationBuilder.AlterColumn<int>(
                name: "Year",
                table: "vehicles",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "PlateNormalized",
                table: "vehicles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Plate",
                table: "vehicles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "vehicles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "vehicles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelCapacity",
                table: "vehicles",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "vehicles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Odometer", table: "vehicles", type: "numeric(12,2)", precision: 12, scale: 2,
                nullable: false, defaultValue: 0m,
                oldClrType: typeof(decimal), oldType: "numeric(20,6)", oldPrecision: 20, oldScale: 6);

            migrationBuilder.AddColumn<string>(
                name: "OdometerUnit",
                table: "vehicles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "vehicles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationId",
                table: "vehicles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "vehicles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VIN",
                table: "vehicles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleBrandId",
                table: "vehicles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "vehicle_models",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "vehicle_models",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "vehicle_models",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "vehicle_brands",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "vehicle_brands",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "vehicle_brands",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "vehicle_brands",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE vehicle_brands b SET
                    "VehicleType" = CASE regexp_replace(upper(t."Code"), '[^A-Z0-9]', '', 'g')
                        WHEN 'CAR' THEN 'Car' WHEN 'MOTORCYCLE' THEN 'Motorcycle' WHEN 'MOTORBIKE' THEN 'Motorcycle'
                        WHEN 'BICYCLE' THEN 'Bicycle' WHEN 'TRUCK' THEN 'Truck' WHEN 'BUS' THEN 'Bus'
                        WHEN 'VAN' THEN 'Van' WHEN 'BOAT' THEN 'Boat' WHEN 'AIRCRAFT' THEN 'Aircraft' WHEN 'OTHER' THEN 'Other' END,
                    "IsActive" = b."IsActive" AND t."IsActive",
                    "NormalizedName" = upper(regexp_replace(btrim(normalize(b."Name", NFC)), '\s+', ' ', 'g'))
                FROM vehicle_types t WHERE b."VehicleTypeId" = t."Id";
                UPDATE vehicle_models SET "NormalizedName" = upper(regexp_replace(btrim(normalize("Name", NFC)), '\s+', ' ', 'g'));
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM vehicle_brands GROUP BY "VehicleType", "NormalizedName" HAVING count(*) > 1)
                       OR EXISTS (SELECT 1 FROM vehicle_models GROUP BY "VehicleBrandId", "NormalizedName" HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'Migration stopped: duplicate catalog names after normalization; reconcile without deleting referenced rows.';
                    END IF;
                END $$;
                UPDATE vehicles v SET "VehicleBrandId" = m."VehicleBrandId", "VehicleType" = b."VehicleType",
                    "FuelType" = 'Other', "OdometerUnit" = 'Kilometres',
                    "Name" = nullif(btrim(v."Name"), ''), "ImageUrl" = nullif(btrim(v."ImageUrl"), ''),
                    "Plate" = nullif(btrim(v."Plate"), ''), "PlateNormalized" = nullif(v."PlateNormalized", '')
                FROM vehicle_models m JOIN vehicle_brands b ON b."Id" = m."VehicleBrandId"
                WHERE v."VehicleModelId" = m."Id";
                -- Flush deferred Owner checks before further ALTER TABLE operations.
                SET CONSTRAINTS ALL IMMEDIATE;
                SET CONSTRAINTS ALL DEFERRED;
                ALTER TABLE vehicles ALTER COLUMN "VehicleBrandId" DROP DEFAULT, ALTER COLUMN "VehicleType" DROP DEFAULT,
                    ALTER COLUMN "FuelType" DROP DEFAULT, ALTER COLUMN "OdometerUnit" DROP DEFAULT;
                ALTER TABLE vehicle_brands ALTER COLUMN "NormalizedName" DROP DEFAULT, ALTER COLUMN "VehicleType" DROP DEFAULT;
                ALTER TABLE vehicle_models ALTER COLUMN "NormalizedName" DROP DEFAULT;
                """);
            migrationBuilder.DropColumn(name: "VehicleTypeId", table: "vehicle_brands");
            migrationBuilder.DropTable(name: "vehicle_types");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_vehicle_models_Id_VehicleBrandId",
                table: "vehicle_models",
                columns: new[] { "Id", "VehicleBrandId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_vehicle_brands_Id_VehicleType",
                table: "vehicle_brands",
                columns: new[] { "Id", "VehicleType" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_AccountId_PlateNormalized",
                table: "vehicles",
                columns: new[] { "AccountId", "PlateNormalized" },
                unique: true,
                filter: "\"PlateNormalized\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_ArchivedAtUtc",
                table: "vehicles",
                column: "ArchivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_PlateNormalized",
                table: "vehicles",
                column: "PlateNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_VehicleBrandId_VehicleModelId",
                table: "vehicles",
                columns: new[] { "VehicleBrandId", "VehicleModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_VehicleBrandId_VehicleType",
                table: "vehicles",
                columns: new[] { "VehicleBrandId", "VehicleType" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_VehicleModelId_VehicleBrandId",
                table: "vehicles",
                columns: new[] { "VehicleModelId", "VehicleBrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_VIN",
                table: "vehicles",
                column: "VIN");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_fuel_type",
                table: "vehicles",
                sql: "\"FuelType\" IN ('Gasoline','Diesel','Electric','Hybrid','PlugInHybrid','Ethanol','Biodiesel','LPG','CNG','Hydrogen','Other')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_odo",
                table: "vehicles",
                sql: "\"Odometer\" BETWEEN 0 AND 9999999999.99");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_odometer_unit",
                table: "vehicles",
                sql: "\"OdometerUnit\" IN ('Kilometres','Miles','Hours')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_optional_numbers",
                table: "vehicles",
                sql: "(\"FuelCapacity\" IS NULL OR \"FuelCapacity\" > 0) AND (\"PurchasePrice\" IS NULL OR \"PurchasePrice\" >= 0) AND (\"SalePrice\" IS NULL OR \"SalePrice\" >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_plate_pair",
                table: "vehicles",
                sql: "(\"Plate\" IS NULL AND \"PlateNormalized\" IS NULL) OR (\"Plate\" IS NOT NULL AND \"PlateNormalized\" IS NOT NULL AND length(\"PlateNormalized\") > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_type",
                table: "vehicles",
                sql: "\"VehicleType\" IN ('Car','Motorcycle','Bicycle','Truck','Bus','Van','Boat','Aircraft','Other')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_year",
                table: "vehicles",
                sql: "\"Year\" IS NULL OR \"Year\" BETWEEN 1 AND 9999");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_models_VehicleBrandId_NormalizedName",
                table: "vehicle_models",
                columns: new[] { "VehicleBrandId", "NormalizedName" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_model_name",
                table: "vehicle_models",
                sql: "length(btrim(\"Name\")) > 0 AND length(\"NormalizedName\") > 0");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_brands_VehicleType_NormalizedName",
                table: "vehicle_brands",
                columns: new[] { "VehicleType", "NormalizedName" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_brand_name",
                table: "vehicle_brands",
                sql: "length(btrim(\"Name\")) > 0 AND length(\"NormalizedName\") > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_brand_type",
                table: "vehicle_brands",
                sql: "\"VehicleType\" IN ('Car','Motorcycle','Bicycle','Truck','Bus','Van','Boat','Aircraft','Other')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_odometer_reading_odo",
                table: "odometer_readings",
                sql: "\"Odo\" BETWEEN 0 AND 9999999999.99");

            migrationBuilder.AddCheckConstraint(
                name: "ck_maintenance_record_numbers",
                table: "maintenance_records",
                sql: "\"Odo\" BETWEEN 0 AND 9999999999.99 AND \"Cost\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_plan_intervals",
                table: "maintenance_plans",
                sql: "\"DueOdo\" BETWEEN 1 AND 9999999999.99 AND \"IntervalKm\" BETWEEN 1 AND 1000000 AND \"IntervalMonths\" BETWEEN 1 AND 120");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fuel_entry_numbers",
                table: "fuel_entries",
                sql: "\"Odo\" BETWEEN 0 AND 9999999999.99 AND \"Cost\" > 0 AND \"Liters\" > 0 AND \"Liters\" <= 500");

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_vehicle_brands_VehicleBrandId_VehicleType",
                table: "vehicles",
                columns: new[] { "VehicleBrandId", "VehicleType" },
                principalTable: "vehicle_brands",
                principalColumns: new[] { "Id", "VehicleType" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_vehicle_models_VehicleModelId_VehicleBrandId",
                table: "vehicles",
                columns: new[] { "VehicleModelId", "VehicleBrandId" },
                principalTable: "vehicle_models",
                principalColumns: new[] { "Id", "VehicleBrandId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // New optional data and legacy catalog identities cannot be safely reconstructed.
            throw new NotSupportedException("Forward-only data-preserving migration. Restore a verified backup or create a reviewed forward migration; do not silently discard vehicle details.");
        }
    }
}
