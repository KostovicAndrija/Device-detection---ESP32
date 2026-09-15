using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantDeviceRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "staff_device_id",
                table: "whitelist_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "staff_device_revision",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "exam_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "registration_expires_at",
                table: "exam_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "staff_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_devices_exam_sessions_RegistrationSessionId",
                        column: x => x.RegistrationSessionId,
                        principalTable: "exam_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_devices_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_whitelist_entries_staff_device_id",
                table: "whitelist_entries",
                column: "staff_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_sessions_owner_user_id",
                table: "exam_sessions",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_devices_DeviceHash",
                table: "staff_devices",
                column: "DeviceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_devices_RegistrationSessionId",
                table: "staff_devices",
                column: "RegistrationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_devices_UserId",
                table: "staff_devices",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_sessions_users_owner_user_id",
                table: "exam_sessions",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_whitelist_entries_staff_devices_staff_device_id",
                table: "whitelist_entries",
                column: "staff_device_id",
                principalTable: "staff_devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_sessions_users_owner_user_id",
                table: "exam_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_whitelist_entries_staff_devices_staff_device_id",
                table: "whitelist_entries");

            migrationBuilder.DropTable(
                name: "staff_devices");

            migrationBuilder.DropIndex(
                name: "IX_whitelist_entries_staff_device_id",
                table: "whitelist_entries");

            migrationBuilder.DropIndex(
                name: "IX_exam_sessions_owner_user_id",
                table: "exam_sessions");

            migrationBuilder.DropColumn(
                name: "staff_device_id",
                table: "whitelist_entries");

            migrationBuilder.DropColumn(
                name: "staff_device_revision",
                table: "users");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "exam_sessions");

            migrationBuilder.DropColumn(
                name: "registration_expires_at",
                table: "exam_sessions");
        }
    }
}
