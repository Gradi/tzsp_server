using System;
using System.Threading;
using PacketDotNet;

namespace TzspServerAnalyzerApi
{
    /// <summary>
    /// Interface for packet analyzers.
    /// </summary>
    public interface IAnalyzer : IDisposable
    {
        /// <summary>
        /// Analyzes current packet. This method is guaranteed
        /// to be called from single thread at the time.
        /// </summary>
        /// <param name="linkLayers">Link layer of current packet.</param>
        /// <param name="packet">Current network packet.</param>
        /// <param name="context">(Optionally) Context from any previous analyzers.</param>
        /// <returns>Result indicating if we must pass packet to next analyzers in chain or not.</returns>
        AResult Handle(LinkLayers linkLayers, Packet packet, object? context, CancellationToken cancellationToken);
    }

}
