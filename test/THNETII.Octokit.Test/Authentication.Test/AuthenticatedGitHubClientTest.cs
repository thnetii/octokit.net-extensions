
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Octokit.Test;

using Xunit;
using Xunit.Abstractions;

namespace Octokit.Authentication.Test
{
    public sealed class AuthenticatedGitHubClientTest : IAsyncDisposable
    {
        private readonly IHost host;
        private readonly IServiceProvider serviceProvider;
        private readonly GitHubClient client;

        public AuthenticatedGitHubClientTest(ITestOutputHelper outputHelper)
        {
            host = TestHostBuilder.CreateHostBuidler()
                .ConfigureLogging(logging => logging.AddXUnit(outputHelper))
                .ConfigureServices((context, services) =>
                {
                    services.AddOctokitCredentials();
                })
                .Build();
            host.Start();
            serviceProvider = host.Services;

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

        public async ValueTask DisposeAsync()
        {
            await host.StopAsync(TimeSpan.FromMilliseconds(120))
                .ConfigureAwait(false);
            host.Dispose();
        }
    }
}
