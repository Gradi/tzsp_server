using System;
using System.Collections.Generic;

namespace TzspServerAnalyzerApi.Attributes
{
    /// <summary>
    /// Defines order of analyzers.
    /// This attribute required event if assembly contains
    /// single analyzer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class AnalyzersOrderAttribute : Attribute
    {
        public IReadOnlyCollection<Type> AnalyzerTypes { get; }

        public AnalyzersOrderAttribute(params Type[] types)
        {
            AnalyzerTypes = types;
        }
    }
}
