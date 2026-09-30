using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260922152000_UpdateA015SensorPlacement")]
public sealed class UpdateA015SensorPlacement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => UpdateSensors(
        migrationBuilder,
        s2X: 2.94,
        s2Y: 8.3807,
        s3X: 5.88,
        s3Y: 0);

    protected override void Down(MigrationBuilder migrationBuilder) => UpdateSensors(
        migrationBuilder,
        s2X: 5.88,
        s2Y: 0,
        s3X: 5.88,
        s3Y: 8.3807);

    private static void UpdateSensors(
        MigrationBuilder migrationBuilder,
        double s2X,
        double s2Y,
        double s3X,
        double s3Y)
    {
        var s2XSql = s2X.ToString(CultureInfo.InvariantCulture);
        var s2YSql = s2Y.ToString(CultureInfo.InvariantCulture);
        var s3XSql = s3X.ToString(CultureInfo.InvariantCulture);
        var s3YSql = s3Y.ToString(CultureInfo.InvariantCulture);
        migrationBuilder.Sql($$"""
            UPDATE classrooms
            SET "LayoutJson" = jsonb_set(
                    jsonb_set(
                        "LayoutJson"::jsonb,
                        '{Sensors}',
                        (
                            SELECT jsonb_agg(
                                CASE sensor->>'Id'
                                    WHEN 'S2' THEN sensor || jsonb_build_object('X', {{s2XSql}}, 'Y', {{s2YSql}})
                                    WHEN 'S3' THEN sensor || jsonb_build_object('X', {{s3XSql}}, 'Y', {{s3YSql}})
                                    ELSE sensor
                                END
                                ORDER BY ordinal
                            )
                            FROM jsonb_array_elements("LayoutJson"::jsonb->'Sensors') WITH ORDINALITY AS sensors(sensor, ordinal)
                        )
                    ),
                    '{Revision}',
                    to_jsonb("Revision" + 1)
                )::text,
                "Revision" = "Revision" + 1
            WHERE "Id" = 'A-0-15'
              AND "LayoutJson"::jsonb ? 'Sensors';
            """);
    }
}
