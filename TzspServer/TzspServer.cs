using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Serilog;

namespace TzspServer
{
    public class TzspServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly CommandLineArguments _args;
        private readonly IPacketProcessor _packetProcessor;

        private readonly object _locker;
        private bool _isDisposed;
        private Socket _socket;
        private Thread _listenerThread;
        private ManualResetEventSlim _stopEvent;

        public TzspServer(ILogger logger, CommandLineArguments args, IPacketProcessor packetProcessor)
        {
            _logger = logger.ForContext<TzspServer>();
            _args = args;
            _packetProcessor = packetProcessor;

            _locker = new object();
            _isDisposed = false;
        }

        public void Start()
        {
            lock (_locker)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(TzspServer));
                if (_socket != null)
                    throw new InvalidProgramException("Tzsp server is already running.");

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.ReceiveTimeout = _args.Timeout;
                _socket.ReceiveBufferSize = _args.BufferSize;
                _logger.Debug("Socket created");

                IPAddress address = null;
                if (!IPAddress.TryParse(_args.ListenAddress, out address))
                {
                    _logger.Warning("Can't parse {IpAddress} string into valid ip address. Will listen on all addresses.",
                        _args.ListenAddress);
                    address = IPAddress.Any;
                }
                var endPoint = new IPEndPoint(address, _args.Port);
                _socket.Bind(endPoint);
                if (!_socket.IsBound)
                    throw new Exception($"Can't bind socket to {endPoint} end point.");
                _logger.Information("Socket bound to {EndPoint}", endPoint);

                _stopEvent = new ManualResetEventSlim(false);
                _listenerThread = new Thread(ThreadProc);
                _listenerThread.Name = $"UDP connection listener on endpoint: {endPoint}";
                _listenerThread.Start();
                _logger.Debug("Listener thread created.");
            }
        }

        public void Dispose()
        {
            lock (_locker)
            {
                if (_isDisposed)
                    return;
                if (_socket == null)
                {
                    _isDisposed = true;
                    return;
                }

                try
                {
                    _stopEvent.Set();
                    _listenerThread.Join();

                    _stopEvent.Dispose();
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                    _socket.Dispose();
                }
                finally
                {
                    _socket = null;
                    _stopEvent = null;
                    _listenerThread = null;
                    _isDisposed = true;
                }
            }
        }

        private void ThreadProc()
        {
            var buffer = new byte[_args.BufferSize];
            while (!_stopEvent.IsSet)
            {
                try
                {
                    int size = _socket.Receive(buffer);
                    if (size != 0)
                    {
                        _packetProcessor.Process(buffer, size);
                        buffer = new byte[_args.BufferSize];
                    }
                }
                catch(SocketException exception)
                {
                    #if DEBUG
                        _logger.Debug(exception, "Exception on receiving data.");
                    #endif
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "General exception on receiving data.");
                }
            }
        }
    }
}
