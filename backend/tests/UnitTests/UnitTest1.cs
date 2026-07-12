using Application.Ingestion;

namespace UnitTests;

public class UnitTest1
{
    [Fact]
    public async Task IngestAsync_ForwardsMessageToPipeline()
    {
        var pipeline = new FakePipeline();
        var service = new RssiIngestionService(pipeline);
        var message = new RssiIngressMessage("device-hash", "sensor-1", "session-1", -64, DateTimeOffset.UtcNow);

        await service.IngestAsync(message);

        Assert.Equal(message, pipeline.LastMessage);
    }

    private sealed class FakePipeline : IRssiProcessingPipeline
    {
        public RssiIngressMessage? LastMessage { get; private set; }

        public Task ProcessAsync(RssiIngressMessage message, CancellationToken cancellationToken = default)
        {
            LastMessage = message;
            return Task.CompletedTask;
        }
    }
}
