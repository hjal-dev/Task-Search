using EFT.Quests;

namespace TaskSearch.Search
{
    internal sealed class TaskSearchResult
    {
        internal Quest Quest { get; }

        internal TaskSearchEntry Entry { get; }

        internal int Score { get; }

        internal int OriginalOrder { get; }

        internal TaskSearchResult(Quest quest, TaskSearchEntry entry, int score, int originalOrder)
        {
            Quest = quest;
            Entry = entry;
            Score = score;
            OriginalOrder = originalOrder;
        }
    }
}
