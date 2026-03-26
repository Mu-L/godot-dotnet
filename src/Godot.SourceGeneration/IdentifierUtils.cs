using System;
using System.Globalization;
using System.Text;
using Godot.Common.CodeAnalysis;

namespace Godot.SourceGeneration;

internal static class IdentifierUtils
{
    /// <summary>
    /// Sanitizes a name so it can be used as a C# namespace.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <param name="escapeSegments">
    /// When <see langword="true"/>, each segment is prefixed with <c>@</c> to ensure it does
    /// not conflict with a C# keyword (e.g. <c>My.namespace</c> becomes <c>@My.@namespace</c>).
    /// </param>
    public static string SanitizeName(string name, bool escapeSegments = false)
    {
        return SanitizeName(name.AsSpan(), escapeSegments);
    }

    /// <summary>
    /// Sanitizes a name so it can be used as a C# namespace.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <param name="escapeSegments">
    /// When <see langword="true"/>, each segment is prefixed with <c>@</c> to ensure it does
    /// not conflict with a C# keyword (e.g. <c>My.namespace</c> becomes <c>@My.@namespace</c>).
    /// </param>
    public static string SanitizeName(ReadOnlySpan<char> name, bool escapeSegments = false)
    {
        // The output must match `IdentifierUtils::sanitize_name` on the C++ side.
        // See: `modules/dotnet/utils/identifier_utils.h`.

        if (name.IsWhiteSpace())
        {
            return escapeSegments ? "@UnnamedProject" : "UnnamedProject";
        }

        var sb = new StringBuilder(name.Length);

        foreach (var segment in new SpanSplitEnumerator<char>(name, '.'))
        {
            int prevLength = sb.Length;
            if (sb.Length > 0)
            {
                sb.Append('.');
            }
            if (!SanitizeSegment(segment, sb, escapeSegments))
            {
                sb.Length = prevLength;
            }
        }

        if (sb.Length == 0)
        {
            return escapeSegments ? "@UnnamedProject" : "UnnamedProject";
        }

        return sb.ToString();
    }

    private static bool SanitizeSegment(ReadOnlySpan<char> segment, StringBuilder sb, bool escape)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        if (escape)
        {
            sb.Append('@');
        }

        // Replace invalid characters with underscores and collapse consecutive underscores.
        bool pendingUnderscore = false;
        bool hasContent = false;
        foreach (char c in segment)
        {
            char ch = IsValidIdentifierPartCharacter(c) ? c : '_';
            if (ch == '_')
            {
                if (hasContent)
                {
                    // If we've already found non-underscore characters before,
                    // this underscore may be trailing or it may be separating
                    // valid characters. We defer appending it until we find out
                    // which case it is.
                    pendingUnderscore = true;
                }
                else
                {
                    // If we haven't found any non-underscore characters yet,
                    // this is a leading underscore and we can just skip it.
                }
            }
            else
            {
                if (!hasContent)
                {
                    // First non-underscore character we find in this segment.
                    // If it's not valid as an identifier start, prefix with an underscore.
                    if (!IsValidIdentifierStartCharacter(ch))
                    {
                        sb.Append('_');
                    }
                    hasContent = true;
                }
                else if (pendingUnderscore)
                {
                    // Found a non-underscore character after one or more underscores,
                    // append a single underscore to collapse them.
                    sb.Append('_');
                    pendingUnderscore = false;
                }
                sb.Append(ch);
            }
        }

        return hasContent;
    }

    private static bool IsValidIdentifierStartCharacter(char c)
    {
        // Matches the 'Identifier_Start_Character' fragment in the C# specification.
        // See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#643-identifiers

        if (c == '_')
        {
            // Underscore_Character
            return true;
        }

        return CharUnicodeInfo.GetUnicodeCategory(c) is
            // Letter_Character "L" or "Nl"
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber;
    }

    private static bool IsValidIdentifierPartCharacter(char c)
    {
        // Matches the 'Identifier_Part_Character' fragment in the C# specification.
        // See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#643-identifiers

        if (c == '_')
        {
            // Underscore_Character
            return true;
        }

        return CharUnicodeInfo.GetUnicodeCategory(c) is
            // Letter_Character "L" or "Nl"
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber or
            // Decimal_Digit_Character "Nd"
            UnicodeCategory.DecimalDigitNumber or
            // Connecting_Character "Pc"
            UnicodeCategory.ConnectorPunctuation or
            // Combining_Character "Mn" or "Mc"
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or
            // Formatting_Character "Cf"
            UnicodeCategory.Format;
    }
}
