using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

namespace Octokit.Test
{
    public class GitHubClientTest
    {
        private static void VisitAllClientTypes(Type rootType, HashSet<Type> result)
        {
            const BindingFlags ifPropBinding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (!result.Add(rootType))
                return;

            foreach (var pi in rootType.GetProperties(ifPropBinding).Where(pi => pi.CanRead && pi.PropertyType.Name.EndsWith("Client", StringComparison.Ordinal)))
            {
                VisitAllClientTypes(pi.PropertyType, result);
            }
        }

        public static HashSet<Type> GetGitHubClientTypes()
        {
            var vistedClients = new HashSet<Type>();
            VisitAllClientTypes(typeof(IGitHubClient), vistedClients);
            return vistedClients;
        }

        public static IEnumerable<object[]> GetGitHubClientTypesMemberData() =>
            GetGitHubClientTypes().Select(t => new[] { t });

        [Fact]
        public void RecursiveEnumerationOverAllGitHubClientTypes()
        {
            var types = GetGitHubClientTypes();

            Assert.NotEmpty(types);
            Assert.All(types, t => Assert.True(t.IsInterface));
        }

        [Theory]
        [MemberData(nameof(GetGitHubClientTypesMemberData))]
        public static void AllGitHubClientHaveImplementationsWithApiConnectionConstructor(Type interfaceType)
        {
            var octokitAssembly = typeof(GitHubClient).Assembly;

            var implTypes = octokitAssembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic)
                .Where(t => t.GetInterfaces().Contains(interfaceType))
                .ToList();

            Assert.Single(implTypes, t =>
            {
                const BindingFlags ctorBinding = BindingFlags.Instance | BindingFlags.Public;
                var ctor = t.GetConstructor(ctorBinding, Type.DefaultBinder,
                    new[] { typeof(IApiConnection) }, null)
                    ?? t.GetConstructor(ctorBinding, Type.DefaultBinder,
                    new[] { typeof(IConnection) }, null);
                return !(ctor is null);
            });
        }
    }
}
