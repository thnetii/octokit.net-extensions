using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Octokit.Authentication.Test;
using Octokit.GitHubSourceLink.Test;
using Octokit.Test;

using THNETII.Octokit.FileProviders;

using Xunit;

namespace Octokit.FileProviders.Test
{
    public class GitHubRepositoryFileProviderTest
    {
        private readonly IServiceProvider provider;
        private readonly GitHubClient client;
        private readonly GitHubSourceLinkOptions sourceLink;

        public GitHubRepositoryFileProviderTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddAuthSecrets()
                .AddGitHubSourceLinkInfo()
                .Build());
            services.AddOctokitCredentials();
            services.AddGitHubSourceLinkOptions();

            provider = services.BuildServiceProvider();

            client = new GitHubClient(
                AssemblyProductHeaderValue.Instance,
                provider.GetRequiredService<ICredentialStore>());

            sourceLink = provider.GetRequiredService<IOptions<GitHubSourceLinkOptions>>()
                .Value;
        }

        [Fact]
        public void CtorThrowsForNullClientWithOwnerAndName()
        {
            IGitHubClient client = null!;
            Assert.Throws<ArgumentNullException>(nameof(client), () =>
            {
                _ = new GitHubRepositoryFileProvider(client, sourceLink.Owner, sourceLink.Repository);
            });
        }

        [Fact]
        public void CtorThrowsForNullOwner()
        {
            var client = this.client;
            string owner = null!;
            string repo = sourceLink.Repository;

            Assert.Throws<ArgumentNullException>(nameof(owner),
                () => new GitHubRepositoryFileProvider(client,
                    owner, repo));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        public void CtorThrowsForInvalidNonNullOwner(string owner)
        {
            string repo = sourceLink.Repository;

            Assert.Throws<ArgumentException>(nameof(owner),
                () => new GitHubRepositoryFileProvider(client,
                    owner, repo));
        }

        [Fact]
        public void CtorThrowsForNullRepositoryName()
        {
            string owner = sourceLink.Owner;
            string repository = null!;

            Assert.Throws<ArgumentNullException>(nameof(repository),
                () => new GitHubRepositoryFileProvider(client,
                    owner, repository));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        public void CtorThrowsForInvalidNonNullRepositoryName(string repository)
        {
            string owner = sourceLink.Owner;

            Assert.Throws<ArgumentException>(nameof(repository),
                () => new GitHubRepositoryFileProvider(client,
                    owner, repository));
        }

        [Fact]
        public void CtorAcceptsNullReferenceWithOwnerAndName()
        {
            string owner = sourceLink.Owner;
            string repository = sourceLink.Repository;

            _ = new GitHubRepositoryFileProvider(client,
                    owner, repository, null);
        }

        [SkippableFact]
        public void GetFileInfoForRepositoryLicense()
        {
            RepositoryContentLicense? licsenseContent;
            try
            {
                licsenseContent = client.Repository.GetLicenseContents(
                    sourceLink.Owner, sourceLink.Repository
                    )
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (NotFoundException) { licsenseContent = null; }

            Skip.If(licsenseContent is null, "Repository does not contain a LICENSE file");

            var fileProvider = new GitHubRepositoryFileProvider(
                client, sourceLink.Owner, sourceLink.Repository,
                sourceLink.Reference);

            var licenseFileInfo = fileProvider.GetFileInfo(licsenseContent.Path);

            Assert.NotNull(licenseFileInfo);
            Assert.True(licenseFileInfo.Exists);
            Assert.Equal(licsenseContent.Name, licenseFileInfo.Name);
            Assert.False(licenseFileInfo.IsDirectory);
        }

        [Fact]
        public void GetDirectoryContentOfRootMatchClientContents()
        {
            var fileProvider = new GitHubRepositoryFileProvider(
                client, sourceLink.Owner, sourceLink.Repository,
                sourceLink.Reference);

            var directoryContents = fileProvider.GetDirectoryContents("");

            Task<IReadOnlyList<RepositoryContent>> clientContentsTask =
                sourceLink.Reference is null
                ? client.Repository.Content.GetAllContents(
                    sourceLink.Owner,
                    sourceLink.Repository)
                : client.Repository.Content.GetAllContentsByRef(
                    sourceLink.Owner,
                    sourceLink.Repository,
                    sourceLink.Reference);
            var clientContents = clientContentsTask.ConfigureAwait(false)
                .GetAwaiter().GetResult();

            Assert.True(directoryContents.Exists);
            Assert.Equal(
                clientContents.Select(c => c.Path),
                directoryContents.Select(fi => fi.Name),
                StringComparer.OrdinalIgnoreCase
                );
        }
    }
}
