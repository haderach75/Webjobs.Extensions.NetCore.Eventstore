using EventStore.ClientAPI.SystemData;

namespace Webjobs.Extensions.NetCore.Eventstore.Impl
{
    public class UserCredentialFactory : IUserCredentialFactory
    {
        public UserCredentials CreateAdminCredentials(string username, string password)
        {
            username = username ?? "admin";
            password = password ?? "changeit";
            return new UserCredentials(username, password);
        }
    }
}