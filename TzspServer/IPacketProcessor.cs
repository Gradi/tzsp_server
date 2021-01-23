using System;
using System.Threading;

namespace TzspServer
{
    public interface IPacketProcessor : IDisposable
    {
        void Process(ReadOnlySpan<byte> bytes, CancellationToken cancellationToken);
    }
}
