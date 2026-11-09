using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iCareX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleSharingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "vehicle_invitations" (
                    "Id" uuid NOT NULL,
                    "VehicleId" uuid NOT NULL,
                    "InvitedByUserId" uuid NOT NULL,
                    "InvitedEmail" varchar(320) NOT NULL,
                    "NormalizedInvitedEmail" varchar(320) NOT NULL,
                    "TokenHash" varchar(128) NOT NULL,
                    "Role" varchar(16) NOT NULL,
                    "Status" varchar(16) NOT NULL,
                    "ExpiresAtUtc" timestamptz NOT NULL,
                    "AcceptedByUserId" uuid NULL,
                    "AcceptedAtUtc" timestamptz NULL,
                    "CreatedAtUtc" timestamptz NOT NULL,
                    "UpdatedAtUtc" timestamptz NOT NULL,
                    "Version" bigint NOT NULL,
                    CONSTRAINT "PK_vehicle_invitations" PRIMARY KEY ("Id"),
                    CONSTRAINT "ck_vehicle_invitation_role" CHECK ("Role" IN ('Manager', 'Contributor', 'Viewer')),
                    CONSTRAINT "ck_vehicle_invitation_status" CHECK ("Status" IN ('Pending', 'Accepted', 'Rejected', 'Revoked', 'Expired')),
                    CONSTRAINT "ck_vehicle_invitation_acceptance" CHECK (("Status" = 'Accepted' AND "AcceptedByUserId" IS NOT NULL AND "AcceptedAtUtc" IS NOT NULL) OR "Status" <> 'Accepted'),
                    CONSTRAINT "FK_vehicle_invitations_vehicles" FOREIGN KEY ("VehicleId") REFERENCES "vehicles" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_vehicle_invitations_inviter" FOREIGN KEY ("InvitedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_vehicle_invitations_accepter" FOREIGN KEY ("AcceptedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
                );
                CREATE UNIQUE INDEX "IX_vehicle_invitations_TokenHash" ON "vehicle_invitations" ("TokenHash");
                CREATE UNIQUE INDEX "IX_vehicle_invitations_VehicleId_NormalizedInvitedEmail_Pending"
                    ON "vehicle_invitations" ("VehicleId", "NormalizedInvitedEmail") WHERE "Status" = 'Pending';
                CREATE INDEX "IX_vehicle_invitations_VehicleId_Status_ExpiresAtUtc"
                    ON "vehicle_invitations" ("VehicleId", "Status", "ExpiresAtUtc");

                CREATE TABLE "reminders" (
                    "Id" uuid NOT NULL,
                    "VehicleId" uuid NOT NULL,
                    "CreatedByUserId" uuid NOT NULL,
                    "AssignedToUserId" uuid NULL,
                    "Title" varchar(100) NOT NULL,
                    "Description" varchar(1000) NOT NULL,
                    "DueAtUtc" timestamptz NULL,
                    "DueOdo" numeric(20,6) NULL,
                    "Status" varchar(16) NOT NULL,
                    "CompletedAtUtc" timestamptz NULL,
                    "CompletedByUserId" uuid NULL,
                    "CreatedAtUtc" timestamptz NOT NULL,
                    "UpdatedAtUtc" timestamptz NOT NULL,
                    "Version" bigint NOT NULL,
                    CONSTRAINT "PK_reminders" PRIMARY KEY ("Id"),
                    CONSTRAINT "ck_reminder_due_odo" CHECK ("DueOdo" IS NULL OR "DueOdo" BETWEEN 0 AND 9999999),
                    CONSTRAINT "ck_reminder_status" CHECK ("Status" IN ('Pending', 'Completed', 'Cancelled')),
                    CONSTRAINT "FK_reminders_vehicles" FOREIGN KEY ("VehicleId") REFERENCES "vehicles" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_reminders_creator" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_reminders_assignee" FOREIGN KEY ("AssignedToUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_reminders_completer" FOREIGN KEY ("CompletedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
                );
                CREATE INDEX "IX_reminders_VehicleId_Status_DueAtUtc_DueOdo"
                    ON "reminders" ("VehicleId", "Status", "DueAtUtc", "DueOdo");

                CREATE TABLE "notifications" (
                    "Id" uuid NOT NULL,
                    "RecipientUserId" uuid NOT NULL,
                    "VehicleId" uuid NULL,
                    "ReferenceType" varchar(64) NOT NULL,
                    "ReferenceId" uuid NULL,
                    "Snapshot" varchar(4000) NOT NULL,
                    "DeduplicationKey" varchar(256) NOT NULL,
                    "ReadAtUtc" timestamptz NULL,
                    "CreatedAtUtc" timestamptz NOT NULL,
                    "UpdatedAtUtc" timestamptz NOT NULL,
                    "Version" bigint NOT NULL,
                    CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_notifications_recipient" FOREIGN KEY ("RecipientUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_notifications_vehicle" FOREIGN KEY ("VehicleId") REFERENCES "vehicles" ("Id") ON DELETE RESTRICT
                );
                CREATE INDEX "IX_notifications_RecipientUserId_ReadAtUtc_CreatedAtUtc_Id"
                    ON "notifications" ("RecipientUserId", "ReadAtUtc", "CreatedAtUtc", "Id");
                CREATE UNIQUE INDEX "IX_notifications_RecipientUserId_DeduplicationKey"
                    ON "notifications" ("RecipientUserId", "DeduplicationKey");

                CREATE TABLE "audit_logs" (
                    "Id" uuid NOT NULL,
                    "ActorUserId" uuid NOT NULL,
                    "VehicleId" uuid NULL,
                    "Action" varchar(100) NOT NULL,
                    "EntityType" varchar(100) NOT NULL,
                    "EntityId" uuid NOT NULL,
                    "OldValues" jsonb NULL,
                    "NewValues" jsonb NULL,
                    "CreatedAtUtc" timestamptz NOT NULL,
                    CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_audit_logs_actor" FOREIGN KEY ("ActorUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_audit_logs_vehicle" FOREIGN KEY ("VehicleId") REFERENCES "vehicles" ("Id") ON DELETE RESTRICT
                );
                CREATE INDEX "IX_audit_logs_VehicleId_CreatedAtUtc" ON "audit_logs" ("VehicleId", "CreatedAtUtc");
                CREATE INDEX "IX_audit_logs_ActorUserId_CreatedAtUtc" ON "audit_logs" ("ActorUserId", "CreatedAtUtc");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS "audit_logs";
                DROP TABLE IF EXISTS "notifications";
                DROP TABLE IF EXISTS "reminders";
                DROP TABLE IF EXISTS "vehicle_invitations";
                """);
        }
    }
}
