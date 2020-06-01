using System;

using Microsoft.Extensions.DependencyInjection;

namespace Octokit.DependencyInjection
{
    public static class GitHubServiceCollectionExtensions
    {
        public static IServiceCollection AddGitHubClient(
            this IServiceCollection services, string? name,
            Action<GitHubServiceBuilder> configureBuilder)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));
            _ = configureBuilder ?? throw new ArgumentNullException(nameof(configureBuilder));

            var builder = new GitHubServiceBuilder(services, name);
            configureBuilder(builder);

            return services;
        }

        public static IServiceCollection AddGitHubClient(
            this IServiceCollection services,
            Action<GitHubServiceBuilder> configureBuilder) =>
            AddGitHubClient(services, null, configureBuilder);
    }
}
