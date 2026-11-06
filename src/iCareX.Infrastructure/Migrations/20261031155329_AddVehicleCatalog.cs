using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iCareX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VehicleModelId",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vehicle_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_brands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_brands_vehicle_types_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "vehicle_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleBrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_models_vehicle_brands_VehicleBrandId",
                        column: x => x.VehicleBrandId,
                        principalTable: "vehicle_brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_VehicleModelId",
                table: "vehicles",
                column: "VehicleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_brands_VehicleTypeId_Name",
                table: "vehicle_brands",
                columns: new[] { "VehicleTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_models_VehicleBrandId_Name",
                table: "vehicle_models",
                columns: new[] { "VehicleBrandId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_types_Code",
                table: "vehicle_types",
                column: "Code",
                unique: true);

            var carType = new Guid("10000000-0000-0000-0000-000000000001");
            var motorcycleType = new Guid("10000000-0000-0000-0000-000000000002");
            var ford = new Guid("20000000-0000-0000-0000-000000000001");
            var hondaCar = new Guid("20000000-0000-0000-0000-000000000002");
            var toyota = new Guid("20000000-0000-0000-0000-000000000003");
            var hondaMotorcycle = new Guid("20000000-0000-0000-0000-000000000004");
            var yamaha = new Guid("20000000-0000-0000-0000-000000000005");
            var fordEcosport = new Guid("30000000-0000-0000-0000-000000000001");
            var fordEverest = new Guid("30000000-0000-0000-0000-000000000002");
            var fordExplorer = new Guid("30000000-0000-0000-0000-000000000003");
            var hondaCivic = new Guid("30000000-0000-0000-0000-000000000004");
            var hondaAccord = new Guid("30000000-0000-0000-0000-000000000005");
            var hondaCrv = new Guid("30000000-0000-0000-0000-000000000006");
            var toyotaVios = new Guid("30000000-0000-0000-0000-000000000007");
            var toyotaCorollaCross = new Guid("30000000-0000-0000-0000-000000000008");
            var toyotaFortuner = new Guid("30000000-0000-0000-0000-000000000009");
            var hondaWave = new Guid("30000000-0000-0000-0000-000000000010");
            var hondaSh = new Guid("30000000-0000-0000-0000-000000000011");
            var yamahaExciter = new Guid("30000000-0000-0000-0000-000000000012");
            var yamahaNvx = new Guid("30000000-0000-0000-0000-000000000013");

            migrationBuilder.Sql($"""
                INSERT INTO vehicle_types ("Id", "Code", "Name", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                VALUES
                    ('{carType}', 'CAR', 'Ô tô', TRUE, NOW(), NOW(), 0),
                    ('{motorcycleType}', 'MOTORCYCLE', 'Xe máy', TRUE, NOW(), NOW(), 0);
                INSERT INTO vehicle_brands ("Id", "VehicleTypeId", "Name", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                VALUES
                    ('{ford}', '{carType}', 'Ford', TRUE, NOW(), NOW(), 0),
                    ('{hondaCar}', '{carType}', 'Honda', TRUE, NOW(), NOW(), 0),
                    ('{toyota}', '{carType}', 'Toyota', TRUE, NOW(), NOW(), 0),
                    ('{hondaMotorcycle}', '{motorcycleType}', 'Honda', TRUE, NOW(), NOW(), 0),
                    ('{yamaha}', '{motorcycleType}', 'Yamaha', TRUE, NOW(), NOW(), 0);
                INSERT INTO vehicle_models ("Id", "VehicleBrandId", "Name", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                VALUES
                    ('{fordEcosport}', '{ford}', 'EcoSport', TRUE, NOW(), NOW(), 0),
                    ('{fordEverest}', '{ford}', 'Everest', TRUE, NOW(), NOW(), 0),
                    ('{fordExplorer}', '{ford}', 'Explorer', TRUE, NOW(), NOW(), 0),
                    ('{hondaCivic}', '{hondaCar}', 'Civic', TRUE, NOW(), NOW(), 0),
                    ('{hondaAccord}', '{hondaCar}', 'Accord', TRUE, NOW(), NOW(), 0),
                    ('{hondaCrv}', '{hondaCar}', 'CR-V', TRUE, NOW(), NOW(), 0),
                    ('{toyotaVios}', '{toyota}', 'Vios', TRUE, NOW(), NOW(), 0),
                    ('{toyotaCorollaCross}', '{toyota}', 'Corolla Cross', TRUE, NOW(), NOW(), 0),
                    ('{toyotaFortuner}', '{toyota}', 'Fortuner', TRUE, NOW(), NOW(), 0),
                    ('{hondaWave}', '{hondaMotorcycle}', 'Wave', TRUE, NOW(), NOW(), 0),
                    ('{hondaSh}', '{hondaMotorcycle}', 'SH', TRUE, NOW(), NOW(), 0),
                    ('{yamahaExciter}', '{yamaha}', 'Exciter', TRUE, NOW(), NOW(), 0),
                    ('{yamahaNvx}', '{yamaha}', 'NVX', TRUE, NOW(), NOW(), 0);
                """);
            migrationBuilder.Sql($"""
                UPDATE vehicles
                SET "VehicleModelId" = CASE "Type"
                    WHEN 'Car' THEN '{toyotaVios}'::uuid
                    WHEN 'Motorcycle' THEN '{hondaWave}'::uuid
                END;
                """);
            migrationBuilder.DropCheckConstraint(name: "ck_vehicle_type", table: "vehicles");
            migrationBuilder.DropColumn(name: "Type", table: "vehicles");
            migrationBuilder.AlterColumn<Guid>(
                name: "VehicleModelId",
                table: "vehicles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_vehicle_models_VehicleModelId",
                table: "vehicles",
                column: "VehicleModelId",
                principalTable: "vehicle_models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vehicles_vehicle_models_VehicleModelId",
                table: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_models");

            migrationBuilder.DropTable(
                name: "vehicle_brands");

            migrationBuilder.DropTable(
                name: "vehicle_types");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_VehicleModelId",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleModelId",
                table: "vehicles");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "vehicles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicle_type",
                table: "vehicles",
                sql: "\"Type\" IN ('Car', 'Motorcycle')");
        }
    }
}
