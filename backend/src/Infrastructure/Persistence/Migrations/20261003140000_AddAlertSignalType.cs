using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261003140000_AddAlertSignalType")]
public sealed class AddAlertSignalType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "signal_type",
            table: "alerts",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "unknown");

        migrationBuilder.Sql(
            """
            UPDATE alerts AS a
            SET signal_type = COALESCE(d.type, 'unknown')
            FROM devices AS d
            WHERE a.device_id = d."Id"
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "signal_type", table: "alerts");
}
