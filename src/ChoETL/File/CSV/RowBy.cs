using System;
#if !_ALL_NET_
using System.Buffers;
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
#if !_ALL_NET_
    internal interface IFL : IDisposable
    {
        /// <summary>
        /// Reads the file and fills the internal buffer.
        /// </summary>
        /// <returns>
        /// The number of characters that have been read. 
        /// This method returns zero when no more characters are left to read.
        /// </returns>
        int FillBuffer();

        /// <summary>
        /// Read and returns the records inside the buffer.
        /// </summary>
        IEnumerable<ReadOnlyMemory<char>> ReadLines();
    }
    internal abstract class RowBy : IFL
    {
        protected int i = 0;
        protected int j = 0;
        protected int c;
        protected TextReader reader;
        protected bool initial = true;

        protected bool yieldLast = false;

        protected int bufferLength;
        protected char[] buffer;
        protected Memory<char> memory;

        public RowBy(TextReader reader, int bufferLength)
        {
            this.reader = reader;

            buffer = ArrayPool<char>.Shared.Rent(bufferLength);
            this.bufferLength = buffer.Length;
        }

        public int FillBuffer()
        {
            var len = i - j;
            if (initial == false)
            {
                if (len == buffer.Length)
                    throw new ChoParserException("Record is too large.");

                Array.Copy(buffer, j, buffer, 0, len);
            }

            var totalRead = reader.Read(buffer, len, bufferLength - len);
            bufferLength = len + totalRead;

            memory = buffer.AsMemory(0, bufferLength);
            i = 0;
            j = 0;

            initial = false;

            if (totalRead == 0 && len != 0 && yieldLast == false)
            {
                yieldLast = true;
                return len;
            }

            return totalRead;
        }

        public abstract IEnumerable<ReadOnlyMemory<char>> ReadLines();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetRecord(out Memory<char> record)
        {
            record = buffer.AsMemory(j, i - j).TrimEndLineEnd();
            return record.Length > 0;
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                ArrayPool<char>.Shared.Return(buffer);
                buffer = null;
            }
        }
    }
    internal class RowByQuote : RowBy
    {
        public readonly string separator;

        public RowByQuote(TextReader reader, int bufferLength, string separator)
            : base(reader, bufferLength)
        {
            this.separator = separator;
        }

        public override IEnumerable<ReadOnlyMemory<char>> ReadLines()
        {
            int Peek() => i < bufferLength ? buffer[i] : -1;

            var hasBufferToConsume = false;
            var quote = QuoteHelper.Quote.Char;

            reloop:

            j = i;

            while (hasBufferToConsume = i < bufferLength)
            {
                var index = memory.Span.Slice(i).IndexOfAny('\r', '\n', '"');
                if (index < 0)
                {
                    i = bufferLength;
                    continue;
                }
                else
                {
                    i += index;
                }

                c = buffer[i++];

                charLoaded:

                if (c == '\r')
                {
                    if (Peek() == '\n')
                    {
                        i++;
                    }
                    goto afterLoop;
                }
                else if (c == '\n')
                {
                    goto afterLoop;
                }
                else if (c == quote)
                {
                    ReadOnlySpan<char> span = buffer.AsSpan().Slice(0, i - 1);
                    var isQuotedField = IsFirstColumn(span) || span.TrimEnd().EndsWith(separator.AsSpan());

                    if (isQuotedField is false)
                        continue;

                    // 1 Outside quoted field
                    // 2 Inside quoted field
                    // 3 Possible escaped quote (the first " in "")

                    var state = 2;

                    while (hasBufferToConsume = i < bufferLength)
                    {
                        c = buffer[i++];
                        switch (state)
                        {
                            case 2:
                                if (c == quote)
                                    state = 3;
                                continue;
                            case 3:
                                state = c == quote ? 2 : 1;
                                if (state == 1)
                                    goto charLoaded;
                                continue;
                        }
                    }
                }
            }

            afterLoop:

            if (hasBufferToConsume == false)
            {
                if (yieldLast && TryGetRecord(out var x))
                    yield return x;

                yield break;
            }

            if (TryGetRecord(out var y))
                yield return y;

            goto reloop;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsFirstColumn(ReadOnlySpan<char> span)
        {
            var onlyWhiteSpace = true;

            for (var i = span.Length - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(span[i]))
                {
                    if (span[i] is '\n' || span[i] is '\r')
                        return true;
                    else
                        continue;
                }
                onlyWhiteSpace = false;
                break;
            }

            return onlyWhiteSpace;
        }
    }

    internal class RowByLine : RowBy
    {
        public RowByLine(TextReader reader, int bufferLength)
            : base(reader, bufferLength)
        {
        }

        public override IEnumerable<ReadOnlyMemory<char>> ReadLines()
        {
            int Peek() => i < bufferLength ? buffer[i] : -1;

            var hasBufferToConsume = false;

            reloop:

            j = i;

            while (hasBufferToConsume = i < bufferLength)
            {
                var index = memory.Span.Slice(i).IndexOfAny('\r', '\n');
                if (index < 0)
                {
                    i = bufferLength;
                    continue;
                }
                else
                {
                    i += index;
                }

                c = buffer[i++];

                switch (c)
                {
                    case '\r':
                        if (Peek() == '\n')
                        {
                            i++;
                        }
                        goto afterLoop;

                    case '\n':
                        goto afterLoop;
                }
            }

            afterLoop:

            if (hasBufferToConsume == false)
            {
                if (yieldLast && TryGetRecord(out var x))
                    yield return x;

                yield break;
            }

            if (TryGetRecord(out var y))
                yield return y;

            goto reloop;
        }
    }
    internal static class MemoryExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<char> TrimEndLineEnd(this Memory<char> current)
        {
            var i = current.Length - 1;

            for (; i >= 0; i--)
            {
                if (current.Span[i] is '\n' || current.Span[i] is '\r')
                    break;
            }

            return current.Slice(0, i + 1);
        }
        public static IEnumerable<T> Skip<T>(this IEnumerable<T> source, bool hasHeader) =>
        hasHeader
        ? source.Skip(1)
        : source;

    }
    internal static class QuoteHelper
    {
        public static readonly (char Char, string String) Quote = ('"', "\"");

        public static void ThrowIfSeparatorContainsQuote(string separator)
        {
            if (separator.Contains(Quote.Char))
                throw new ArgumentException("Separator must not contain quote char", nameof(separator));
        }
    }
#endif
}
