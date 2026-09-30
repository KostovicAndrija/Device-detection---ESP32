using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260923123000_SyncActiveRoomLayouts")]
public sealed class SyncActiveRoomLayouts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE exam_sessions AS session
            SET room_snapshot_json = classroom."LayoutJson"
            FROM classrooms AS classroom
            WHERE session.room_id = classroom."Id"
              AND session.room_id = 'A-0-15'
              AND session.status = 'active';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Prethodne kopije aktivnih rasporeda nisu dostupne za pouzdano vraćanje.
    }
}
