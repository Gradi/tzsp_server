namespace TzspServerAnalyzerApi
{
    public readonly struct AResult
    {
        public readonly bool IsContinue;
        public readonly bool IsNewContext;
        public readonly object? Context;

        private AResult(bool isContinue, bool isNewContext, object? context)
        {
            IsContinue = isContinue;
            IsNewContext = isNewContext;
            Context = context;
        }

        public static AResult Stop() => new AResult(false, false, null);

        public static AResult Continue() => new AResult(true, false, null);

        public static AResult ContinueWithNewContext(object? context) =>
            new AResult(true, true, context);
    }
}
