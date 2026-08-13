using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TaskSearch.Search
{
    internal static class SearchTextNormalizer
    {
        private static readonly Regex RichTextTag =
            new Regex("<[^<>]{1,64}>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string working = text;

            if (working.IndexOf('<') >= 0)
            {
                working = RichTextTag.Replace(working, " ");
            }

            working = working.ToLowerInvariant();

            try
            {
                working = working.Normalize(NormalizationForm.FormD);
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            StringBuilder builder = new StringBuilder(working.Length);
            bool pendingSpace = false;

            foreach (char c in working)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (IsApostrophe(c))
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    if (pendingSpace && builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    pendingSpace = false;
                    builder.Append(c);
                }
                else
                {
                    pendingSpace = true;
                }
            }

            return builder.ToString();
        }

        private static bool IsApostrophe(char c)
        {
            return c == '\'' || c == '’' || c == 'ʼ' || c == '`' || c == '´';
        }

        internal static bool Contains(string normalizedHaystack, string normalizedNeedle, out bool atWordStart)
        {
            atWordStart = false;

            if (string.IsNullOrEmpty(normalizedHaystack) || string.IsNullOrEmpty(normalizedNeedle))
            {
                return false;
            }

            int index = normalizedHaystack.IndexOf(normalizedNeedle, StringComparison.Ordinal);

            if (index < 0)
            {
                return false;
            }

            while (index >= 0)
            {
                if (index == 0 || normalizedHaystack[index - 1] == ' ')
                {
                    atWordStart = true;
                    return true;
                }

                index = normalizedHaystack.IndexOf(
                    normalizedNeedle, index + 1, StringComparison.Ordinal);
            }

            return true;
        }
    }
}
