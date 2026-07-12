namespace Application.Ingestion;

public interface IRssiProcessingPipeline
{
    Task ProcessAsync(RssiIngressMessage message, CancellationToken cancellationToken = default);
}
