using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Serilog.Events;

namespace TzspServer
{
    public class CommandLineArguments
    {
        [Option("listen-address", HelpText = "Address to listen for TZSP packets. If not set defaults to 0.0.0.0")]
        public string? ListenAddress { get; set; }

        [Option('p', HelpText = "Port to listen", Default = 37008)]
        public int Port { get; set; }

        [Option("log", HelpText = "Enables console logging", Default = true)]
        public bool IsConsoleLoggingEnabled { get; set; }

        [Option("log-file", HelpText = "Log file path. If not set then file logging will be disabled.")]
        public string? LogFile { get; set; }

        [Option("log-level", HelpText = "Log level", Default = LogEventLevel.Verbose)]
        public LogEventLevel LogLevel { get; set; }

        [Option('a', HelpText = "File paths to assemblies containning analyzers")]
        public IEnumerable<string>? Analyzers { get; set; }

        [Option("buffer-size", HelpText = "Receiver buffer size (in bytes)", Default = 1048576)]
        public int BufferSize { get; set; }

        public void ValidateAndThrow()
        {
            var errors = new List<string>();

            if (Port <= 0 || Port > 65535)
                errors.Add($"Port out of range({Port}).");

            if (BufferSize <= 0)
                errors.Add($"Buffer size is <= 0({BufferSize}).");

            if (Analyzers == null || Analyzers.Count() == 0)
                errors.Add("No analyzers are set. Add paths to some external analyzers.");
            else
            {
                foreach (var path in Analyzers)
                {
                    if (!File.Exists(path))
                        errors.Add($"File not found \"{path}\"");
                }
            }

            if (errors.Count != 0)
            {
                var n = Environment.NewLine;
                throw new Exception($"Invalid command line options:{n}{string.Join(n, errors)}");
            }
        }
    }
}
