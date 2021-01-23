using System;
using PacketDotNet;

namespace TzspPacketUnpacker
{
    public static class TzspHelper
    {
        private const byte Version = 1;
        private const byte TagPadding = 0;
        private const byte TagEnd = 1;
        private const int MinimumLength = 4;

        public static PacketData Parse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < MinimumLength)
                throw new TzspUnpackException($"Packet length is < {MinimumLength}.");

            var state = State.Version;
            LinkLayers linkLayer = LinkLayers.Null;

            for(int i = 0; i < bytes.Length; ++i)
            {
                switch (state)
                {
                    case State.Version:
                        if (Version != bytes[i])
                            throw new TzspUnpackException($"Version number mismatch. {Version} != {bytes[i]}.");
                        state = State.Type;
                        break;

                    case State.Type:
                        if (bytes[i] > 5)
                            throw new TzspUnpackException($"Invalid type value {bytes[i]}.");
                        state = State.Protocol;
                        break;

                    case State.Protocol:
                        ushort protocol = (ushort) ((bytes[i] << 8) | (bytes[i + 1]));
                        i += 1;
                        linkLayer = GetLinkLayerFromProtocol(protocol);
                        state = State.TaggedFields;
                        break;

                    case State.TaggedFields:
                        for (int j = i; j < bytes.Length; )
                        {
                            var type = bytes[j];
                            if (type == TagPadding)
                            {
                                j += 1;
                                continue;
                            }
                            if (type == TagEnd)
                            {
                                i = j;
                                break;
                            }
                            var tagLength = bytes[j + 1];
                            j += 2 + tagLength;
                        }
                        state = State.Packet;
                        break;

                    case State.Packet:
                            return new PacketData(linkLayer, bytes.Slice(i));
                }
            }

            throw new TzspUnpackException("Invalid TZSP packet structure (missing bytes).");
        }

        private static LinkLayers GetLinkLayerFromProtocol(ushort protocol)
        {
            switch (protocol)
            {
                case 1: return LinkLayers.Ethernet;
                case 18: return LinkLayers.Ieee802;
                default: return LinkLayers.Null;
            }
        }

        private enum State
        {
            Version,
            Type,
            Protocol,
            TaggedFields,
            Packet
        }
    }
}
