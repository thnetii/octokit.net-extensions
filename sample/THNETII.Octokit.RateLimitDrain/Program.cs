using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Octokit;
using Octokit.DependencyInjection;

namespace THNETII.Octokit.RateLimitDrain
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            var cmdRoot = new RootCommand()
            {
                Handler = CommandHandler.Create
                (async (IHost host, CancellationToken cancelToken) =>
                {
                    var provider = host.Services;
                    var logger = provider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(Program));
                    var client = provider.GetRequiredService<IGitHubClient>();
                    client.SetRequestTimeout(Timeout.InfiniteTimeSpan);

                    while (!cancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            int remaining, limit;
                            DateTimeOffset reset;

                            try
                            {
                                var meta = await client.Miscellaneous.GetMetadata().ConfigureAwait(false);
                                var apiInfo = client.GetLastApiInfo();
                                var rateLimitInfo = apiInfo.RateLimit;

                                remaining = rateLimitInfo.Remaining;
                                limit = rateLimitInfo.Limit;
                                reset = rateLimitInfo.Reset;

                                logger.LogInformation($"Received Response, rate limit info: {{{nameof(rateLimitInfo.Remaining)}}} / {{{nameof(rateLimitInfo.Limit)}}} messages remaining. Next Rate-Limit window starts at: {{{nameof(rateLimitInfo.Reset)}}}", remaining, limit, reset);
                            }
                            catch (RateLimitExceededException rateLimitExcept)
                            {
                                
                                foreach (var detail in rateLimitExcept.ApiError?.Errors ?? Enumerable.Empty<ApiErrorDetail>())
                                {
                                    logger.LogError(new EventId(rateLimitExcept.HResult, detail.Code),
                                        detail.Message);
                                }

                                remaining = rateLimitExcept.Remaining;
                                limit = rateLimitExcept.Limit;
                                reset = rateLimitExcept.Reset;

                                logger.LogError(new EventId(rateLimitExcept.HResult, nameof(RateLimitExceededException)),
                                    rateLimitExcept, $"Received Response, rate limit info: {{{nameof(rateLimitExcept.Remaining)}}} / {{{nameof(rateLimitExcept.Limit)}}} messages remaining. Next Rate-Limit window starts at: {{{nameof(rateLimitExcept.Reset)}}}",
                                    remaining, limit, reset.ToLocalTime());

                                var timeToReset = rateLimitExcept.Reset - DateTimeOffset.Now;
                                if (timeToReset > TimeSpan.Zero)
                                {
                                    var delayTask = Task.Delay(timeToReset, cancelToken);
                                    logger.LogInformation($"Time to wait until next Rate-Limit window: {{{nameof(HttpResponseHeaders.RetryAfter)}}}",
                                        timeToReset);
                                    await delayTask.ConfigureAwait(false);
                                }
                                else
                                {
                                    logger.LogDebug("Time to next Rate-Limit windows is less than 0. Continuing with next request immediately.");
                                }
                            }
                        }
                        catch (OperationCanceledException except)
                        {
                            logger.LogWarning(except, "Stopping program due to intercepted cancellation");
                            break;
                        }
                    }
                })
            };
            var cmdParser = new CommandLineBuilder(cmdRoot)
                .UseDefaults()
                .UseHost(Host.CreateDefaultBuilder, host =>
                {
                    host.ConfigureServices(services =>
                    {
                        services.AddGitHubClient(nameof(Octokit), github => github
                            .UseAssemblyProductHeader(typeof(Program).Assembly)
                            .UseHttpClientFactoryConnection()
                            .AddClients()
                            );
                    });
                })
                .Build();

            return cmdParser.InvokeAsync(args);
        }
    }
}
