using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Octokit.Authentication.Test;

using Xunit;
using Xunit.Abstractions;

namespace Octokit.DependencyInjection.Test
{
    public class SampleHostTest
    {
        private readonly ITestOutputHelper outputHelper;

        public SampleHostTest(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [SkippableFact]
        public void RetrievesCurrentAuthenticatedUser()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
                .AddAuthSecrets()
                .Build()
                );
            services.AddLogging(logging =>
            {
                logging.AddDebug();
                logging.AddXUnit(outputHelper);
            });
            services.AddOctokitCredentials();

            services.AddGitHubClient(github => github
                .UseAssemblyProductHeader(typeof(SampleHostTest).Assembly)
                .UseHttpClientFactoryConnection()
                .AddClients()
                );

            using var provider = services.BuildServiceProvider();

            var credentials = provider.GetRequiredService<Credentials>();
            Skip.If(credentials.AuthenticationType == AuthenticationType.Anonymous,
                "Test cannot execute, because anonymous credentials were supplied");

            var usersClient = provider.GetRequiredService<IUsersClient>();
            var currentUser = usersClient.Current()
                .ConfigureAwait(false).GetAwaiter().GetResult();

            Assert.NotNull(currentUser);
            if (credentials.Login is string login)
                Assert.Equal(login, currentUser.Login);
        }
    }
}
