using System;
using PacketDotNet;

namespace TzspPacketUnpacker
{
    public readonly ref struct PacketData
    {
        public readonly LinkLayers LinkLayer;
        public readonly Span<byte> Data;

        public PacketData(LinkLayers linkLayer, Span<byte> data)
        {
            LinkLayer = linkLayer;
            Data = data;
        }
    }
}
