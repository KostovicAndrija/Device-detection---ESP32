using System.Text;
using Application.Ingestion;
using Domain.Entities;

namespace UnitTests;
public sealed class ClassroomAndDecoderTests
{
    [Fact]
    public void Desk_dimensions_round_trip_and_cannot_cross_the_room_boundary()
    {
        var layout = new RoomLayout("A-0-15", "A-0-15", "", 5.88, 8.3807,
            [new("K-1",1.07283,2.4087,2,.6,"desk",2)], [], Boards: [new("TABLA-1",4.41,.06,2.94,.12)]);
        var saved = Classroom.Create(layout).Layout();
        Assert.Equal(2d, saved.Computers[0].Width);
        Assert.Equal("desk", saved.Computers[0].Kind);
        Assert.Equal(2, saved.Computers[0].ComputerCount);
        Assert.Equal(2.94, saved.Boards![0].Width);
        Assert.Throws<ArgumentException>(() => (layout with { Computers = [new("K",.2,2,2,.6,"desk")] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Computers = [new("K",2,2,2,null,"desk")] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Computers = [new("K",2,2,double.NaN,.6,"desk")] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Computers = [new("K",2,2,2,.6,"desk",21)] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Boards = [new("T",.2,.06,2,.12)] }).Validate());
    }
    [Fact]
    public void Room_rejects_outside_coordinates_duplicate_sensors_and_invalid_calibration()
    {
        var layout = Classroom.Defaults().First().Layout();
        Assert.Throws<ArgumentException>(() => (layout with { Computers = [new("PC",9,2)] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Sensors = [new("S1",0,0),new("s1",1,1)] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Sensors = [new("S1",0,0,PathLossExponent:0)] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Width = double.NaN }).Validate());
    }
    [Fact]
    public void New_room_gets_a_board_centered_on_the_top_wall()
    {
        var saved = new RoomLayout("NEW", "Nova", "", 10, 7, [], []).WithDefaultBoard();

        var board = Assert.Single(saved.Boards!);
        Assert.Equal("TABLA-1", board.Id);
        Assert.Equal(5, board.X);
        Assert.Equal(board.Height / 2, board.Y);
        Assert.Equal(5, board.Width);
    }
    [Fact]
    public void Existing_mqtt_contract_is_normalized_by_replaceable_decoder()
    {
        var json = """{"deviceIdentifier":"device","sensorId":"NEW-SENSOR","sessionId":"session","signalType":"bluetooth_pairing","rssi":-61,"timestamp":"2026-09-18T10:00:00Z","eventId":"event"}""";
        var message = new JsonSensorMessageDecoder().Decode(Encoding.UTF8.GetBytes(json));
        Assert.NotNull(message); Assert.Equal("NEW-SENSOR",message.SensorId);Assert.Equal("bluetooth_pairing",message.SignalType);
        Assert.Equal(-61,message.Rssi);Assert.Equal("event",message.EventId);Assert.Equal(10,message.CreatedAt.Hour);
    }
}
