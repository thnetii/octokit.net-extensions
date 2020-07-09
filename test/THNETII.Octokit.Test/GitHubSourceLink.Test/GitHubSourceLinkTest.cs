using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Octokit.Authentication.Test;
using Octokit.Test;

using Xunit;
using Xunit.Abstractions;

namespace Octokit.GitHubSourceLink.Test
{
    public class GitHubSourceLinkTest : IAsyncDisposable
    {
        private readonly IHost host;
        private readonly IServiceProvider serviceProvider;
        private readonly GitHubClient client;

        public GitHubSourceLinkTest(ITestOutputHelper outputHelper)
        {
            host = TestHostBuilder.CreateHostBuidler()
                .ConfigureLogging(logging => logging.AddXUnit(outputHelper))
                .ConfigureServices((context, services) =>
                {
                    services.AddOctokitCredentials();
                    services.AddGitHubSourceLinkOptions();
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
        public void GetsRepositoryContents()
        {
            var sourceLink = serviceProvider
                .GetRequiredService<IOptions<GitHubSourceLinkOptions>>()
                .Value;

            Task<IReadOnlyList<RepositoryContent>> contentTask =
                sourceLink.Reference is null
                ? client.Repository.Content.GetAllContents(
                    sourceLink.Owner,
                    sourceLink.Repository)
                : client.Repository.Content.GetAllContentsByRef(
                    sourceLink.Owner,
                    sourceLink.Repository,
                    sourceLink.Reference);
            var contents = contentTask.ConfigureAwait(false)
                .GetAwaiter().GetResult();

            Assert.NotEmpty(contents);
        }

        public async ValueTask DisposeAsync()
        {
            await host.StopAsync(TimeSpan.FromMilliseconds(120))
                .ConfigureAwait(false);
            host.Dispose();
        }
    }
}
