using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261003130000_AddSensorHeartbeats")]
public sealed class AddSensorHeartbeats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sensor_nodes",
            columns: table => new
            {
                classroom_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                sensor_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                firmware_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                is_simulated = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_sensor_nodes", x => new { x.classroom_id, x.sensor_id }));

        migrationBuilder.CreateIndex(name: "IX_sensor_nodes_last_heartbeat_at", table: "sensor_nodes", column: "last_heartbeat_at");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "sensor_nodes");
}
