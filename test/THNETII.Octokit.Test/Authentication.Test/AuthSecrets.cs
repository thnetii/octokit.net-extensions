using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Octokit.Internal;

namespace Octokit.Authentication.Test
{
    internal static class AuthSecrets
    {
        internal static IServiceCollection AddOctokitCredentials(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var sectionName = ConfigurationPath.Combine(
                    nameof(Octokit), nameof(Credentials));
                var config = provider.GetRequiredService<IConfiguration>()
                    .GetSection(sectionName);
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
