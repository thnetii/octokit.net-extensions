using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Octokit.Authentication.Test;
using Octokit.Internal;
using Octokit.Test;

using Xunit;

namespace Octokit.DependencyInjection.Test
{
    public static class ServiceCollectionTest
    {
        public static class AddGitHubClient
        {
            [Fact]
            public static void ThrowsForNullServiceCollection()
            {
                IServiceCollection services = null!;
                Assert.Throws<ArgumentNullException>(nameof(services), () =>
                {
                    services.AddGitHubClient(_ => { });
                });
            }

            [Fact]
            public static void PassesNonNullBuilder()
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github =>
                {
                    Assert.NotNull(github);
                    Assert.IsType<GitHubServiceBuilder>(github);
                    Assert.Same(services, github.Services);
                    Assert.Null(github.Name);
                });
            }

            [Fact]
            public static void ReturnsSameServiceCollection()
            {
                var services = new ServiceCollection();
                var returnInst = services.AddGitHubClient(_ => { });

                Assert.Same(services, returnInst);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("test")]
            public static void PassesNonNullBuilderWithName(string name)
            {
                var services = new ServiceCollection();
                services.AddGitHubClient(name, github =>
                {
                    Assert.NotNull(github);
                    Assert.IsType<GitHubServiceBuilder>(github);
                    Assert.Same(name, github.Name);

                });
            }
        }

        public static class UseAssemblyProductHeader
        {
            [Fact]
            public static void ThrowsForNullAssembly()
            {
                var services = new ServiceCollection();
                services.AddGitHubClient(github =>
                {
                    Assert.Throws<ArgumentNullException>("assembly", () =>
                    {
                        github.UseAssemblyProductHeader(null!);
                    });
                });
            }

            [Fact]
            public static void RegistersProductHeaderValueServiceUsingTestClassAssembly()
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github => github
                    .UseAssemblyProductHeader(typeof(ServiceCollectionTest).Assembly)
                    );

                Assert.Contains(services, desc =>
                {
                    return desc.ServiceType == typeof(ProductHeaderValue);
                });
            }

            [Fact]
            public static void RegistersProductHeaderValueServiceUsingEntryAssembly()
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github => github
                    .UseAssemblyProductHeader(Assembly.GetEntryAssembly()!)
                    );

                Assert.Contains(services, desc =>
                {
                    return desc.ServiceType == typeof(ProductHeaderValue);
                });
            }
        }

        public static class UseHttpClientFactoryConnection
        {
            [Fact]
            public static void DoesNotReRegisterHttpClientFactory()
            {
                var services = new ServiceCollection();
                services.AddHttpClient();

                services.AddGitHubClient(github => github
                    .UseHttpClientFactoryConnection()
                    );

                Assert.Single(services.Select(desc => desc.ServiceType),
                    typeof(IHttpMessageHandlerFactory)
                    );
            }

            [Theory]
            [InlineData(typeof(IHttpMessageHandlerFactory))]
            [InlineData(typeof(IHttpClient))]
            [InlineData(typeof(IConnection))]
            [InlineData(typeof(IApiConnection))]
            public static void RegistersRequiredService(Type service)
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github => github
                    .UseHttpClientFactoryConnection()
                    );

                Assert.Contains(service,
                    services.Select(desc => desc.ServiceType)
                    );
            }

            [Fact]
            public static void RequireIConnectionThrowsWithoutProductHeaderValue()
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github => github
                    .UseHttpClientFactoryConnection()
                    );

                using var provider = services.BuildServiceProvider();

                Assert.Throws<InvalidOperationException>(() =>
                {
                    _ = provider.GetRequiredService<IConnection>();
                });
            }

            [Fact]
            public static void RequireIConnectionReturnsAuthenticatedWithCredentialStore()
            {
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
                    .AddUserSecrets(typeof(UseHttpClientFactoryConnection).Assembly)
                    .Build());
                services.AddOctokitCredentials();

                services.AddGitHubClient(github => github
                    .UseAssemblyProductHeader(typeof(ServiceCollectionTest).Assembly)
                    .UseHttpClientFactoryConnection()
                    );

                using var provider = services.BuildServiceProvider();

                var credentials = provider.GetRequiredService<Credentials>();
                var credStore = provider.GetRequiredService<ICredentialStore>();
                var connection = provider.GetRequiredService<IConnection>();

                Assert.NotEqual(AuthenticationType.Anonymous, credentials.AuthenticationType);
                Assert.Equal(credStore, connection.CredentialStore);
                Assert.Equal(credentials, connection.Credentials);
            }
        }

        public static class AddClients
        {
            [Fact]
            public static void ReturnsSameBuilder()
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github =>
                {
                    Assert.Same(github, github.AddClients());
                });
            }

            [Theory]
            [MemberData(nameof(GitHubClientTest.GetGitHubClientTypesMemberData), MemberType = typeof(GitHubClientTest))]
            public static void RegistersRequiredService(Type service)
            {
                var services = new ServiceCollection();

                services.AddGitHubClient(github => github
                    .AddClients()
                    );

                Assert.Contains(service,
                    services.Select(desc => desc.ServiceType));
            }
        }

        [Theory]
        [InlineData(typeof(ProductHeaderValue), typeof(ProductHeaderValue))]
        [InlineData(typeof(IHttpClient), typeof(HttpClientAdapter))]
        [InlineData(typeof(IConnection), typeof(Connection))]
        [InlineData(typeof(IApiConnection), typeof(ApiConnection))]
        public static void GetRequiredServiceReturnsConcreteImplementation(
            Type service, Type implementation)
        {
            var services = new ServiceCollection();

            services.AddGitHubClient(github => github
                .UseAssemblyProductHeader(typeof(ServiceCollectionTest).Assembly)
                .UseHttpClientFactoryConnection()
                );

            using var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService(service);
            Assert.IsAssignableFrom(service, instance);
            Assert.IsAssignableFrom(implementation, instance);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("test")]
        public static void GetRequiredServiceReturnsHttpClientWithName(string name)
        {
            var services = new ServiceCollection();
            services.AddGitHubClient(name, github => github
                .UseAssemblyProductHeader(typeof(ServiceCollectionTest).Assembly)
                .UseHttpClientFactoryConnection()
                );

            using var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<IHttpClient>();
            Assert.IsAssignableFrom<HttpClientAdapter>(instance);
        }

        [Theory]
        [MemberData(nameof(GitHubClientTest.GetGitHubClientTypesMemberData), MemberType = typeof(GitHubClientTest))]
        public static void GetRequiredServiceReturnsApiClient(Type client)
        {
            var services = new ServiceCollection();
            services.AddGitHubClient(github => github
                .UseAssemblyProductHeader(typeof(ServiceCollectionTest).Assembly)
                .UseHttpClientFactoryConnection()
                .AddClients()
                );

            using var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService(client);
            Assert.IsAssignableFrom(client, instance);
        }
    }
}
