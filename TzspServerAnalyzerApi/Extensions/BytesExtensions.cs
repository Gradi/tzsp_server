using System;
using System.Linq;
using System.Text;

namespace TzspServerAnalyzerApi.Extensions
{
    public static class BytesExtensions
    {
        private static readonly string[] _lowerHex;
        private static readonly string[] _upperHex;
        private static readonly char[] _asciiLetters;

        static BytesExtensions()
        {
            _lowerHex = new string[(int)byte.MaxValue + 1];
            _upperHex = new string[(int)byte.MaxValue + 1];
            _asciiLetters = new char[(int)byte.MaxValue + 1];

            for(int i = 0; i <= byte.MaxValue; ++i)
            {
                _lowerHex[i] = i.ToString("x2").ToLowerInvariant();
                _upperHex[i] = i.ToString("x2").ToUpperInvariant();

                char ch = Encoding.ASCII.GetChars(new byte[] { (byte)i }).Single();
                if (!Char.IsControl(ch))
                    _asciiLetters[i] = ch;
                else
                    _asciiLetters[i] = '.';
            }
        }

        public static string AsHexLower(this byte[] bytes)
        {
            ReadOnlySpan<byte> span = bytes;
            return AsHexLower(span);
        }

        public static string AsHexUpper(this byte[] bytes)
        {
            ReadOnlySpan<byte> span = bytes;
            return AsHexUpper(span);
        }

        public static string AsAsciiLetters(this byte[] bytes)
        {
            ReadOnlySpan<byte> span = bytes;
            return AsAsciiLetters(span);
        }

        public static string AsHexLower(this ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(span.Length * 2);
            for (int i = 0; i < span.Length; ++i)
                builder.Append(_lowerHex[span[i]]);
            return  builder.ToString();
        }

        public static string AsHexUpper(this ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(span.Length * 2);
            for (int i = 0; i < span.Length; ++i)
                builder.Append(_upperHex[span[i]]);
            return builder.ToString();
        }

        public static string AsAsciiLetters(this ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(span.Length);
            for (int i = 0; i < span.Length; ++i)
                builder.Append(_asciiLetters[span[i]]);
            return builder.ToString();
        }
    }
}
