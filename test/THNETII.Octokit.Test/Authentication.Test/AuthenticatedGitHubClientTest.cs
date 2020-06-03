
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Octokit.Test;

using Xunit;

namespace Octokit.Authentication.Test
{
    public class AuthenticatedGitHubClientTest
    {
        private readonly ServiceProvider serviceProvider;
        private readonly GitHubClient client;

        public AuthenticatedGitHubClientTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddAuthSecrets()
                .Build());
            services.AddOctokitCredentials();

            serviceProvider = services.BuildServiceProvider();

            client = new GitHubClient(
                AssemblyProductHeaderValue.Instance,
                serviceProvider.GetRequiredService<ICredentialStore>()
                );
        }

        [Fact]
        public void IsConnectionAuthenticated()
        {
            Assert.NotEqual(AuthenticationType.Anonymous, client.Credentials.AuthenticationType);
        }

        [Fact]
        public void GetsAuthenticatedUser()
        {
            var currentUser = client.User.Current().ConfigureAwait(false)
                .GetAwaiter().GetResult();

            Assert.NotNull(currentUser);
        }
    }
}
