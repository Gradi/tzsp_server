using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using PacketDotNet;
using TzspServerAnalyzerApi;
using TzspServerAnalyzerApi.Attributes;

namespace TzspServer.Analyzers
{
    internal class PluggableAnalyzer : IAnalyzer
    {
        private static readonly string OwnApiName;
        private static readonly Version OwnApiVersion;

        private AssemblyLoadContext _asmContext;
        private IReadOnlyCollection<IAnalyzer> _analyzers;

        static PluggableAnalyzer()
        {
            var asm = Assembly.GetAssembly(typeof(IAnalyzer))!;
            OwnApiVersion = asm.GetName().Version!;
            OwnApiName = asm.GetName().FullName;
        }

        public PluggableAnalyzer(string assemblyPath)
        {
            _asmContext = new AssemblyLoadContext(null, true);
            var assembly = LoadMainDllWithDeps(_asmContext, assemblyPath);

            var refApiName = assembly.GetReferencedAssemblies()
                .FirstOrDefault(r => r.FullName == OwnApiName);
            if (refApiName == null)
            {
                throw new ArgumentException($"Assembly {assembly.GetName()} doesn't seem to have refence to {OwnApiName}.");
            }
            if (refApiName.Version != OwnApiVersion)
            {
                throw new ArgumentException($"Assembly {assembly.GetName()} references different version of api " +
                                            $"(My: {OwnApiVersion}, Plugin: {refApiName.Version}).");
            }

            var analyzerTypes = assembly.GetCustomAttribute<AnalyzersOrderAttribute>()?.AnalyzerTypes;
            if (analyzerTypes == null || analyzerTypes.Count == 0)
            {
                throw new ArgumentException($"Assembly {assembly.GetName()} doesn't have {nameof(AnalyzersOrderAttribute)} " +
                                            $"or collection is empty.");
            }

            if (analyzerTypes.Any(t => t.IsAbstract || !typeof(IAnalyzer).IsAssignableFrom(t)))
            {
                throw new ArgumentException($"Assembly's {nameof(AnalyzersOrderAttribute)} have entries that doesn't " +
                                            $"implement {nameof(IAnalyzer)} interface.");
            }

            var analyzers = new IAnalyzer[analyzerTypes.Count];
            int index = 0;
            foreach (var type in analyzerTypes)
            {
                try
                {
                    analyzers[index++] = (IAnalyzer)(Activator.CreateInstance(type) ??
                                                     throw new Exception($"{nameof(Activator)}.{nameof(Activator.CreateInstance)}" +
                                                                         $"returned null."));
                }
                catch(Exception exception)
                {
                    throw new Exception($"Can't instantiate analyzer of type {type}.", exception);
                }
            }
            _analyzers = analyzers;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public AResult Handle(LinkLayers linkLayers, Packet packet, object? context, CancellationToken cancellationToken)
        {
            var lastResult = AResult.Continue();
            foreach (var analyzer in _analyzers)
            {
                lastResult = analyzer.Handle(linkLayers, packet, context, cancellationToken);
                if (!lastResult.IsContinue)
                    return AResult.Stop();

                if (lastResult.IsNewContext)
                    context = lastResult.Context;
            }

            // This way we don't leak third party assembly's
            // context to outer world.
            // But third party assembly can still capture
            // outer world's context.
            // So, is this worth it?
            return lastResult.IsContinue ?
                AResult.Continue() : AResult.Stop();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            foreach (var analyzer in _analyzers)
            {
                try
                {
                    analyzer.Dispose();
                }
                catch
                {
                    /* Intentionally left empty. */
                }
            }

            _analyzers = null!;
            _asmContext.Unload();
            _asmContext = null!;
        }

        private static Assembly LoadMainDllWithDeps(AssemblyLoadContext context, string mainAsmPath)
        {
            using var stream = File.OpenRead(mainAsmPath);
            var asm = context.LoadFromStream(stream);

            var dirPath = Path.GetDirectoryName(mainAsmPath)!;
            var refs = asm.GetReferencedAssemblies()
                .Select(r => Path.Combine(dirPath, r.Name + ".dll"))
                .Where(File.Exists);

            foreach (var dllPath in refs)
            {
                using var depStream = File.OpenRead(dllPath);
                context.LoadFromStream(depStream);
            }
            return asm;
        }
    }
}
