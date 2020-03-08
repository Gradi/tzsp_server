using System;
using System.Linq;
using System.Threading;
using CommandLine;
using Serilog;
using SimpleInjector;
using TzspServer.Analyzers;
using TzspServerAnalyzerApi;

namespace TzspServer
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CommandLineArguments>(args).WithParsed(NewMain);
        }

        private static void NewMain(CommandLineArguments args)
        {
            var container = SetupDependencies(args);
            var logger = container.GetInstance<ILogger>();
            logger.Information("Starting up...");

            try
            {
                var waitHandle = new ManualResetEventSlim(false);
                Console.CancelKeyPress += (s, a) =>
                {
                    a.Cancel = true;
                    waitHandle.Set();
                };

                container.GetInstance<TzspServer>().Start();
                logger.Information("Started up. Press \"Ctrl+C\" to stop.");
                waitHandle.Wait();
                logger.Information("Ctr+C detected. Stopping.");
            }
            catch(Exception exception)
            {
                logger.Fatal(exception, "Exception on start up.");
            }
            finally
            {
                container.Dispose();
            }
        }

        private static Container SetupDependencies(CommandLineArguments args)
        {
            var container = new Container();
            ConfigureLogging(container, args);
            container.RegisterInstance(args);
            container.Register<IPacketProcessor, QueuedPacketProcessor>(Lifestyle.Singleton);
            container.Register<TzspServer>(Lifestyle.Singleton);

            if (args.Analyzers == null || args.Analyzers.Count() == 0)
            {
                container.Collection.Register(typeof(IAnalyzer), new Type[0]);
            }
            else
            {
                foreach (var path in args.Analyzers)
                {
                    container.Collection.Append<IAnalyzer>(() => new HotReloadAnalyzer(container.GetInstance<ILogger>(), path), Lifestyle.Singleton);
                }
            }

            container.Verify();
            return container;
        }

        private static void ConfigureLogging(Container container, CommandLineArguments args)
        {
            container.Register<ILogger>(() =>
            {
                var config = new LoggerConfiguration();
                if (args.IsConsoleLoggingEnabled)
                    config.WriteTo.Console();
                if (args.LogFile != null)
                    config.WriteTo.File(args.LogFile, shared: true);
                config.MinimumLevel.Is(args.LogLevel);
                var logger = config.CreateLogger();
                Serilog.Log.Logger = logger;
                return logger;
            }, Lifestyle.Singleton);
        }
    }
}
