using System;

using Microsoft.Extensions.DependencyInjection;

namespace Octokit.DependencyInjection
{
    public static class GitHubServiceCollectionExtensions
    {
        public static GitHubServiceBuilder AddGitHubClient(
            this IServiceCollection services, string? name = null)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            return new GitHubServiceBuilder(services, name);
        }
    }
}
