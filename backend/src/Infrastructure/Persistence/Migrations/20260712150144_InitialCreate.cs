using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    risk_score = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "device_observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    signal_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rssi = table.Column<double>(type: "double precision", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_observations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "exam_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    room_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "whitelist_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    student_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    device_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whitelist_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_session_id_created_at",
                table: "alerts",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_device_observations_captured_at",
                table: "device_observations",
                column: "captured_at");

            migrationBuilder.CreateIndex(
                name: "IX_device_observations_session_id_captured_at",
                table: "device_observations",
                columns: new[] { "session_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "IX_devices_hash_id",
                table: "devices",
                column: "hash_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_last_seen_at",
                table: "devices",
                column: "last_seen_at");

            migrationBuilder.CreateIndex(
                name: "IX_whitelist_entries_session_id_device_hash",
                table: "whitelist_entries",
                columns: new[] { "session_id", "device_hash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "device_observations");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "exam_sessions");

            migrationBuilder.DropTable(
                name: "whitelist_entries");
        }
    }
}
