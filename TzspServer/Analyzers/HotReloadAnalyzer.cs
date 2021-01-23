using System;
using System.IO;
using System.Threading;
using PacketDotNet;
using Serilog;
using TzspServerAnalyzerApi;

namespace TzspServer.Analyzers
{
    internal class HotReloadAnalyzer : IAnalyzer
    {
        private readonly ILogger _logger;
        private readonly string _assemblyPath;

        private readonly object _locker;
        private bool _isDisposed;
        private DateTime _lastModTime;
        private IAnalyzer? _currentAnalyzer;
        private System.Timers.Timer? _timer;

        public HotReloadAnalyzer(ILogger logger, string assemblyPath)
        {
            _logger = logger
                .ForContext<HotReloadAnalyzer>()
                .ForContext("AssemblyPath", assemblyPath);
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException(null, assemblyPath);
            _assemblyPath = assemblyPath;

            _locker = new object();
            _isDisposed = false;
            _lastModTime = File.GetLastWriteTime(assemblyPath);
            _currentAnalyzer = new PluggableAnalyzer(assemblyPath);

            _timer = new System.Timers.Timer(300);
            _timer.AutoReset = false;
            _timer.Elapsed += TimerEvent;
            _timer.Start();
        }

        public AResult Handle(LinkLayers linkLayers, Packet packet, object? context, CancellationToken cancellationToken)
        {
            IAnalyzer analyzer;

            lock (_locker)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(HotReloadAnalyzer));
                analyzer = _currentAnalyzer!; // If we are not disposed, analyzer exists.
            }

            return analyzer.Handle(linkLayers, packet, context, cancellationToken);
        }

        public void Dispose()
        {
            lock (_locker)
            {
                if (_isDisposed) return;

                _timer!.Stop();
                _timer.Elapsed -= TimerEvent;
                _timer.Dispose();
                _timer = null;

                try
                {
                    _currentAnalyzer!.Dispose();
                }
                finally
                {
                    _currentAnalyzer = null;
                    _isDisposed = true;
                }
            }
        }

        private void TimerEvent(object sender, System.Timers.ElapsedEventArgs args)
        {
            if (_isDisposed) return;

            try
            {
                var currentModTime = File.GetLastWriteTime(_assemblyPath);
                if (currentModTime > _lastModTime)
                {
                    _logger.Information("Detected change of {AssemblyPath}. Reloading", _assemblyPath);
                    _lastModTime = currentModTime;

                    IAnalyzer newAnalyzer = new PluggableAnalyzer(_assemblyPath);
                    IAnalyzer? oldAnalyzer = null;

                    lock (_locker)
                    {
                        if (!_isDisposed)
                        {
                            oldAnalyzer = _currentAnalyzer;
                            _currentAnalyzer = newAnalyzer;
                        }
                        else
                            newAnalyzer.Dispose();
                    }

                    oldAnalyzer?.Dispose();
                    _logger.Information("Reloaded.");
                }
            }
            catch(Exception exception)
            {
                _logger.Error(exception, "Error on hot reloading {AssemblyPath}", _assemblyPath);
            }
            finally
            {
                lock (_locker)
                {
                    _timer?.Start();
                }
            }
        }
    }
}
