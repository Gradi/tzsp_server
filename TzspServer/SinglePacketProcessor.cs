using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PacketDotNet;
using Serilog;
using TzspPacketUnpacker;
using TzspServerAnalyzerApi;

namespace TzspServer
{
    /// <summary>
    /// Packet processor that process single packet at the time.
    /// No queue or something like that.
    /// </summary>
    public class SinglePacketProcessor : IPacketProcessor
    {
        private readonly IAnalyzer[] _analyzers;

        public SinglePacketProcessor
            (
                IEnumerable<IAnalyzer> analyzers,
                ILogger logger
            )
        {
            _analyzers = analyzers.ToArray();

            if (_analyzers.Length == 0)
            {
                logger
                    .ForContext<SinglePacketProcessor>()
                    .Warning("No analyzers.");
            }
        }

        public void Process(ReadOnlySpan<byte> bytes, CancellationToken cancellationToken)
        {
            var data = TzspHelper.Parse(bytes);

            var packet = Packet.ParsePacket(data.LinkLayer, data.Data.ToArray());
            if (packet == null)
                throw new Exception($"Can't parse packet from {data.LinkLayer} link layer.");

            object? context = null;

            foreach (var analyzer in _analyzers)
            {
                var result = analyzer.Handle(data.LinkLayer, packet, context, cancellationToken);

                if (!result.IsContinue)
                    return;

                if (result.IsNewContext)
                    context = result.Context;
            }
        }

        public void Dispose() {}
    }
}
