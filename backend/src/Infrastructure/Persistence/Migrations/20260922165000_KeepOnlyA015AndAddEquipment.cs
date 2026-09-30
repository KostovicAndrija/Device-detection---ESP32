using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260922165000_KeepOnlyA015AndAddEquipment")]
public sealed class KeepOnlyA015AndAddEquipment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH inserted AS (
                INSERT INTO classrooms ("Id", "LayoutJson", "Revision")
                VALUES (
                    'A-0-15',
                    $layout${"Id":"A-0-15","Name":"Učionica A-0-15","Description":"Raspored učionice A-0-15 sa katedrom, tablom i računarima.","Width":5.88,"Height":8.3807,"Computers":[{"Id":"K-01","X":1.07283,"Y":2.4087,"Width":2,"Height":0.6,"Kind":"desk","ComputerCount":2},{"Id":"K-02","X":4.29783,"Y":2.4087,"Width":2.7,"Height":0.6,"Kind":"desk","ComputerCount":3},{"Id":"K-03","X":1.07283,"Y":3.6087,"Width":2,"Height":0.6,"Kind":"desk","ComputerCount":2},{"Id":"K-04","X":4.29783,"Y":3.6087,"Width":2.7,"Height":0.6,"Kind":"desk","ComputerCount":3},{"Id":"K-05","X":1.07283,"Y":4.8087,"Width":2,"Height":0.6,"Kind":"desk","ComputerCount":2},{"Id":"K-06","X":4.29783,"Y":4.8087,"Width":2.7,"Height":0.6,"Kind":"desk","ComputerCount":3},{"Id":"K-07","X":1.07283,"Y":6.0087,"Width":2,"Height":0.6,"Kind":"desk","ComputerCount":2},{"Id":"K-08","X":4.29783,"Y":6.0087,"Width":2.7,"Height":0.6,"Kind":"desk","ComputerCount":3},{"Id":"K-09","X":1.07283,"Y":7.2087,"Width":2,"Height":0.6,"Kind":"desk","ComputerCount":2},{"Id":"K-10","X":4.29783,"Y":7.2087,"Width":2.7,"Height":0.6,"Kind":"desk","ComputerCount":3},{"Id":"K-11","X":4.59783,"Y":1.8087,"Width":2,"Height":0.6,"Kind":"desk","ComputerCount":1,"IsTeacherDesk":true}],"Sensors":[{"Id":"S1","X":0,"Y":0,"Model":"ESP32","ReferenceRssi":-45,"PathLossExponent":2.7},{"Id":"S2","X":2.94,"Y":8.3807,"Model":"ESP32","ReferenceRssi":-45,"PathLossExponent":2.7},{"Id":"S3","X":5.88,"Y":0,"Model":"ESP32","ReferenceRssi":-45,"PathLossExponent":2.7}],"Revision":1,"Boards":[{"Id":"TABLA-1","X":4.41,"Y":0.06,"Width":2.94,"Height":0.12}]}$layout$,
                    1
                )
                ON CONFLICT ("Id") DO NOTHING
                RETURNING "Id"
            )
            UPDATE classrooms
            SET "LayoutJson" = jsonb_set(
                    jsonb_set(
                        "LayoutJson"::jsonb,
                        '{Computers}',
                        (
                            SELECT jsonb_agg(
                                CASE
                                    WHEN desk->>'Id' IN ('K-01','K-03','K-05','K-07','K-09')
                                        THEN desk || jsonb_build_object('ComputerCount', 2, 'IsTeacherDesk', false)
                                    WHEN desk->>'Id' IN ('K-02','K-04','K-06','K-08','K-10')
                                        THEN desk || jsonb_build_object('ComputerCount', 3, 'IsTeacherDesk', false)
                                    WHEN desk->>'Id' = 'K-11'
                                        THEN desk || jsonb_build_object('ComputerCount', 1, 'IsTeacherDesk', true)
                                    ELSE desk
                                END
                                ORDER BY ordinal
                            )
                            FROM jsonb_array_elements("LayoutJson"::jsonb->'Computers') WITH ORDINALITY AS desks(desk, ordinal)
                        )
                    ),
                    '{Boards}',
                    '[{"Id":"TABLA-1","X":4.41,"Y":0.06,"Width":2.94,"Height":0.12}]'::jsonb,
                    true
                )::text,
                "Revision" = "Revision" + 1
            WHERE "Id" = 'A-0-15'
              AND NOT EXISTS (SELECT 1 FROM inserted);

            UPDATE classrooms
            SET "LayoutJson" = jsonb_set("LayoutJson"::jsonb, '{Revision}', to_jsonb("Revision"))::text
            WHERE "Id" = 'A-0-15';

            DELETE FROM classrooms WHERE "Id" <> 'A-0-15';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Brisanje ostalih učionica je namerna i nepovratna migracija podataka.");
}
