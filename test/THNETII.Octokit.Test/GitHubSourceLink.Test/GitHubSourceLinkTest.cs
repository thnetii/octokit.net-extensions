using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Octokit.Authentication.Test;
using Octokit.Test;

using Xunit;

namespace Octokit.GitHubSourceLink.Test
{
    public static class GitHubSourceLinkTest
    {
        [Fact]
        public static void GetsRepositoryContents()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddAuthSecrets()
                .AddGitHubSourceLinkInfo()
                .Build());
            services.AddOctokitCredentials();
            services.AddGitHubSourceLinkOptions();

            using var provider = services.BuildServiceProvider();

            var client = new GitHubClient(
                AssemblyProductHeaderValue.Instance,
                provider.GetRequiredService<ICredentialStore>());

            var sourceLink = provider.GetRequiredService<IOptions<GitHubSourceLinkOptions>>()
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
    }
}
