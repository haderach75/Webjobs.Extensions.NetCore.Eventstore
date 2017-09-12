using EventStore.ClientAPI.SystemData;

namespace Webjobs.Extensions.NetCore.Eventstore
{
    public interface IUserCredentialFactory
    {
        UserCredentials CreateAdminCredentials(string username, string password);
    }
}