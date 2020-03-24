using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Serilog;
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

        public ILogger Logger
        {
            get => throw new NotSupportedException();
            set
            {
                if (_analyzers != null)
                {
                    foreach (var analyzer in _analyzers)
                        analyzer.Logger = value;
                }
            }
        }

        static PluggableAnalyzer()
        {
            var asm = Assembly.GetAssembly(typeof(IAnalyzer));
            OwnApiVersion = asm.GetName().Version;
            OwnApiName = asm.GetName().FullName;
        }

        public PluggableAnalyzer(string assemblyPath)
        {
            _asmContext = new AssemblyLoadContext(null, true);
            var assembly = LoadMainDllWithDeps(_asmContext, assemblyPath);

            var refApiName = assembly.GetReferencedAssemblies()
                .FirstOrDefault(r => r.FullName == OwnApiName);
            if (refApiName == null)
                throw new ArgumentException($"Assembly {assembly.GetName()} doesn't seem to have refence to {OwnApiName}.");
            if (refApiName.Version != OwnApiVersion)
                throw new ArgumentException($"Assembly {assembly.GetName()} references different version of api (My: {OwnApiVersion}, Plugin: {refApiName.Version}).");

            var analyzerTypes = assembly.GetCustomAttribute<AnalyzersOrderAttribute>()?.AnalyzerTypes;
            if (analyzerTypes == null || analyzerTypes.Count == 0)
                throw new ArgumentException($"Assembly {assembly.GetName()} doesn't have {nameof(AnalyzersOrderAttribute)} or collection is empty.");
            if (analyzerTypes.Any(t => t.IsAbstract || !typeof(IAnalyzer).IsAssignableFrom(t)))
                throw new ArgumentException($"Assembly's {nameof(AnalyzersOrderAttribute)} have entries that doesn't implement {nameof(IAnalyzer)} interface.");

            var analyzers = new IAnalyzer[analyzerTypes.Count];
            int index = 0;
            foreach (var type in analyzerTypes)
            {
                try
                {
                    analyzers[index++] = (IAnalyzer)Activator.CreateInstance(type);
                }
                catch(Exception exception)
                {
                    throw new Exception($"Can't instantiate analyzer of type {type}.", exception);
                }
            }
            _analyzers = analyzers;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DataPacket Handle(DataPacket dataPacket)
        {
            foreach (var analyzer in _analyzers)
            {
                dataPacket = analyzer.Handle(dataPacket);
                if (dataPacket == null)
                    return null;
            }
            return dataPacket;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            try
            {
                Logger = null;
                foreach (var analyzer in _analyzers)
                    analyzer.Dispose();
            }
            finally
            {
                _analyzers = null;
                _asmContext?.Unload();
                _asmContext = null;
            }
        }

        private static Assembly LoadMainDllWithDeps(AssemblyLoadContext context, string mainAsmPath)
        {
            using var stream = File.OpenRead(mainAsmPath);
            var asm = context.LoadFromStream(stream);

            var dirPath = Path.GetDirectoryName(mainAsmPath);
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
