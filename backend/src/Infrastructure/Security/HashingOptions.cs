namespace Infrastructure.Security;

public sealed class HashingOptions
{
    public const string SectionName = "Hashing";

    public string Salt { get; set; } = "device-detection-salt";
    public string Pepper { get; set; } = "device-detection-pepper";
}
