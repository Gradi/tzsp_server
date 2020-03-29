using System;
using System.IO;
using Serilog;
using TzspServerAnalyzerApi;

namespace TzspServer.Analyzers
{
    internal class HotReloadAnalyzer : IAnalyzer
    {
        private readonly ILogger _logger;
        private readonly string _assemblyPath;

        private readonly object _locker;
        private DateTime _lastModTime;
        private IAnalyzer _currentAnalyzer;
        private System.Timers.Timer _timer;
        private bool _isDisposed;

        public HotReloadAnalyzer(ILogger logger, string assemblyPath)
        {
            _logger = logger
                .ForContext<HotReloadAnalyzer>()
                .ForContext("AssemblyPath", assemblyPath);
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException(null, assemblyPath);
            _assemblyPath = assemblyPath;

            _locker = new object();
            _lastModTime = File.GetLastWriteTime(assemblyPath);
            _currentAnalyzer = new PluggableAnalyzer(assemblyPath);
            _isDisposed = false;

            _timer = new System.Timers.Timer(300);
            _timer.AutoReset = false;
            _timer.Elapsed += TimerEvent;
            _timer.Start();
        }

        public DataPacket Handle(DataPacket dataPacket)
        {
            lock (_locker)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(HotReloadAnalyzer));
                return _currentAnalyzer.Handle(dataPacket);
            }
        }

        public void Dispose()
        {
            lock (_locker)
            {
                if (_isDisposed)
                    return;

                _timer.Stop();
                _timer.Elapsed -= TimerEvent;
                _timer.Dispose();
                _timer = null;

                try
                {
                    _currentAnalyzer.Dispose();
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
            if (_isDisposed)
                return;

            try
            {
                var currentModTime = File.GetLastWriteTime(_assemblyPath);
                if (currentModTime > _lastModTime)
                {
                    _logger.Information("Detected change of {AssemblyPath}. Reloading",
                        _assemblyPath);
                    _lastModTime = currentModTime;

                    using var stream = File.OpenRead(_assemblyPath);
                    IAnalyzer newAnalyzer = new PluggableAnalyzer(_assemblyPath);
                    IAnalyzer oldAnalyzer = null;

                    lock (_locker)
                    {
                        if (!_isDisposed)
                        {
                            oldAnalyzer = _currentAnalyzer;
                            _currentAnalyzer = newAnalyzer;
                            _logger.Information("Reloaded.");
                        }
                        else
                            newAnalyzer.Dispose();
                    }

                    oldAnalyzer?.Dispose();
                }

                lock (_locker)
                {
                    if (!_isDisposed)
                        _timer.Start();
                }
            }
            catch(Exception exception)
            {
                _logger.Error(exception, "Error on hot reloading {AssemblyPath}",
                    _assemblyPath);

                lock (_locker)
                {
                    if (!_isDisposed)
                        _timer.Start();
                }
            }
        }
    }
}
