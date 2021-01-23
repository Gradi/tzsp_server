using System;

namespace TzspPacketUnpacker
{
    public class TzspUnpackException : Exception
    {
        internal TzspUnpackException(string message) : base($"Error on unpacking TZSP packet: {message}") {}
    }
}
