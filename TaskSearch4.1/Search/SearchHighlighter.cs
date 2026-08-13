using System;
using System.Collections.Generic;
using System.Text;

namespace TaskSearch.Search
{
    internal static class SearchHighlighter
    {
        private const string OpenTag = "<mark=#FFFFFF33>";
        private const string CloseTag = "</mark>";

        internal static string Apply(string source, TaskSearchQuery query, bool sourceHasMarkup)
        {
            if (string.IsNullOrEmpty(source) || query == null || query.IsEmpty)
            {
                return source;
            }

            try
            {
                string working = sourceHasMarkup ? source : source.Replace("<", "&lt;");

                List<int> starts = new List<int>();
                List<int> ends = new List<int>();

                for (int t = 0; t < query.Tokens.Count; t++)
                {
                    CollectMatches(working, query.Tokens[t], starts, ends);
                }

                if (starts.Count == 0)
                {
                    return source;
                }

                return Build(working, Merge(starts, ends));
            }
            catch (Exception)
            {
                return source;
            }
        }

        private static void CollectMatches(string source, string token, List<int> starts, List<int> ends)
        {
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            int from = 0;

            while (from < source.Length)
            {
                int at = source.IndexOf(token, from, StringComparison.OrdinalIgnoreCase);

                if (at < 0)
                {
                    return;
                }

                if (!InsideTag(source, at))
                {
                    starts.Add(at);
                    ends.Add(at + token.Length);
                }

                from = at + 1;
            }
        }

        private static bool InsideTag(string source, int index)
        {
            for (int i = index; i >= 0; i--)
            {
                if (source[i] == '>')
                {
                    return false;
                }

                if (source[i] == '<')
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int[]> Merge(List<int> starts, List<int> ends)
        {
            List<int[]> spans = new List<int[]>();

            for (int i = 0; i < starts.Count; i++)
            {
                spans.Add(new int[] { starts[i], ends[i] });
            }

            spans.Sort(CompareSpans);

            List<int[]> merged = new List<int[]>();

            for (int i = 0; i < spans.Count; i++)
            {
                if (merged.Count > 0 && spans[i][0] <= merged[merged.Count - 1][1])
                {
                    if (spans[i][1] > merged[merged.Count - 1][1])
                    {
                        merged[merged.Count - 1][1] = spans[i][1];
                    }

                    continue;
                }

                merged.Add(spans[i]);
            }

            return merged;
        }

        private static int CompareSpans(int[] left, int[] right)
        {
            if (left[0] != right[0])
            {
                return left[0].CompareTo(right[0]);
            }

            return left[1].CompareTo(right[1]);
        }

        private static string Build(string source, List<int[]> spans)
        {
            StringBuilder builder = new StringBuilder(source.Length + (spans.Count * 24));
            int cursor = 0;

            for (int i = 0; i < spans.Count; i++)
            {
                builder.Append(source, cursor, spans[i][0] - cursor);
                builder.Append(OpenTag);
                builder.Append(source, spans[i][0], spans[i][1] - spans[i][0]);
                builder.Append(CloseTag);
                cursor = spans[i][1];
            }

            builder.Append(source, cursor, source.Length - cursor);
            return builder.ToString();
        }
    }
}
