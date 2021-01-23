using System.Threading;
using PacketDotNet;

namespace TzspServerAnalyzerApi
{
    /// <summary>
    /// Helper implementation of analyzer that processes only
    /// specific kind of packets.
    /// </summary>
    /// <typeparam name="T">Kind of packet to process (<seealso cref="TcpPacket"/>, <seealso cref="UdpPacket"/>, etc.)</typeparam>
    public abstract class PacketFilterAnalyzer<T> : IAnalyzer where T : Packet
    {
        public virtual AResult Handle(LinkLayers linkLayers, Packet packet, object? context, CancellationToken cancellationToken)
        {
            while (packet != null)
            {
                if (packet is T target)
                {
                    return Handle(linkLayers, target, context, cancellationToken);
                }
                packet = packet.PayloadPacket;
            }
            return AResult.Continue();
        }

        public abstract void Dispose();

        protected abstract AResult Handle(LinkLayers linkLayers, T packet, object? context, CancellationToken cancellationToken);
    }
}
