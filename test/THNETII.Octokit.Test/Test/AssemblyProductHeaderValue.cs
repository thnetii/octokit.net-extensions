using System.Reflection;

namespace Octokit.Test
{
    public static class AssemblyProductHeaderValue
    {
        private static readonly Assembly testAssembly =
            typeof(AssemblyProductHeaderValue).Assembly;
        private static readonly AssemblyName testAssemblyName =
            testAssembly.GetName();

        public static ProductHeaderValue Instance { get; } =
            new ProductHeaderValue(
                testAssemblyName.Name,
                testAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? testAssemblyName.Version?.ToString()
                ?? "0.0.1"
                );
    }
}
