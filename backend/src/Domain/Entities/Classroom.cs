using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Entities;

public sealed record ComputerLocation(string Id, double X, double Y,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Width = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Height = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Kind = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int ComputerCount = 0,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool IsTeacherDesk = false);
public sealed record BoardLocation(string Id, double X, double Y, double Width, double Height);
public sealed record SensorLocation(string Id, double X, double Y, string Model = "ESP32", double ReferenceRssi = -45, double PathLossExponent = 2.7);
public sealed record RoomLayout(string Id, string Name, string Description, double Width, double Height,
    IReadOnlyList<ComputerLocation> Computers, IReadOnlyList<SensorLocation> Sensors, int Revision = 1,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BoardLocation>? Boards = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Id.Length > 64 || Id != Id.Trim() || Id.Contains('/') || Id.Contains('\\') ||
            string.IsNullOrWhiteSpace(Name) || Name.Length > 200 || Description is null || Description.Length > 1000)
            throw new ArgumentException("Unesite oznaku i naziv učionice odgovarajuće dužine.");
        if (!double.IsFinite(Width) || !double.IsFinite(Height) || Width is <= 0 or > 1000 || Height is <= 0 or > 1000)
            throw new ArgumentException("Dimenzije moraju biti između 0 i 1000 metara.");
        if (Computers is null || Sensors is null || Computers.Count > 500 || Sensors.Count > 100 || Boards is { Count: > 100 })
            throw new ArgumentException("Učionica podržava do 500 klupa ili računara i do 100 senzora.");
        void Check(string id, double x, double y)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || id != id.Trim() || !double.IsFinite(x) || !double.IsFinite(y) || x < 0 || y < 0 || x > Width || y > Height)
                throw new ArgumentException("Oznake moraju biti popunjene, a koordinate unutar učionice.");
        }
        foreach (var p in Computers)
        {
            Check(p.Id, p.X, p.Y);
            if (p.Kind is not null and not "desk" and not "computer") throw new ArgumentException("Nepoznata vrsta opreme.");
            if (p.ComputerCount is < 0 or > 20) throw new ArgumentException("Broj računara na stolu mora biti između 0 i 20.");
            if (p.Width.HasValue != p.Height.HasValue) throw new ArgumentException("Unesite obe dimenzije klupe.");
            if (p.Width is double w && p.Height is double h &&
                (!double.IsFinite(w) || !double.IsFinite(h) || w <= 0 || h <= 0 ||
                p.X-w/2 < -0.000001 || p.X+w/2 > Width+0.000001 || p.Y-h/2 < -0.000001 || p.Y+h/2 > Height+0.000001))
                throw new ArgumentException("Cela klupa mora biti unutar učionice. Koordinate označavaju njen centar.");
        }
        foreach (var s in Sensors)
        {
            Check(s.Id, s.X, s.Y);
            if (string.IsNullOrWhiteSpace(s.Model) || s.Model.Length > 100 || !double.IsFinite(s.ReferenceRssi) || s.ReferenceRssi is < -120 or > 0 ||
                !double.IsFinite(s.PathLossExponent) || s.PathLossExponent is <= 0 or > 10) throw new ArgumentException("Neispravni parametri senzora.");
        }
        foreach (var board in Boards ?? [])
        {
            Check(board.Id, board.X, board.Y);
            if (!double.IsFinite(board.Width) || !double.IsFinite(board.Height) || board.Width <= 0 || board.Height <= 0 ||
                board.X-board.Width/2 < -0.000001 || board.X+board.Width/2 > Width+0.000001 ||
                board.Y-board.Height/2 < -0.000001 || board.Y+board.Height/2 > Height+0.000001)
                throw new ArgumentException("Tabla mora biti unutar učionice.");
        }
        if (Computers.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Computers.Count ||
            Sensors.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Sensors.Count ||
            (Boards?.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() ?? 0) != (Boards?.Count ?? 0))
            throw new ArgumentException("Oznake opreme i senzora moraju biti jedinstvene unutar svoje grupe.");
    }

    public RoomLayout WithDefaultBoard()
    {
        if (Boards is { Count: > 0 }) return this;
        var boardHeight = Math.Min(0.12, Height * 0.1);
        return this with
        {
            Boards = [new BoardLocation("TABLA-1", Width / 2, boardHeight / 2, Width * 0.5, boardHeight)]
        };
    }
}

public sealed class Classroom
{
    private Classroom() { }
    public string Id { get; private set; } = "";
    public string LayoutJson { get; private set; } = "";
    public int Revision { get; private set; }
    public RoomLayout Layout() => JsonSerializer.Deserialize<RoomLayout>(LayoutJson)!;
    public static Classroom Create(RoomLayout layout)
    {
        layout.Validate();
        return new Classroom { Id = layout.Id, Revision = 1, LayoutJson = JsonSerializer.Serialize(layout with { Revision = 1 }) };
    }
    public void Update(RoomLayout layout)
    {
        layout.Validate();
        if (Id != layout.Id) throw new ArgumentException("Oznaka postojeće učionice se ne menja.");
        if (layout.Revision != Revision) throw new InvalidOperationException("Učionica je promenjena. Osvežite prikaz.");
        Revision++;
        LayoutJson = JsonSerializer.Serialize(layout with { Revision = Revision });
    }

    public static IEnumerable<Classroom> Defaults()
    {
        foreach (var (id, name, columns, rows) in new[] {
            ("UC-101", "Učionica 101", new[]{12d,27,55,70,85}, new[]{27d,40,53,66,79}),
            ("UC-202", "Računarska sala 202", new[]{18d,40,62,84}, new[]{28d,49,70}),
            ("LAB-A", "Laboratorija A", new[]{24d,50,76}, new[]{34d,56,78}) })
        {
            var computers = rows.SelectMany((y,r) => columns.Select((x,c) => new ComputerLocation((r*columns.Length+c+1).ToString(), x/100*8, y/100*6))).ToArray();
            yield return Create(new RoomLayout(id,name,"Postojeći raspored učionice",8,6,computers,
                [new("S1",0,0),new("S2",8,0),new("S3",4,6)]));
        }
    }
}
