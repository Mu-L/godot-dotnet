using System;
using System.Collections;
using System.Collections.Generic;

namespace Godot.BindingsGeneration;

internal enum BBCodeTokenType
{
    None,
    Text,
    StartTag,
    EndTag,
}

internal ref struct BBCodeParser
{
    private const int StackallocByteThreshold = 256;
    private const int StackallocCharThreshold = StackallocByteThreshold / 2;

    private readonly ReadOnlySpan<char> _buffer;

    public ReadOnlySpan<char> ValueSpan { readonly get; private set; }

    private int _consumed;

    private BBCodeTokenType _tokenType = BBCodeTokenType.None;

    public readonly BBCodeTokenType TokenType => _tokenType;

    public BBCodeParser(ReadOnlySpan<char> input)
    {
        _buffer = input;
    }

    private bool ConsumeValue(char value)
    {
        return value switch
        {
            '[' => ConsumeTag(),
            _ => ConsumeTextUntilNextTag(),
        };
    }

    private bool ConsumeTag()
    {
        int tagEndIndex = _buffer.Slice(_consumed).IndexOf(']');
        if (tagEndIndex == -1)
        {
            // Malformed tag, treat input as text.
            return ConsumeTextUntilNextTag();
        }

        ReadOnlySpan<char> tag = _buffer.Slice(_consumed, tagEndIndex + 1);
        if (tag.Length <= "[]".Length)
        {
            // Malformed tag, treat input as text.
            return ConsumeTextUntilNextTag();
        }

        int nextTagStartIndex = tag.Slice(1).IndexOf('[');
        if (nextTagStartIndex != -1)
        {
            // Found a start of another tag before the end of the current tag,
            // this is a malformed tag, treat input as text.
            return ConsumeTextUntilNextTag();
        }

        // Check if it's an end tag.
        if (tag[1] == '/')
        {
            if (tag.Length <= "[/]".Length)
            {
                // Malformed end tag, treat input as text.
                return ConsumeTextUntilNextTag();
            }

            return ConsumeEndTag(tag);
        }

        return ConsumeStartTag(tag);
    }

    private bool ConsumeStartTag(ReadOnlySpan<char> tag)
    {
        _tokenType = BBCodeTokenType.StartTag;
        ValueSpan = tag;
        _consumed += tag.Length;
        return true;
    }

    private bool ConsumeEndTag(ReadOnlySpan<char> tag)
    {
        _tokenType = BBCodeTokenType.EndTag;
        ValueSpan = tag;
        _consumed += tag.Length;
        return true;
    }

    private bool ConsumeTextUntilNextTag()
    {
        int nextTagIndex = _buffer.Slice(_consumed + 1).IndexOf('[');
        if (nextTagIndex == -1)
        {
            // No next tag found, consume the rest of the input as text.
            ValueSpan = _buffer.Slice(_consumed);
            _consumed = _buffer.Length;
            _tokenType = BBCodeTokenType.Text;
            return true;
        }

        // We skipped the current character, so we need to add 1 to the index to get the correct length.
        ValueSpan = _buffer.Slice(_consumed, nextTagIndex + 1);
        _consumed += nextTagIndex + 1;
        _tokenType = BBCodeTokenType.Text;
        return true;
    }

    private bool ConsumeTextUntilMatchingEndTag(ReadOnlySpan<char> tagName)
    {
        int endTagLength = tagName.Length + "[/]".Length;
        Span<char> endTag = endTagLength <= StackallocCharThreshold
            ? stackalloc char[StackallocCharThreshold]
            : new char[endTagLength];

        endTag = endTag.Slice(0, endTagLength);

        int consumed = 0;
        consumed = Append("[/", endTag, consumed);
        consumed = Append(tagName, endTag, consumed);
        consumed = Append("]", endTag, consumed);

        int matchingEndTagIndex = _buffer.Slice(_consumed).IndexOf(endTag);
        if (matchingEndTagIndex == -1)
        {
            // No matching end tag found, consume the rest of the input as text.
            ValueSpan = _buffer.Slice(_consumed);
            _consumed = _buffer.Length;
            _tokenType = BBCodeTokenType.Text;
            return true;
        }

        ValueSpan = _buffer.Slice(_consumed, matchingEndTagIndex);
        _consumed += matchingEndTagIndex;
        _tokenType = BBCodeTokenType.Text;
        return true;

        static int Append(ReadOnlySpan<char> source, Span<char> destination, int offset)
        {
            source.CopyTo(destination.Slice(offset));
            offset += source.Length;
            return offset;
        }
    }

    /// <summary>
    /// Read the next token from the input.
    /// </summary>
    public bool Read()
    {
        if (_consumed >= _buffer.Length)
        {
            // Reached the end of the input.
            return false;
        }

        char nextCharacter = _buffer[_consumed];
        return ConsumeValue(nextCharacter);
    }

    /// <summary>
    /// Read until the corresponding end tag of the current start tag is found,
    /// and consider everything in between as text.
    /// This is useful for tags like [code] that should not be parsed for nested tags.
    /// </summary>
    public bool ReadToEndTag()
    {
        if (_tokenType != BBCodeTokenType.StartTag)
        {
            throw new InvalidOperationException("Current token is not a start tag.");
        }

        var startTag = GetStartTag();
        return ConsumeTextUntilMatchingEndTag(startTag.TagName);
    }

    /// <summary>
    /// Get the current token as a start tag.
    /// </summary>
    /// <exception cref="InvalidOperationException">Current token is not a start tag.</exception>
    /// <returns>The current start tag.</returns>
    public readonly BBCodeStartTag GetStartTag()
    {
        if (_tokenType != BBCodeTokenType.StartTag)
        {
            throw new InvalidOperationException("Current token is not a start tag.");
        }

        return new BBCodeStartTag(ValueSpan);
    }

    /// <summary>
    /// Get the current token as an end tag.
    /// </summary>
    /// <exception cref="InvalidOperationException">Current token is not an end tag.</exception>
    /// <returns>The current end tag.</returns>
    public readonly BBCodeEndTag GetEndTag()
    {
        if (_tokenType != BBCodeTokenType.EndTag)
        {
            throw new InvalidOperationException($"Current token is not an end tag. It's {_tokenType} ({ValueSpan}).");
        }

        return new BBCodeEndTag(ValueSpan);
    }
}

internal ref struct BBCodeStartTag
{
    private readonly ReadOnlySpan<char> _buffer;

    public readonly ReadOnlySpan<char> TagName { get; }

    private readonly ReadOnlySpan<char> _attributesBuffer;

    public BBCodeStartTag(ReadOnlySpan<char> tag)
    {
        // throw new InvalidOperationException($"BBCodeStartTag is {tag}");

        if (tag.Length == 0)
        {
            throw new ArgumentException("Tag cannot be empty.", nameof(tag));
        }
        if (tag.Length <= "[]".Length)
        {
            throw new ArgumentException("Tag is too short to be valid.", nameof(tag));
        }

        _buffer = tag;

        // Trim the surrounding brackets.
        tag = tag[1..^1];

        if (tag[0] == '/')
        {
            throw new ArgumentException("Tag cannot be an end tag.", nameof(tag));
        }

        int tagNameEndIndex = tag.IndexOfAny("= ");
        if (tagNameEndIndex == -1)
        {
            tagNameEndIndex = tag.Length;
        }

        TagName = tag.Slice(0, tagNameEndIndex);
        _attributesBuffer = tag.Slice(tagNameEndIndex).Trim();
    }

    public bool HasAttributes() => !_attributesBuffer.IsEmpty;

    public BBCodeStartTagAttributeEnumerator EnumerateAttributes()
    {
        return new BBCodeStartTagAttributeEnumerator(_attributesBuffer);
    }
}

internal ref struct BBCodeEndTag
{
    private readonly ReadOnlySpan<char> _buffer;

    public readonly ReadOnlySpan<char> TagName { get; }

    public BBCodeEndTag(ReadOnlySpan<char> tag)
    {
        if (tag.Length == 0)
        {
            throw new ArgumentException("Tag cannot be empty.", nameof(tag));
        }
        if (tag.Length <= "[/]".Length)
        {
            throw new ArgumentException("Tag is too short to be valid.", nameof(tag));
        }

        _buffer = tag;

        // Trim the surrounding brackets.
        tag = tag[1..^1];

        if (tag[0] != '/')
        {
            throw new ArgumentException("Tag must be an end tag.", nameof(tag));
        }

        TagName = tag[1..];
    }
}

internal ref struct BBCodeTagAttribute
{
    public ReadOnlySpan<char> Name { get; }

    public ReadOnlySpan<char> Value { get; }

    public BBCodeTagAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
    {
        Name = name;
        Value = value;
    }
}

internal ref struct BBCodeStartTagAttributeEnumerator : IEnumerator<BBCodeTagAttribute>
{
    private readonly ReadOnlySpan<char> _buffer;

    private int _consumed;

    public BBCodeTagAttribute Current { get; private set; }

    readonly object IEnumerator.Current => throw new NotSupportedException();

    public BBCodeStartTagAttributeEnumerator(ReadOnlySpan<char> tag)
    {
        _buffer = tag;
    }

    public ReadOnlySpan<char> First()
    {
        // Special case for single unnamed attribute.

        Reset();
        if (!MoveNext())
        {
            throw new InvalidOperationException("Tag does not contain any attributes.");
        }

        if (!Current.Name.IsEmpty)
        {
            throw new InvalidOperationException("Tag does not contain a single unnamed attribute.");
        }

        return Current.Value;
    }

    public bool MoveNext()
    {
        if (_consumed >= _buffer.Length)
        {
            // Reached the end of the attributes.
            return false;
        }

        ReadOnlySpan<char> remainingBuffer = _buffer.Slice(_consumed);

        int nextAttributeIndex = remainingBuffer.IndexOf(' ');
        if (nextAttributeIndex == -1)
        {
            nextAttributeIndex = remainingBuffer.Length;
        }

        ReadOnlySpan<char> attribute = remainingBuffer.Slice(0, nextAttributeIndex);
        int nameSeparator = attribute.IndexOf('=');
        if (nameSeparator == -1)
        {
            // This attribute does not have a name,
            // treat the entire attribute as the value.
            Current = new BBCodeTagAttribute(ReadOnlySpan<char>.Empty, attribute);
            _consumed += nextAttributeIndex + 1;
            return true;
        }

        {
            ReadOnlySpan<char> attributeName = attribute.Slice(0, nameSeparator).Trim();
            ReadOnlySpan<char> attributeValue = attribute.Slice(nameSeparator + 1).Trim();
            Current = new BBCodeTagAttribute(attributeName, attributeValue);
            _consumed += nextAttributeIndex + 1;
            return true;
        }
    }

    public void Reset()
    {
        _consumed = 0;
    }

    public void Dispose() { }
}
