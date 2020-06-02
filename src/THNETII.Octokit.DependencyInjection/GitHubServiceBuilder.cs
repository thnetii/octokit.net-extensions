using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Octokit.Internal;

namespace Octokit.DependencyInjection
{
    public class GitHubServiceBuilder
    {
        public GitHubServiceBuilder(IServiceCollection services, string? name)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Name = name;
        }

        public IServiceCollection Services { get; }
        public string? Name { get; }

        public GitHubServiceBuilder UseAssemblyProductHeader(Assembly assembly)
        {
            _ = assembly ?? throw new ArgumentNullException(nameof(assembly));

            var asmName = assembly.GetName();
            string version = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?? asmName.Version.ToString();

            var header = new ProductHeaderValue(asmName.Name, version);
            Services.AddSingleton(header);

            return this;
        }

        public GitHubServiceBuilder UseHttpClientFactoryConnection()
        {
            if (!Services.Any(desc => desc.ServiceType == typeof(IHttpMessageHandlerFactory)))
            {
                if (Name is string name)
                    Services.AddHttpClient(name);
                else
                    Services.AddHttpClient();
            }

            Services.AddTransient<IHttpClient>(provider =>
            {
                var httpFactory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
                var adapter = Name is string name
                    ? new HttpClientAdapter(() => httpFactory.CreateHandler(name))
                    : new HttpClientAdapter(httpFactory.CreateHandler);
                return adapter;
            });
            Services.TryAddSingleton<IJsonSerializer, SimpleJsonSerializer>();
            Services.AddTransient<IConnection, Connection>(provider =>
            {
                var credStore = provider.GetService<ICredentialStore>() ??
                    new InMemoryCredentialStore(Credentials.Anonymous);
                var serializer = provider.GetService<IJsonSerializer>()
                        ?? new SimpleJsonSerializer();
                var client = provider.GetRequiredService<IHttpClient>();
                return ActivatorUtilities.CreateInstance<Connection>(
                    provider, GitHubClient.GitHubApiUrl, credStore, client, serializer);
            });
            Services.AddTransient<IApiConnection, ApiConnection>();

            return this;
        }

        public GitHubServiceBuilder AddClients()
        {
            Services.AddTransient<IGitHubClient>(provider =>
            {
                if (provider.GetService<IConnection>() is IConnection connection)
                    return new GitHubClient(connection);
                return ActivatorUtilities.CreateInstance<GitHubClient>(provider);
            });

            foreach (var pair in GetGitHubClientTypesWithImplementation())
                Services.TryAddTransient(pair.Key, pair.Value);

            return this;
        }

        private static Dictionary<Type, Type> GetGitHubClientTypesWithImplementation()
        {
            var githubClientTypes = new Dictionary<Type, Type>();
            VisitAllClientTypes(typeof(IGitHubClient), githubClientTypes);
            return githubClientTypes;

            static void VisitAllClientTypes(Type interfaceType, Dictionary<Type, Type> knownTypes)
            {
                const BindingFlags ifPropBinding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (knownTypes.TryGetValue(interfaceType, out _))
                    return; // Interface already known

                if (FindImplementationType(interfaceType) is Type implType)
                    knownTypes[interfaceType] = implType;

                foreach (var pi in interfaceType.GetProperties(ifPropBinding).Where(pi => pi.CanRead && pi.PropertyType.Name.EndsWith("Client", StringComparison.Ordinal)))
                {
                    VisitAllClientTypes(pi.PropertyType, knownTypes);
                }
            }

            static Type? FindImplementationType(Type interfaceType)
            {
                var octokitAssembly = typeof(GitHubClient).Assembly;
                return octokitAssembly.GetTypes()
                    .Where(t => t.IsClass && t.IsPublic)
                    .Where(t => t.GetInterfaces().Contains(interfaceType))
                    .SingleOrDefault(t =>
                    {
                        const BindingFlags ctorBinding = BindingFlags.Instance | BindingFlags.Public;
                        var ctor = t.GetConstructor(ctorBinding, Type.DefaultBinder,
                            new[] { typeof(IApiConnection) }, null)
                            //?? t.GetConstructor(ctorBinding, Type.DefaultBinder,
                            //new[] { typeof(ApiConnection) }, null)
                            ?? t.GetConstructor(ctorBinding, Type.DefaultBinder,
                            new[] { typeof(IConnection) }, null)
                            //?? t.GetConstructor(ctorBinding, Type.DefaultBinder,
                            //new[] { typeof(Connection) }, null)
                            ;
                        return !(ctor is null);
                    });
            }
        }
    }
}
