using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iCareX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixVehicleOwnerConstraintTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION ensure_exactly_one_active_vehicle_owner()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE
                    vehicle_ids uuid[];
                    vehicle_id uuid;
                BEGIN
                    -- The same function is attached to two different row types.
                    -- Do not access VehicleId on vehicles, or Id on vehicle_members.
                    IF TG_TABLE_NAME = 'vehicles' THEN
                        IF TG_OP = 'INSERT' THEN
                            vehicle_ids := ARRAY[NEW."Id"];
                        ELSE
                            vehicle_ids := ARRAY[OLD."Id", NEW."Id"];
                        END IF;
                    ELSE
                        IF TG_OP = 'DELETE' THEN
                            vehicle_ids := ARRAY[OLD."VehicleId"];
                        ELSIF TG_OP = 'INSERT' THEN
                            vehicle_ids := ARRAY[NEW."VehicleId"];
                        ELSE
                            -- A moved membership must leave both vehicles valid.
                            vehicle_ids := ARRAY[OLD."VehicleId", NEW."VehicleId"];
                        END IF;
                    END IF;

                    FOREACH vehicle_id IN ARRAY vehicle_ids LOOP
                        IF EXISTS (SELECT 1 FROM "vehicles" WHERE "Id" = vehicle_id)
                           AND (SELECT count(*) FROM "vehicle_members"
                                WHERE "VehicleId" = vehicle_id
                                  AND "RevokedAtUtc" IS NULL AND "Role" = 'Owner') <> 1
                        THEN
                            RAISE EXCEPTION 'vehicle must have exactly one active Owner'
                                USING ERRCODE = '23514';
                        END IF;
                    END LOOP;
                    RETURN NULL; -- Return values of AFTER triggers are ignored.
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
                """);
        }
    }
}
