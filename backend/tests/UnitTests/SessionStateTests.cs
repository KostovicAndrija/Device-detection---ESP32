using Domain.Entities;

namespace UnitTests;

public sealed class SessionStateTests
{
    [Fact]
    public void Session_AllowsOnlyPlannedActiveCompletedFlow()
    {
        var session = ExamSession.Create("Test", "R1", DateTimeOffset.UtcNow);

        session.Start(DateTimeOffset.UtcNow);
        session.Stop(DateTimeOffset.UtcNow.AddHours(1));

        Assert.Equal("completed", session.Status);
        Assert.Throws<InvalidOperationException>(() => session.Start(DateTimeOffset.UtcNow));
    }
}
