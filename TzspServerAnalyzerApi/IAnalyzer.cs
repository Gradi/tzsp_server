using System;

namespace TzspServerAnalyzerApi
{
    /// <summary>
    /// Interface for packet analyzers.
    /// </summary>
    public interface IAnalyzer : IDisposable
    {
        /// <summary>
        /// Handles single data packet. Method is guaranteed
        /// to be called by one thread at the time.
        /// </summary>
        /// <returns><see cref="DataPacket"/> instance. You may create a new one. Return null to short-circuit chain.</returns>
        DataPacket Handle(DataPacket dataPacket);
    }
}
