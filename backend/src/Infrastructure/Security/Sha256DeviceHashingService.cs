using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

public sealed class Sha256DeviceHashingService(IOptions<HashingOptions> options) : IDeviceHashingService
{
    private readonly HashingOptions _options = options.Value;

    public string Hash(string rawIdentifier)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier))
        {
            throw new ArgumentException("Raw identifier is required.", nameof(rawIdentifier));
        }

        var payload = $"{_options.Salt}:{rawIdentifier.Trim().ToLowerInvariant()}:{_options.Pepper}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
