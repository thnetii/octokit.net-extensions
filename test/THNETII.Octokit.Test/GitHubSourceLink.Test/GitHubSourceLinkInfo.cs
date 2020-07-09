using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Octokit.Test;

namespace Octokit.GitHubSourceLink.Test
{
    public static class GitHubSourceLinkInfo
    {
        internal static string FilePath { get; } =
            typeof(GitHubSourceLinkInfo).FullName + ".json";

        public static string? GetGitShaFromAssemblyVersion()
        {
            string? sha = null;
            if (typeof(GitHubSourceLinkInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion is string version)
            {
                int idx = version.IndexOf('+', StringComparison.Ordinal);
                if (idx >= 0)
                    sha = version.Substring(idx + 1);
            }
            return sha;
        }

        public static IConfigurationBuilder AddGitHubSourceLinkInfo(
            this IConfigurationBuilder config)
        {
            const string baseKey = nameof(GitHubSourceLink);
            return config
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [ConfigurationPath.Combine(baseKey, nameof(GitHubSourceLinkOptions.Owner))] = "thnetii",
                    [ConfigurationPath.Combine(baseKey, nameof(GitHubSourceLinkOptions.Repository))] = "octokit.net-extensions",
                    [ConfigurationPath.Combine(baseKey, nameof(GitHubSourceLinkOptions.Reference))] = GetGitShaFromAssemblyVersion(),
                })
                .AddJsonFile(EmbeddedFiles.Provider, FilePath,
                    optional: true, reloadOnChange: false);
        }

        public static IServiceCollection AddGitHubSourceLinkOptions(
            this IServiceCollection services)
        {
            services.AddOptions<GitHubSourceLinkOptions>()
                .Configure<IConfiguration>((options, config) =>
                    config.Bind(nameof(GitHubSourceLink), options)
                    )
                .Validate(options => options.Validate())
                ;

            return services;
        }
                
    }

    public class GitHubSourceLinkOptions
    {
        public string Owner { get; set; } = null!;
        public string Repository { get; set; } = null!;
        public string? Reference { get; set; }

        [SuppressMessage("Usage", "CA2208: Instantiate argument exceptions correctly", Justification = nameof(Options))]
        public bool Validate()
        {
            if (Owner is null)
                throw new ArgumentNullException(nameof(Owner));
            if (Repository is null)
                throw new ArgumentNullException(nameof(Repository));

            return true;
        }
    }
}
