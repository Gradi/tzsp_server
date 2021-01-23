using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace TzspServer
{
    public class HostedTzspListener : IHostedService
    {
        private readonly ILogger _logger;
        private readonly CommandLineArguments _args;
        private readonly IPacketProcessor _packetProcessor;

        private Socket? _socket;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listeningTask;

        public HostedTzspListener
            (
                ILogger logger,
                CommandLineArguments commandLineArguments,
                IPacketProcessor packetProcessor
            )
        {
            _logger = logger.ForContext<HostedTzspListener>();
            _args = commandLineArguments;
            _packetProcessor = packetProcessor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var endPoint = GetEndPoint();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(endPoint);
            if (!_socket.IsBound)
                throw new Exception($"Can't bind socket to {endPoint}.");
            _socket.ReceiveBufferSize = _args.BufferSize;
            _logger.Information("Listening on {$EndPoint}", endPoint);

            _cancellationTokenSource = new CancellationTokenSource();
            _listeningTask = Task.Factory.StartNew(ListenAsync, TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_socket == null)
                return;

            _logger.Information("Shutting down listening...");

            _cancellationTokenSource!.Cancel();
            await _listeningTask!;

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _listeningTask = null;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Dispose();
            }
            catch(Exception exception)
            {
                throw new Exception("Error on closing socket.", exception);
            }
            finally
            {
                _socket = null;
            }
        }

        private async Task ListenAsync()
        {
            var token = _cancellationTokenSource!.Token;

            var buffer = new byte[_args.BufferSize];
            var memory = new Memory<byte>(buffer);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var size = await _socket!.ReceiveAsync(memory, SocketFlags.None, token);
                    if (size > 0)
                    {
                        ProcessPacket(new ReadOnlySpan<byte>(buffer, 0, size));
                    }
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "Error on receiving data from socket.");
                }
            }
        }

        private void ProcessPacket(ReadOnlySpan<byte> bytes)
        {
            try
            {
                _packetProcessor.Process(bytes, _cancellationTokenSource!.Token);
            }
            catch(Exception exception)
            {
                _logger.Error(exception, "Error on processing packet of {BytesSize} size.", bytes.Length);
            }
        }

        private IPEndPoint GetEndPoint()
        {
            if (!IPAddress.TryParse(_args.ListenAddress, out var address))
            {
                _logger.Warning("Can't parse ip address({IpAddress}). Will listen on all interfaces.",
                    _args.ListenAddress);
                address = IPAddress.Any;
            }
            return new IPEndPoint(address, _args.Port);
        }
    }
}
