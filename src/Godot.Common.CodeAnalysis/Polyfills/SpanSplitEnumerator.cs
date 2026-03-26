// https://github.com/dotnet/runtime/blob/5caa14bd341f2639b586fca29874c6f92d9711cc/src/libraries/System.Private.CoreLib/src/System/MemoryExtensions.cs#SpanSplitEnumerator

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;

namespace Godot.Common.CodeAnalysis
{
    internal enum SpanSplitEnumeratorMode
    {
        /// <summary>Either a default <see cref="SpanSplitEnumerator{T}"/> was used, or the enumerator has finished enumerating and there's no more work to do.</summary>
        None = 0,

        /// <summary>A single T separator was provided.</summary>
        SingleElement,

        /// <summary>A span of separators was provided, each of which should be treated independently.</summary>
        Any,

        /// <summary>The separator is a span of elements to be treated as a single sequence.</summary>
        Sequence,

        /// <summary>The separator is an empty sequence, such that no splits should be performed.</summary>
        EmptySequence,
    }

    internal ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _source;

        private readonly T _separator = default!;

        private readonly ReadOnlySpan<T> _separatorBuffer;

        private SpanSplitEnumeratorMode _splitMode;

        private int _startCurrent = 0;

        private int _endCurrent = 0;

        private int _startNext = 0;

        public readonly SpanSplitEnumerator<T> GetEnumerator() => this;

        public readonly ReadOnlySpan<T> Source => _source;

        public readonly ReadOnlySpan<T> Current => _source.Slice(_startCurrent, _endCurrent - _startCurrent);

        internal SpanSplitEnumerator(ReadOnlySpan<T> source, ReadOnlySpan<T> separators)
        {
            _source = source;
            if (typeof(T) == typeof(char) && separators.Length == 0)
            {
                _separatorBuffer = WhiteSpaceChars.AsSpan<T>();
                _splitMode = SpanSplitEnumeratorMode.Any;
            }

            _separatorBuffer = separators;
            _splitMode = SpanSplitEnumeratorMode.Any;
        }

        internal SpanSplitEnumerator(ReadOnlySpan<T> source, ReadOnlySpan<T> separator, bool treatAsSingleSeparator)
        {
            Debug.Assert(treatAsSingleSeparator, "Should only ever be called as true; exists to differentiate from separators overload");

            _source = source;
            _separatorBuffer = separator;
            _splitMode = separator.Length == 0
                ? SpanSplitEnumeratorMode.EmptySequence
                : SpanSplitEnumeratorMode.Sequence;
        }

        internal SpanSplitEnumerator(ReadOnlySpan<T> source, T separator)
        {
            _source = source;
            _separator = separator;
            _splitMode = SpanSplitEnumeratorMode.SingleElement;
        }

        public bool MoveNext()
        {
            // Search for the next separator index.
            int separatorIndex, separatorLength;
            switch (_splitMode)
            {
                case SpanSplitEnumeratorMode.None:
                    return false;

                case SpanSplitEnumeratorMode.SingleElement:
                    separatorIndex = _source.Slice(_startNext).IndexOf(_separator);
                    separatorLength = 1;
                    break;

                case SpanSplitEnumeratorMode.Any:
                    separatorIndex = _source.Slice(_startNext).IndexOfAny(_separatorBuffer);
                    separatorLength = 1;
                    break;

                case SpanSplitEnumeratorMode.Sequence:
                    separatorIndex = _source.Slice(_startNext).IndexOf(_separatorBuffer);
                    separatorLength = _separatorBuffer.Length;
                    break;

                case SpanSplitEnumeratorMode.EmptySequence:
                    separatorIndex = -1;
                    separatorLength = 1;
                    break;

                default:
                    throw new InvalidOperationException($"Invalid split mode: {_splitMode}");
            }

            _startCurrent = _startNext;
            if (separatorIndex >= 0)
            {
                _endCurrent = _startCurrent + separatorIndex;
                _startNext = _endCurrent + separatorLength;
            }
            else
            {
                _startNext = _endCurrent = _source.Length;

                // Set _splitMode to None so that subsequent MoveNext calls will return false.
                _splitMode = SpanSplitEnumeratorMode.None;
            }

            return true;
        }

        private const string WhiteSpaces = "\t\n\v\f\r\u0020\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";

        public static readonly T[]? WhiteSpaceChars;

        static SpanSplitEnumerator()
        {
            if (typeof(T) == typeof(char))
            {
                WhiteSpaceChars = WhiteSpaces.Cast<T>().ToArray();
            }
        }
    }
}
