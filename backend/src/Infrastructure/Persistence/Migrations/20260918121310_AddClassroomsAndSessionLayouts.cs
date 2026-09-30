using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomsAndSessionLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "room_snapshot_json",
                table: "exam_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "classrooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LayoutJson = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classrooms", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "classrooms",
                columns: new[] { "Id", "LayoutJson", "Revision" },
                values: new object[,]
                {
                    { "LAB-A", "{\"Id\":\"LAB-A\",\"Name\":\"Laboratorija A\",\"Description\":\"Postoje\\u0107i raspored u\\u010Dionice\",\"Width\":8,\"Height\":6,\"Computers\":[{\"Id\":\"1\",\"X\":1.92,\"Y\":2.04},{\"Id\":\"2\",\"X\":4,\"Y\":2.04},{\"Id\":\"3\",\"X\":6.08,\"Y\":2.04},{\"Id\":\"4\",\"X\":1.92,\"Y\":3.3600000000000003},{\"Id\":\"5\",\"X\":4,\"Y\":3.3600000000000003},{\"Id\":\"6\",\"X\":6.08,\"Y\":3.3600000000000003},{\"Id\":\"7\",\"X\":1.92,\"Y\":4.68},{\"Id\":\"8\",\"X\":4,\"Y\":4.68},{\"Id\":\"9\",\"X\":6.08,\"Y\":4.68}],\"Sensors\":[{\"Id\":\"S1\",\"X\":0,\"Y\":0,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7},{\"Id\":\"S2\",\"X\":8,\"Y\":0,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7},{\"Id\":\"S3\",\"X\":4,\"Y\":6,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7}],\"Revision\":1}", 1 },
                    { "UC-101", "{\"Id\":\"UC-101\",\"Name\":\"U\\u010Dionica 101\",\"Description\":\"Postoje\\u0107i raspored u\\u010Dionice\",\"Width\":8,\"Height\":6,\"Computers\":[{\"Id\":\"1\",\"X\":0.96,\"Y\":1.62},{\"Id\":\"2\",\"X\":2.16,\"Y\":1.62},{\"Id\":\"3\",\"X\":4.4,\"Y\":1.62},{\"Id\":\"4\",\"X\":5.6,\"Y\":1.62},{\"Id\":\"5\",\"X\":6.8,\"Y\":1.62},{\"Id\":\"6\",\"X\":0.96,\"Y\":2.4000000000000004},{\"Id\":\"7\",\"X\":2.16,\"Y\":2.4000000000000004},{\"Id\":\"8\",\"X\":4.4,\"Y\":2.4000000000000004},{\"Id\":\"9\",\"X\":5.6,\"Y\":2.4000000000000004},{\"Id\":\"10\",\"X\":6.8,\"Y\":2.4000000000000004},{\"Id\":\"11\",\"X\":0.96,\"Y\":3.18},{\"Id\":\"12\",\"X\":2.16,\"Y\":3.18},{\"Id\":\"13\",\"X\":4.4,\"Y\":3.18},{\"Id\":\"14\",\"X\":5.6,\"Y\":3.18},{\"Id\":\"15\",\"X\":6.8,\"Y\":3.18},{\"Id\":\"16\",\"X\":0.96,\"Y\":3.96},{\"Id\":\"17\",\"X\":2.16,\"Y\":3.96},{\"Id\":\"18\",\"X\":4.4,\"Y\":3.96},{\"Id\":\"19\",\"X\":5.6,\"Y\":3.96},{\"Id\":\"20\",\"X\":6.8,\"Y\":3.96},{\"Id\":\"21\",\"X\":0.96,\"Y\":4.74},{\"Id\":\"22\",\"X\":2.16,\"Y\":4.74},{\"Id\":\"23\",\"X\":4.4,\"Y\":4.74},{\"Id\":\"24\",\"X\":5.6,\"Y\":4.74},{\"Id\":\"25\",\"X\":6.8,\"Y\":4.74}],\"Sensors\":[{\"Id\":\"S1\",\"X\":0,\"Y\":0,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7},{\"Id\":\"S2\",\"X\":8,\"Y\":0,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7},{\"Id\":\"S3\",\"X\":4,\"Y\":6,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7}],\"Revision\":1}", 1 },
                    { "UC-202", "{\"Id\":\"UC-202\",\"Name\":\"Ra\\u010Dunarska sala 202\",\"Description\":\"Postoje\\u0107i raspored u\\u010Dionice\",\"Width\":8,\"Height\":6,\"Computers\":[{\"Id\":\"1\",\"X\":1.44,\"Y\":1.6800000000000002},{\"Id\":\"2\",\"X\":3.2,\"Y\":1.6800000000000002},{\"Id\":\"3\",\"X\":4.96,\"Y\":1.6800000000000002},{\"Id\":\"4\",\"X\":6.72,\"Y\":1.6800000000000002},{\"Id\":\"5\",\"X\":1.44,\"Y\":2.94},{\"Id\":\"6\",\"X\":3.2,\"Y\":2.94},{\"Id\":\"7\",\"X\":4.96,\"Y\":2.94},{\"Id\":\"8\",\"X\":6.72,\"Y\":2.94},{\"Id\":\"9\",\"X\":1.44,\"Y\":4.199999999999999},{\"Id\":\"10\",\"X\":3.2,\"Y\":4.199999999999999},{\"Id\":\"11\",\"X\":4.96,\"Y\":4.199999999999999},{\"Id\":\"12\",\"X\":6.72,\"Y\":4.199999999999999}],\"Sensors\":[{\"Id\":\"S1\",\"X\":0,\"Y\":0,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7},{\"Id\":\"S2\",\"X\":8,\"Y\":0,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7},{\"Id\":\"S3\",\"X\":4,\"Y\":6,\"Model\":\"ESP32\",\"ReferenceRssi\":-45,\"PathLossExponent\":2.7}],\"Revision\":1}", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "classrooms");

            migrationBuilder.DropColumn(
                name: "room_snapshot_json",
                table: "exam_sessions");
        }
    }
}
