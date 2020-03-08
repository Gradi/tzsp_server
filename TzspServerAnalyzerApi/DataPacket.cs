using System;
using System.Collections.Generic;
using PacketDotNet;

namespace TzspServerAnalyzerApi
{
    public sealed class DataPacket
    {
        private Packet _packet;
        private bool _isPacketParsed;

        public ulong PacketCounter { get; set; }

        public DateTime PacketArrivalTime { get; set; }

        public byte[] RawPacket { get; set; }

        public LinkLayers LinkLayer { get; set; }

        public Packet Packet
        {
            get
            {
                if (!_isPacketParsed)
                {
                    _packet = Packet.ParsePacket(LinkLayer, RawPacket);
                    _isPacketParsed = true;
                }
                return _packet;
            }
            set
            {
                _packet = value;
                _isPacketParsed = true;
            }
        }

        /// <summary>
        /// Additional data you may put for the next analyzers.
        /// </summary>
        public IDictionary<object, object> Data { get; set; }
    }
}
