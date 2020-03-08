using System.Collections.Generic;
using CommandLine;
using Serilog.Events;

namespace TzspServer
{
    public class CommandLineArguments
    {
        [Option("listen-address", HelpText = "Address to listen for TZSP packets. If not set defaults to 0.0.0.0")]
        public string ListenAddress { get; set; }

        [Option('p', HelpText = "Port to listen", Default = 37008)]
        public int Port { get; set; }

        [Option("log", HelpText = "Enables console logging", Default = false)]
        public bool IsConsoleLoggingEnabled { get; set; }

        [Option("log-file", HelpText = "Log file path. If not set then file logging will be disabled.")]
        public string LogFile { get; set; }

        [Option("log-level", HelpText = "Log level", Default = LogEventLevel.Verbose)]
        public LogEventLevel LogLevel { get; set; }

        [Option('a', HelpText = "File paths to assemblies containning analyzers")]
        public IEnumerable<string> Analyzers { get; set; }

        [Option("buffer-size", HelpText = "Receiver buffer size (in bytes)", Default = 1048576)]
        public int BufferSize { get; set; }

        [Option("timeout", HelpText = "Timeout of socket in miliseconds", Default = 5000)]
        public int Timeout { get; set; }

        [Option("queue-size", HelpText = "Size of the queue.", Default = 50000)]
        public int QueueSize { get; set; }
    }
}
