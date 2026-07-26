namespace Application.Abstractions.Security;

public interface IDeviceHashingService
{
    string Hash(string rawIdentifier);
}
