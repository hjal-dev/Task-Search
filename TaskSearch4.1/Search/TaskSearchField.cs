namespace TaskSearch.Search
{
    internal enum TaskSearchField
    {
        Name = 0,
        Trader = 1,
        Location = 2,
        ObjectiveType = 3,
        ObjectiveText = 4,
        Item = 5,
        Description = 6,
        Prerequisite = 7,
        QuestId = 8
    }

    internal static class TaskSearchFieldWeights
    {
        internal const int Count = 9;

        private static readonly int[] Weights =
        {
            100,
            80,
            70,
            60,
            50,
            40,
            40,
            20,
            10
        };

        internal static int Of(TaskSearchField field)
        {
            int index = (int)field;
            return index >= 0 && index < Weights.Length ? Weights[index] : 0;
        }

        internal const float MidWordPenalty = 0.55f;

        internal const int ExactNameBonus = 1000;

        internal const int NamePrefixBonus = 250;

        internal const int AllTokensInNameBonus = 120;
    }
}
