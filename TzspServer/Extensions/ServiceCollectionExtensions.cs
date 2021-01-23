using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TzspServer.Analyzers;
using TzspServerAnalyzerApi;

namespace TzspServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalAnalyzer(this IServiceCollection services, string assemblyFilePath)
        {
            return services.AddSingleton<IAnalyzer>(sp =>
                        new HotReloadAnalyzer(sp.GetRequiredService<ILogger>(), assemblyFilePath));
        }
    }
}
