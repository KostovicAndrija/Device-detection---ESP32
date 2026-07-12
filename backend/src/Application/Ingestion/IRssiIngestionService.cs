namespace Application.Ingestion;

public interface IRssiIngestionService
{
    Task IngestAsync(RssiIngressMessage message, CancellationToken cancellationToken = default);
}
