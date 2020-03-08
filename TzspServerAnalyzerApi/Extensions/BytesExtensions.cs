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
            if (bytes == null)
                return string.Empty;
            return AsHexLower(bytes, 0, bytes.Length);
        }

        public static string AsHexUpper(this byte[] bytes)
        {
            if (bytes == null)
                return string.Empty;
            return AsHexUpper(bytes, 0, bytes.Length);
        }

        public static string AsAsciiLetters(this byte[] bytes)
        {
            if (bytes == null)
                return string.Empty;
            return AsAsciiLetters(bytes, 0, bytes.Length);
        }

        public static string AsHexLower(this byte[] bytes, int start, int length)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(length * 2);
            for (int i = start; i < length; ++i)
                builder.Append(_lowerHex[bytes[i]]);
            return  builder.ToString();
        }

        public static string AsHexUpper(this byte[] bytes, int start, int length)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(length * 2);
            for (int i = start; i < length; ++i)
                builder.Append(_upperHex[bytes[i]]);
            return builder.ToString();
        }

        public static string AsAsciiLetters(this byte[] bytes, int start, int length)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(length);
            for (int i = start; i < length; ++i)
                builder.Append(_asciiLetters[bytes[i]]);
            return builder.ToString();
        }
    }
}
