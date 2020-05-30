using System;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

using Octokit.Internal;

namespace Octokit.Authentication.Test
{
    internal static class AuthSecrets
    {
        internal static EmbeddedFileProvider EmbeddedFiles { get; } =
            new EmbeddedFileProvider(typeof(AuthSecrets).Assembly, nameof(Octokit));

        internal static string FilePath { get; } = string.Join("/",
            typeof(AuthSecrets).Namespace.Split('.')
                .Where(s => !s.Equals(nameof(Octokit), StringComparison.OrdinalIgnoreCase))
            )
            + nameof(AuthSecrets) + ".json";

        internal static IConfigurationBuilder AddAuthSecrets(this IConfigurationBuilder config)
        {
            return config.AddJsonFile(EmbeddedFiles, FilePath,
                optional: true, reloadOnChange: false);
        }

        internal static IServiceCollection AddOctokitCredentials(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>()
                    .GetSection(nameof(Credentials));
                var login = config[nameof(Credentials.Login)];
                var password = config[nameof(Credentials.Password)];
                var token = config["Token"];
                var authType = Enum.TryParse(
                    config[nameof(Credentials.AuthenticationType)],
                    ignoreCase: true,
                    out AuthenticationType authTypeParsed
                    ) ? (AuthenticationType?)authTypeParsed : null;

                return authType switch
                {
                    AuthenticationType.Anonymous => Credentials.Anonymous,
                    _ => login switch
                    {
                        string l when !string.IsNullOrEmpty(l) => password switch
                        {
                            string pwd when !string.IsNullOrEmpty(pwd) => authType switch
                            {
                                AuthenticationType at => new Credentials(l, pwd, at),
                                _ => new Credentials(l, pwd),
                            },
                            _ => null
                        },
                        _ => token switch
                        {
                            string t when !string.IsNullOrEmpty(t) => authType switch
                            {
                                AuthenticationType at => new Credentials(t, at),
                                null => new Credentials(t),
                            },
                            _ => null,
                        }
                    }
                } ?? Credentials.Anonymous;
            });
            services.AddSingleton<ICredentialStore, InMemoryCredentialStore>();

            return services;
        }
    }
}
