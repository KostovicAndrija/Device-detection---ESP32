namespace Application.Ingestion;

public sealed class RssiIngestionService(IRssiProcessingPipeline pipeline) : IRssiIngestionService
{
    public Task IngestAsync(RssiIngressMessage message, CancellationToken cancellationToken = default)
        => pipeline.ProcessAsync(message, cancellationToken);
}
