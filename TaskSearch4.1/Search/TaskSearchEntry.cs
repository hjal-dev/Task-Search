using System;
using System.Collections.Generic;

namespace TaskSearch.Search
{
    internal sealed class TaskSearchEntry
    {
        private readonly string[] _fields = new string[TaskSearchFieldWeights.Count];

        internal string QuestId { get; }

        internal string DisplayName { get; }

        internal string NormalizedName
        {
            get
            {
                string name = _fields[(int)TaskSearchField.Name];

                if (name == null)
                {
                    return "";
                }

                return name;
            }
        }

        internal TaskSearchEntry(string questId, string displayName)
        {
            QuestId = questId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        internal void Add(TaskSearchField field, string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return;
            }

            string normalized = SearchTextNormalizer.Normalize(rawText);

            if (normalized.Length == 0)
            {
                return;
            }

            int index = (int)field;
            string existing = _fields[index];

            if (existing == null)
            {
                _fields[index] = normalized;
                return;
            }

            if (ContainsFragment(existing, normalized))
            {
                return;
            }

            _fields[index] = existing + " " + normalized;
        }

        internal void AddRange(TaskSearchField field, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (string value in values)
            {
                Add(field, value);
            }
        }

        private static bool ContainsFragment(string haystack, string fragment)
        {
            int index = haystack.IndexOf(fragment, StringComparison.Ordinal);

            while (index >= 0)
            {
                bool startsAtBoundary = index == 0 || haystack[index - 1] == ' ';
                int end = index + fragment.Length;
                bool endsAtBoundary = end == haystack.Length || haystack[end] == ' ';

                if (startsAtBoundary && endsAtBoundary)
                {
                    return true;
                }

                index = haystack.IndexOf(fragment, index + 1, StringComparison.Ordinal);
            }

            return false;
        }

        internal int Score(TaskSearchQuery query)
        {
            if (query == null || query.IsEmpty)
            {
                return 0;
            }

            int total = 0;
            int tokensFoundInName = 0;

            for (int t = 0; t < query.Tokens.Count; t++)
            {
                string token = query.Tokens[t];
                float best = 0f;

                for (int f = 0; f < _fields.Length; f++)
                {
                    string text = _fields[f];

                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    if (!SearchTextNormalizer.Contains(text, token, out bool atWordStart))
                    {
                        continue;
                    }

                    if (f == (int)TaskSearchField.Name)
                    {
                        tokensFoundInName++;
                    }

                    float weight = TaskSearchFieldWeights.Of((TaskSearchField)f);

                    if (!atWordStart)
                    {
                        weight *= TaskSearchFieldWeights.MidWordPenalty;
                    }

                    if (weight > best)
                    {
                        best = weight;
                    }
                }

                if (best <= 0f)
                {
                    return 0;
                }

                total += (int)Math.Round(best);
            }

            string name = NormalizedName;

            if (name.Length > 0)
            {
                if (string.Equals(name, query.Normalized, StringComparison.Ordinal))
                {
                    total += TaskSearchFieldWeights.ExactNameBonus;
                }
                else if (name.StartsWith(query.Normalized, StringComparison.Ordinal))
                {
                    total += TaskSearchFieldWeights.NamePrefixBonus;
                }
                else if (tokensFoundInName == query.Tokens.Count)
                {
                    total += TaskSearchFieldWeights.AllTokensInNameBonus;
                }
            }

            return total;
        }
    }
}
