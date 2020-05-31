using System;
using System.Diagnostics.CodeAnalysis;
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
    public static class ServiceCollectionTests
    {
        public static class AddGitHubClient
        {
            [Fact]
            public static void ThrowsForNullServiceCollection()
            {
                IServiceCollection services = null!;
                Assert.Throws<ArgumentNullException>(nameof(services), () =>
                {
                    services.AddGitHubClient();
                });
            }

            [Fact]
            public static void ReturnsNonNullBuilder()
            {
                var services = new ServiceCollection();
                var builder = services.AddGitHubClient();

                Assert.NotNull(builder);
                Assert.IsType<GitHubServiceBuilder>(builder);

                Assert.Same(services, builder.Services);
                Assert.Null(builder.Name);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("test")]
            public static void ReturnsNonNullBuilderWithName(string name)
            {
                var services = new ServiceCollection();
                var builder = services.AddGitHubClient(name);

                Assert.NotNull(builder);
                Assert.IsType<GitHubServiceBuilder>(builder);
                Assert.Same(name, builder.Name);
            }
        }

        public static class UseAssemblyProductHeader
        {
            [Fact]
            public static void ThrowsForNullAssembly()
            {
                var services = new ServiceCollection();
                var builder = services.AddGitHubClient();

                Assert.Throws<ArgumentNullException>("assembly", () =>
                {
                    builder.UseAssemblyProductHeader(null!);
                });
            }

            [Fact]
            public static void RegistersProductHeaderValueServiceUsingTestClassAssembly()
            {
                var services = new ServiceCollection();

                var returnInstance = services.AddGitHubClient()
                    .UseAssemblyProductHeader(typeof(ServiceCollectionTests).Assembly)
                    ;

                Assert.NotNull(returnInstance);
                Assert.Contains(services, desc =>
                {
                    return desc.ServiceType == typeof(ProductHeaderValue);
                });
            }

            [Fact]
            public static void RegistersProductHeaderValueServiceUsingEntryAssembly()
            {
                var services = new ServiceCollection();

                var returnInstance = services.AddGitHubClient()
                    .UseAssemblyProductHeader(Assembly.GetEntryAssembly());
                ;

                Assert.NotNull(returnInstance);
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

                var returnInstance = services.AddGitHubClient()
                    .UseHttpClientFactoryConnection();

                Assert.NotNull(returnInstance);
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

                var returnInstance = services.AddGitHubClient()
                    .UseHttpClientFactoryConnection();

                Assert.NotNull(returnInstance);
                Assert.Contains(service,
                    services.Select(desc => desc.ServiceType)
                    );
            }

            [Fact]
            public static void RequireIConnectionThrowsWithoutProductHeaderValue()
            {
                var services = new ServiceCollection();

                var returnInstance = services.AddGitHubClient()
                    .UseHttpClientFactoryConnection();

                using var provider = services.BuildServiceProvider();

                Assert.NotNull(returnInstance);
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
                    .AddAuthSecrets()
                    .Build());
                services.AddOctokitCredentials();

                var returnInstance = services.AddGitHubClient()
                    .UseAssemblyProductHeader(typeof(ServiceCollectionTests).Assembly)
                    .UseHttpClientFactoryConnection();

                using var provider = services.BuildServiceProvider();

                var credentials = provider.GetRequiredService<Credentials>();
                var credStore = provider.GetRequiredService<ICredentialStore>();
                var connection = provider.GetRequiredService<IConnection>();

                Assert.NotEqual(AuthenticationType.Anonymous, credentials.AuthenticationType);
                Assert.Equal(credStore, connection.CredentialStore);
                Assert.Equal(credentials, connection.Credentials);

                Assert.NotNull(returnInstance);
            }
        }

        public static class AddClients
        {
            [Fact]
            public static void ReturnsSameBuilder()
            {
                var services = new ServiceCollection();

                var builder = services.AddGitHubClient();
                var returnInstance = builder.AddClients();

                Assert.NotNull(returnInstance);
                Assert.Same(builder, returnInstance);
            }

            [Theory]
            [MemberData(nameof(GitHubClientTest.GetGitHubClientTypesMemberData), MemberType = typeof(GitHubClientTest))]
            public static void RegistersRequiredService(Type service)
            {
                var services = new ServiceCollection();

                services.AddGitHubClient()
                    .AddClients();

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

            var returnInstance = services.AddGitHubClient()
                .UseAssemblyProductHeader(typeof(ServiceCollectionTests).Assembly)
                .UseHttpClientFactoryConnection();

            using var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService(service);
            Assert.IsAssignableFrom(service, instance);
            Assert.IsAssignableFrom(implementation, instance);
            Assert.NotNull(returnInstance);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("test")]
        public static void GetRequiredServiceReturnsHttpClientWithName(string name)
        {
            var services = new ServiceCollection();
            services.AddGitHubClient(name)
                .UseAssemblyProductHeader(typeof(ServiceCollectionTests).Assembly)
                .UseHttpClientFactoryConnection();

            using var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService<IHttpClient>();
            Assert.IsAssignableFrom<HttpClientAdapter>(instance);
        }

        [Theory]
        [MemberData(nameof(GitHubClientTest.GetGitHubClientTypesMemberData), MemberType = typeof(GitHubClientTest))]
        public static void GetRequiredServiceReturnsApiClient(Type client)
        {
            var services = new ServiceCollection();
            services.AddGitHubClient()
                .UseAssemblyProductHeader(typeof(ServiceCollectionTests).Assembly)
                .UseHttpClientFactoryConnection()
                .AddClients()
                ;

            using var provider = services.BuildServiceProvider();

            var instance = provider.GetRequiredService(client);
            Assert.IsAssignableFrom(client, instance);
        }
    }
}
