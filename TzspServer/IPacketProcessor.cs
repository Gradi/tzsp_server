using System;

namespace TzspServer
{
    public interface IPacketProcessor : IDisposable
    {
        void Process(byte[] bytes, int length);
    }
}
