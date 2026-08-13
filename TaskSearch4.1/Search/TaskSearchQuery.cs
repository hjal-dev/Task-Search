using System;
using System.Collections.Generic;

namespace TaskSearch.Search
{
    internal sealed class TaskSearchQuery
    {
        private static readonly char[] TokenSeparator = { ' ' };

        internal static readonly TaskSearchQuery Empty = new TaskSearchQuery(string.Empty, Array.Empty<string>());

        internal string Normalized { get; }

        internal IReadOnlyList<string> Tokens { get; }

        internal bool IsEmpty
        {
            get { return Tokens.Count == 0; }
        }

        private TaskSearchQuery(string normalized, IReadOnlyList<string> tokens)
        {
            Normalized = normalized;
            Tokens = tokens;
        }

        internal static TaskSearchQuery Parse(string rawText)
        {
            string normalized = SearchTextNormalizer.Normalize(rawText);

            if (normalized.Length == 0)
            {
                return Empty;
            }

            string[] parts = normalized.Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return Empty;
            }

            List<string> tokens = new List<string>(parts.Length);

            foreach (string part in parts)
            {
                if (!tokens.Contains(part))
                {
                    tokens.Add(part);
                }
            }

            return new TaskSearchQuery(normalized, tokens);
        }
    }
}
