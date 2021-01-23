using System;
using PacketDotNet;

namespace TzspPacketUnpacker
{
    public readonly ref struct PacketData
    {
        public readonly LinkLayers LinkLayer;
        public readonly ReadOnlySpan<byte> Data;

        public PacketData(LinkLayers linkLayer, ReadOnlySpan<byte> data)
        {
            LinkLayer = linkLayer;
            Data = data;
        }
    }
}
