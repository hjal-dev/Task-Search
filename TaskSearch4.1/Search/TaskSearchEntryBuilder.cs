using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using EFT.Quests;
using TaskSearch.Eft;
using TaskSearch.Logging;

namespace TaskSearch.Search
{
    internal static class TaskSearchEntryBuilder
    {
        private const int MaxConditionDepth = 6;

        internal static TaskSearchEntry Build(Quest quest, Func<string, string> resolveQuestName)
        {
            if (quest == null)
            {
                return null;
            }

            string questId = SafeGet(() => quest.Id, null);

            if (string.IsNullOrEmpty(questId))
            {
                return null;
            }

            QuestTemplate template = SafeGet(() => quest.Template, null);

            string displayName = template != null
                ? SafeGet(() => template.Name, null)
                : null;

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = questId;
            }

            TaskSearchEntry entry = new TaskSearchEntry(questId, displayName);

            if (TaskSearchConfig.SearchQuestNames.Value)
            {
                entry.Add(TaskSearchField.Name, displayName);
            }

            if (TaskSearchConfig.SearchQuestIds.Value)
            {
                entry.Add(TaskSearchField.QuestId, questId);
            }

            if (template == null)
            {
                TaskSearchLog.WarnOnce(
                    "notemplate:" + questId,
                    $"Quest '{questId}' has no template; indexing name only.");
                return entry;
            }

            AddDescription(entry, template, questId);
            AddTrader(entry, template, questId);
            AddQuestLocation(entry, template, questId);
            AddQuestType(entry, template, questId);
            AddConditions(entry, template, questId, resolveQuestName);

            return entry;
        }

        private static void AddDescription(TaskSearchEntry entry, QuestTemplate template, string questId)
        {
            if (!TaskSearchConfig.SearchDescriptions.Value)
            {
                return;
            }

            try
            {
                entry.Add(TaskSearchField.Description, template.Description);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "desc:" + questId, $"Quest '{questId}' description unavailable: {ex.Message}");
            }
        }

        private static void AddTrader(TaskSearchEntry entry, QuestTemplate template, string questId)
        {
            if (!TaskSearchConfig.SearchTraders.Value)
            {
                return;
            }

            try
            {
                List<string> names = new List<string>(3);
                EftTraders.CollectNames(template.TraderId, names);
                entry.AddRange(TaskSearchField.Trader, names);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "trader:" + questId, $"Quest '{questId}' trader unavailable: {ex.Message}");
            }
        }

        private static void AddQuestLocation(TaskSearchEntry entry, QuestTemplate template, string questId)
        {
            if (!TaskSearchConfig.SearchLocations.Value)
            {
                return;
            }

            try
            {
                string locationId = template.LocationId;

                if (string.IsNullOrEmpty(locationId))
                {
                    return;
                }

                if (EftLocalization.TryGetLocationName(locationId, out string name))
                {
                    entry.Add(TaskSearchField.Location, name);
                }
                else if (!string.Equals(locationId, "any", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Add(TaskSearchField.Location, locationId);
                }
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "loc:" + questId, $"Quest '{questId}' location unavailable: {ex.Message}");
            }
        }

        private static void AddQuestType(TaskSearchEntry entry, QuestTemplate template, string questId)
        {
            if (!TaskSearchConfig.SearchObjectives.Value)
            {
                return;
            }

            try
            {
                entry.Add(TaskSearchField.ObjectiveType, SplitCamelCase(template.QuestType.ToString()));
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "type:" + questId, $"Quest '{questId}' type unavailable: {ex.Message}");
            }
        }

        private static void AddConditions(
            TaskSearchEntry entry, QuestTemplate template, string questId, Func<string, string> resolveQuestName)
        {
            ConditionsDict conditions;

            try
            {
                conditions = template.Conditions;
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "conds:" + questId, $"Quest '{questId}' conditions unavailable: {ex.Message}");
                return;
            }

            if (conditions == null)
            {
                return;
            }

            HashSet<Condition> visited = new HashSet<Condition>();

            foreach (var bucket in conditions)
            {
                if (bucket.Value == null)
                {
                    continue;
                }

                foreach (Condition condition in Enumerate(bucket.Value))
                {
                    AddCondition(entry, condition, questId, resolveQuestName, visited, 0);
                }
            }
        }

        private static void AddCondition(
            TaskSearchEntry entry,
            Condition condition,
            string questId,
            Func<string, string> resolveQuestName,
            HashSet<Condition> visited,
            int depth)
        {
            if (condition == null || depth > MaxConditionDepth || !visited.Add(condition))
            {
                return;
            }

            Type conditionType = condition.GetType();

            AddObjectiveType(entry, conditionType);
            AddObjectiveText(entry, condition, questId, conditionType);

            AddConditionTargets(entry, condition, questId, conditionType, resolveQuestName);

            foreach (Condition child in GetChildConditions(condition, questId))
            {
                AddCondition(entry, child, questId, resolveQuestName, visited, depth + 1);
            }
        }

        private static void AddObjectiveType(TaskSearchEntry entry, Type conditionType)
        {
            if (!TaskSearchConfig.SearchObjectives.Value)
            {
                return;
            }

            string name = conditionType.Name;

            if (name.StartsWith("Condition", StringComparison.Ordinal))
            {
                name = name.Substring("Condition".Length);
            }

            entry.Add(TaskSearchField.ObjectiveType, SplitCamelCase(name));
        }

        private static void AddObjectiveText(
            TaskSearchEntry entry, Condition condition, string questId, Type conditionType)
        {
            if (!TaskSearchConfig.SearchObjectives.Value)
            {
                return;
            }

            try
            {
                entry.Add(TaskSearchField.ObjectiveText, condition.FormattedDescription);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "objtext:" + questId + ":" + conditionType.Name,
                    $"Quest '{questId}': objective text for {conditionType.Name} unavailable " +
                    $"({ex.GetType().Name}). Other fields still indexed.");
            }
        }

        private static void AddConditionTargets(
            TaskSearchEntry entry,
            Condition condition,
            string questId,
            Type conditionType,
            Func<string, string> resolveQuestName)
        {
            List<string> targets = new List<string>(4);

            try
            {
                ConditionTargetReader.Collect(condition, targets);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "targets:" + questId + ":" + conditionType.Name,
                    $"Quest '{questId}': could not read targets of {conditionType.Name}: {ex.Message}");
                return;
            }

            if (targets.Count == 0)
            {
                return;
            }

            if (condition is ConditionQuest)
            {
                if (!TaskSearchConfig.SearchRequirements.Value)
                {
                    return;
                }

                foreach (string target in targets)
                {
                    string name = resolveQuestName?.Invoke(target);
                    entry.Add(TaskSearchField.Prerequisite, string.IsNullOrEmpty(name) ? target : name);
                }

                return;
            }

            bool isLocationCondition = condition is ConditionLocation
                                       || condition is ConditionTransitionLocation
                                       || condition is ConditionExitName
                                       || condition is ConditionZone
                                       || condition is ConditionInZone
                                       || condition is ConditionLaunchFlare
                                       || condition is ConditionVisitPlace;

            bool isItemCondition = condition is ConditionItem
                                   || condition is ConditionExamineItem
                                   || condition is ConditionUseItem
                                   || condition is ConditionSellItemToTrader
                                   || condition is ConditionLeaveItemAtLocation
                                   || condition is ConditionPlaceBeacon;

            foreach (string target in targets)
            {
                bool handled = false;

                if (isLocationCondition && TaskSearchConfig.SearchLocations.Value)
                {
                    handled |= AddLocationTarget(entry, target);
                }

                if (isItemCondition && TaskSearchConfig.SearchItems.Value)
                {
                    handled |= AddItemTarget(entry, target);
                }

                if (handled || (isLocationCondition && isItemCondition))
                {
                    continue;
                }

                if (!isLocationCondition && !isItemCondition)
                {
                    if (TaskSearchConfig.SearchItems.Value && AddItemTarget(entry, target))
                    {
                        continue;
                    }

                    if (!TaskSearchConfig.SearchObjectives.Value)
                    {
                        continue;
                    }

                    if (EftLocalization.TryLocalize(target, out string localized))
                    {
                        entry.Add(TaskSearchField.ObjectiveType, localized);
                    }
                    else
                    {
                        entry.Add(TaskSearchField.ObjectiveType, SplitCamelCase(target));
                    }

                    continue;
                }

                if (isLocationCondition)
                {
                    if (TaskSearchConfig.SearchLocations.Value)
                    {
                        entry.Add(TaskSearchField.Location, SplitIdentifier(target));
                    }
                }
                else if (TaskSearchConfig.SearchItems.Value)
                {
                    entry.Add(TaskSearchField.Item, target);
                }
            }
        }

        private static bool AddLocationTarget(TaskSearchEntry entry, string target)
        {
            if (EftLocalization.TryGetLocationName(target, out string mapName))
            {
                entry.Add(TaskSearchField.Location, mapName);
                return true;
            }

            if (EftLocalization.TryLocalize(target, out string zoneName))
            {
                entry.Add(TaskSearchField.Location, zoneName);
                return true;
            }

            return false;
        }

        private static bool AddItemTarget(TaskSearchEntry entry, string target)
        {
            List<string> names = new List<string>(2);
            EftLocalization.CollectItemNames(target, names);

            if (names.Count == 0)
            {
                return false;
            }

            entry.AddRange(TaskSearchField.Item, names);
            return true;
        }

        private static IEnumerable<Condition> GetChildConditions(Condition condition, string questId)
        {
            List<Condition> children = new List<Condition>();

            try
            {
                if (condition is ConditionCounterCreator counter)
                {
                    children.AddRange(Enumerate(counter.Conditions));
                }

                children.AddRange(Enumerate(condition.ChildConditions));

                if (condition.VisibilityConditions != null)
                {
                    foreach (Condition visibility in condition.VisibilityConditions)
                    {
                        if (visibility != null)
                        {
                            children.Add(visibility);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce(
                    "children:" + questId,
                    $"Quest '{questId}': could not walk nested conditions: {ex.Message}");
            }

            return children;
        }

        private static IEnumerable<Condition> Enumerate(object collection)
        {
            if (collection is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is Condition condition)
                    {
                        yield return condition;
                    }
                }
            }
        }

        internal static string SplitCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length + 8);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static string SplitIdentifier(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : SplitCamelCase(value.Replace('_', ' ').Replace('-', ' '));
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter();
            }
            catch (Exception)
            {
                return fallback;
            }
        }
    }
}
