using System;
using System.Collections.Generic;
using System.Diagnostics;
using EFT.Quests;
using TaskSearch.Eft;
using TaskSearch.Logging;

namespace TaskSearch.Search
{
    internal sealed class TaskSearchIndex
    {
        private readonly Dictionary<string, TaskSearchEntry> _entries =
            new Dictionary<string, TaskSearchEntry>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> _questNames =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private int _signature;

        internal int Count
        {
            get { return _entries.Count; }
        }

        internal void Invalidate()
        {
            _entries.Clear();
            _signature = 0;
        }

        internal void Rebuild(IEnumerable<Quest> quests)
        {
            if (quests == null)
            {
                _entries.Clear();
                _questNames.Clear();
                _signature = 0;
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            _questNames.Clear();

            List<Quest> displayable = new List<Quest>();
            int total = 0;
            int signature = 17;

            foreach (Quest quest in quests)
            {
                if (quest == null)
                {
                    continue;
                }

                total++;
                CacheQuestName(quest);

                unchecked
                {
                    signature = (signature * 31) + (SafeId(quest)?.GetHashCode() ?? 0);
                    signature = (signature * 31) + SafeStatusCode(quest);
                }

                if (IsDisplayable(quest))
                {
                    displayable.Add(quest);
                }
            }

            unchecked
            {
                foreach (string name in _questNames.Values)
                {
                    signature = (signature * 31) + (name?.GetHashCode() ?? 0);
                }
            }

            if (signature == _signature && _entries.Count > 0)
            {
                stopwatch.Stop();
                return;
            }

            _entries.Clear();
            _signature = signature;

            int failed = 0;

            foreach (Quest quest in displayable)
            {
                if (!TryIndex(quest))
                {
                    failed++;
                }
            }

            stopwatch.Stop();
        }

        private static bool IsDisplayable(Quest quest)
        {
            try
            {
                EQuestStatus status = quest.QuestStatus;

                return status == EQuestStatus.Started
                       || status == EQuestStatus.AvailableForFinish
                       || status == EQuestStatus.MarkedAsFailed;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static int SafeStatusCode(Quest quest)
        {
            try
            {
                return (int)quest.QuestStatus;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        internal int IndexMissing(IEnumerable<Quest> quests)
        {
            if (quests == null)
            {
                return 0;
            }

            int added = 0;

            foreach (Quest quest in quests)
            {
                string id = SafeId(quest);

                if (string.IsNullOrEmpty(id) || _entries.ContainsKey(id))
                {
                    continue;
                }

                CacheQuestName(quest);

                if (TryIndex(quest))
                {
                    added++;
                }
            }

            if (added > 0)
            {
            }

            return added;
        }

        private bool TryIndex(Quest quest)
        {
            string id = SafeId(quest);

            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            try
            {
                TaskSearchEntry entry = TaskSearchEntryBuilder.Build(quest, ResolveQuestName);

                if (entry == null)
                {
                    return false;
                }

                _entries[id] = entry;
                return true;
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "index:" + id,
                    $"Skipping quest '{id}' - it could not be indexed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private void CacheQuestName(Quest quest)
        {
            try
            {
                string id = quest.Id;

                if (string.IsNullOrEmpty(id))
                {
                    return;
                }

                string name = quest.Template?.Name;

                if (!string.IsNullOrEmpty(name))
                {
                    _questNames[id] = name;
                }
            }
            catch (Exception)
            {
            }
        }

        private string ResolveQuestName(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            if (_questNames.TryGetValue(questId, out string cached))
            {
                return cached;
            }

            if (EftLocalization.TryLocalize(questId + " name", out string localized))
            {
                _questNames[questId] = localized;
                return localized;
            }

            return null;
        }

        internal TaskSearchEntry GetEntry(Quest quest)
        {
            string id = SafeId(quest);
            return string.IsNullOrEmpty(id) ? null : GetEntry(id);
        }

        internal TaskSearchEntry GetEntry(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            return _entries.TryGetValue(questId, out TaskSearchEntry entry) ? entry : null;
        }

        internal List<TaskSearchResult> Search(IReadOnlyList<Quest> candidates, TaskSearchQuery query)
        {
            List<TaskSearchResult> results = new List<TaskSearchResult>();

            if (candidates == null || query == null || query.IsEmpty)
            {
                return results;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                Quest quest = candidates[i];
                TaskSearchEntry entry = GetEntry(quest);

                if (entry == null)
                {
                    continue;
                }

                int score = entry.Score(query);

                if (score > 0)
                {
                    results.Add(new TaskSearchResult(quest, entry, score, i));
                }
            }

            results.Sort(CompareResults);
            return results;
        }

        private static int CompareResults(TaskSearchResult left, TaskSearchResult right)
        {
            int byScore = right.Score.CompareTo(left.Score);
            return byScore != 0 ? byScore : left.OriginalOrder.CompareTo(right.OriginalOrder);
        }

        private static string SafeId(Quest quest)
        {
            if (quest == null)
            {
                return null;
            }

            try
            {
                return quest.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
