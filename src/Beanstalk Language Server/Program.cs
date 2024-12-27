using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace BeanstalkLanguageServer;

internal static class Program
{
	private static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration()
			.Enrich.FromLogContext()
			.WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
			.MinimumLevel.Verbose()
			.CreateLogger();

		IObserver<WorkDoneProgressReport> workDone = null!;
		
		var server = await LanguageServer.From
		(
			options =>
				options
					.WithInput(Console.OpenStandardInput())
					.WithOutput(Console.OpenStandardOutput())
					.ConfigureLogging
					(
						x => x
							.AddSerilog(Log.Logger)
							.AddLanguageProtocolLogging()
							.SetMinimumLevel(LogLevel.Debug)
					)
					.WithHandler<TextDocumentHandler>()
					.WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
					.WithServices
					(
						services =>
						{
							services.AddSingleton
							(
								provider =>
								{
									var loggerFactory = provider.GetService<ILoggerFactory>()!;
									var logger = loggerFactory.CreateLogger<Logger>();

									return new Logger(logger);
								}
							);

							services
								.AddSingleton(new ConfigurationItem { Section = "typescript" })
								.AddSingleton(new ConfigurationItem { Section = "terminal" });
						}
					)
					.OnInitialize
					(
						async (server, request, _) =>
						{
							var manager = server.WorkDoneManager.For
							(
								request, new WorkDoneProgressBegin
								{
									Title = "Initializing Beanstalk language server...",
									Percentage = 0
								}
							);

							workDone = manager;
						}
					)
					.OnInitialized
					(
						async (server, request, response, cancellationToken) =>
						{
							workDone.OnNext
							(
								new WorkDoneProgressReport
								{
									Percentage = 10,
									Message = "Initialized"
								}
							);

							workDone.OnCompleted();
						}
					)
					.OnStarted
					(
						async (languageServer, cancellationToken) =>
						{
							var logger = languageServer.Services.GetService<ILogger<Logger>>()!;
							var configuration = await languageServer.Configuration.GetConfiguration
							(
								new ConfigurationItem
								{
									Section = "typescript"
								},
								new ConfigurationItem
								{
									Section = "terminal"
								}
							).ConfigureAwait(false);

							var baseConfig = new JObject();
							foreach (var config in languageServer.Configuration.AsEnumerable())
							{
								baseConfig.Add(config.Key, config.Value);
							}
							
							logger.LogInformation("Base Config {@Config}", baseConfig);

							var scopeConfig = new JObject();
							foreach (var config in configuration.AsEnumerable())
							{
								baseConfig.Add(config.Key, config.Value);
							}
							
							logger.LogInformation("Scoped Config {@Config}", scopeConfig);
							
						}
					)
		).ConfigureAwait(false);

		await server.WaitForExit.ConfigureAwait(false);
	}
}

internal class Logger
{
	private readonly ILogger<Logger> logger;

	public Logger(ILogger<Logger> logger)
	{
		this.logger = logger;
	}
}