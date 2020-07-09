
using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Octokit.Test
{
    public static class TestHostBuilder
    {
        public static IHostBuilder CreateHostBuidler(string[]? args = null) =>
            Host.CreateDefaultBuilder(args ?? Array.Empty<string>())
                .UseEnvironment("Development")
                .ConfigureAppConfiguration(config =>
                {
                    config.AddUserSecrets(typeof(TestHostBuilder).Assembly,
                        optional: false);
                })
                .ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(opts =>
                        opts.SuppressStatusMessages = true);
                });

    }
}
