using System.Linq;
using System.Text;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TzspServer.Extensions;

namespace TzspServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CommandLineArguments>(args).WithParsed(NewMain);
        }

        private static void NewMain(CommandLineArguments args)
        {
            args.ValidateAndThrow();
            using var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(CommandLineArguments args)
        {
            var builder = new HostBuilder();
            builder.UseDefaultServiceProvider(sp =>
            {
                sp.ValidateOnBuild = true;
                sp.ValidateScopes = true;
            })
            .UseConsoleLifetime()
            .ConfigureLogging(log =>
            {
                ConfigureSerilog(log, args);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(args);
                services.AddHostedService<HostedTzspListener>();
                services.AddSingleton<IPacketProcessor, SinglePacketProcessor>();

                foreach (var analyzerPath in args.Analyzers!)
                    services.AddExternalAnalyzer(analyzerPath);

            });
            return builder;
        }

        private static void ConfigureSerilog(ILoggingBuilder log, CommandLineArguments args)
        {
            var config = new LoggerConfiguration();
            config.MinimumLevel.Is(args.LogLevel);

            if (args.IsConsoleLoggingEnabled)
                config.WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(args.LogFile))
                config.WriteTo.File(args.LogFile, shared: true, encoding: Encoding.UTF8);

            Serilog.Log.Logger = config.CreateLogger();

            log.Services.AddSingleton<Serilog.ILogger>(Serilog.Log.Logger);
            log.ClearProviders();
            log.AddSerilog();
        }
    }
}
