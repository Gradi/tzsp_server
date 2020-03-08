using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PacketDotNet;
using Serilog;
using TzspServerAnalyzerApi;
using TzspServerAnalyzerApi.Extensions;

namespace TzspServer
{
    public class QueuedPacketProcessor : IPacketProcessor
    {
        private readonly ILogger _logger;
        private readonly IAnalyzer[] _analyzers;

        private readonly CircularBuffer<Packet> _queue;
        private readonly AutoResetEvent _newDataEvent;
        private readonly ManualResetEvent _stopEvent;
        private readonly Thread _workerThread;
        private ulong _counter;

        public QueuedPacketProcessor
            (
                ILogger logger,
                IReadOnlyCollection<IAnalyzer> analyzers,
                CommandLineArguments arguments)
        {
            _logger = logger.ForContext<QueuedPacketProcessor>();
            _analyzers = analyzers.ToArray();
            if (_analyzers.Length == 0)
                _logger.Warning("No analyzers.");

            _queue = new CircularBuffer<Packet>(arguments.QueueSize);
            _newDataEvent = new AutoResetEvent(false);
            _stopEvent = new ManualResetEvent(false);
            _workerThread = new Thread(ThreadProc);
            _workerThread.Name = "Packet processing thread";
            _workerThread.Start();
            _counter = 0UL;
        }

        public void Process(byte[] bytes, int length)
        {
            #if DEBUG
            _logger.Debug("Got packet of size {Size}", length);
            #endif

            var packet = new Packet(_counter++, DateTime.Now, bytes, length);
            _queue.Add(packet);
            _newDataEvent.Set();
        }

        public void Dispose()
        {
            _stopEvent.Set();
            _workerThread.Join();

            _newDataEvent.Dispose();
            _stopEvent.Dispose();

            foreach (var analyzer in _analyzers)
            {
                try
                {
                    analyzer.Dispose();
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "Error on disposing analyzer.");
                }
            }
        }

        private void ThreadProc()
        {
            var handlers = new WaitHandle[] { _stopEvent, _newDataEvent };
            while (true)
            {
                WaitHandle.WaitAny(handlers);
                if (_stopEvent.WaitOne(0))
                    return;

                while (_queue.TryTake(out var packet) && !_stopEvent.WaitOne(0))
                {
                    try
                    {
                        ProcessPacket(packet);
                    }
                    catch(Exception exception)
                    {
                        _logger.Error(exception, "Unexpected exception.");
                    }
                }
            }
        }

        private void ProcessPacket(Packet packet)
        {
            LinkLayers linkLayer;
            int offset;
            try
            {
                (linkLayer, offset) = TzspHelper.Parse(packet.Data, packet.Length);
                if ((packet.Length - offset) == 0)
                    throw new Exception("Packet has 0 length.");
            }
            catch(Exception exception)
            {
                _logger.Error(exception, "Can't parse TZSP packet from {BytesHex}",
                    packet.Data.AsHexLower(0, packet.Length));
                return;
            }

            byte[] dataCopy = new byte[packet.Length - offset];
            Array.Copy(packet.Data, offset, dataCopy, 0, packet.Length - offset);
            var dataPacket = new DataPacket
            {
                PacketCounter = packet.Counter,
                PacketArrivalTime = packet.PacketTime,
                RawPacket = dataCopy,
                LinkLayer = linkLayer,
                Data = new Dictionary<object, object>(),
            };

            foreach (var analyzer in _analyzers)
            {
                try
                {
                    dataPacket = analyzer.Handle(dataPacket);
                    if (dataPacket == null)
                        return;
                }
                catch(Exception exception)
                {
                    _logger.Error(exception, "Error on handling data packet.");
                    return;
                }
            }
        }

        private readonly struct Packet
        {
            public readonly ulong Counter;
            public readonly DateTime PacketTime;
            public readonly byte[] Data;
            public readonly int Length;

            public Packet(ulong counter, DateTime packetTime, byte[] data, int length)
            {
                Counter = counter;
                PacketTime = packetTime;
                Data = data;
                Length = length;
            }
        }
    }
}
