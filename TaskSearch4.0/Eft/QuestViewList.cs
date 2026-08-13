using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EFT.UI;
using TaskSearch.Logging;

namespace TaskSearch.Eft
{
    internal sealed class QuestViewList
    {
        private static FieldInfo _viewListField;
        private static bool _viewListFieldResolved;

        private readonly object _instance;
        private readonly MethodInfo _filterBy;
        private readonly MethodInfo _updateOrder;
        private readonly PropertyInfo _keys;

        private QuestViewList(object instance, MethodInfo filterBy, MethodInfo updateOrder, PropertyInfo keys)
        {
            _instance = instance;
            _filterBy = filterBy;
            _updateOrder = updateOrder;
            _keys = keys;
        }

        internal static QuestViewList FromTasksPanel(TasksPanel panel)
        {
            if (panel == null)
            {
                return null;
            }

            try
            {
                FieldInfo field = ResolveViewListField();
                object instance = field?.GetValue(panel);

                if (instance == null)
                {
                    return null;
                }

                Type type = instance.GetType();

                MethodInfo filterBy = FindMethod(type, "FilterBy", 1);
                MethodInfo updateOrder = FindMethod(type, "UpdateOrder", 1);
                PropertyInfo keys = type.GetProperty("Keys", BindingFlags.Public | BindingFlags.Instance);

                if (filterBy == null || updateOrder == null || keys == null)
                {
                    TaskSearchLog.WarnOnce(
                        "viewlist-api",
                        $"The quest view list ({type.Name}) is missing FilterBy/UpdateOrder/Keys. " +
                        "Filtering is disabled for this game build.");
                    return null;
                }

                return new QuestViewList(instance, filterBy, updateOrder, keys);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("viewlist-read", "Could not read the quest view list: " + ex.Message);
                return null;
            }
        }

        private static FieldInfo ResolveViewListField()
        {
            if (_viewListFieldResolved)
            {
                return _viewListField;
            }

            _viewListFieldResolved = true;

            FieldInfo[] fields = typeof(TasksPanel)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                Type type = field.FieldType;

                if (!type.IsGenericType)
                {
                    continue;
                }

                Type[] args = type.GetGenericArguments();

                if (args.Length != 2 || args[0] != typeof(QuestClass) || args[1] != typeof(NotesTask))
                {
                    continue;
                }

                if (FindMethod(type, "FilterBy", 1) == null)
                {
                    continue;
                }

                _viewListField = field;
                return _viewListField;
            }

            TaskSearchLog.Warn(
                "Could not find the quest view list field on TasksPanel. Search filtering is disabled.");
            return null;
        }

        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                if (method.Name == name
                    && !method.IsGenericMethodDefinition
                    && method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }

            return null;
        }

        internal List<QuestClass> GetQuests()
        {
            try
            {
                IEnumerable raw = _keys.GetValue(_instance, null) as IEnumerable;

                if (raw != null)
                {
                    List<QuestClass> quests = new List<QuestClass>();

                    foreach (object item in raw)
                    {
                        QuestClass quest = item as QuestClass;

                        if (quest != null)
                        {
                            quests.Add(quest);
                        }
                    }

                    return quests;
                }
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("viewlist-keys", "Could not enumerate quest views: " + ex.Message);
            }

            return new List<QuestClass>();
        }

        internal void Filter(Func<QuestClass, bool> predicate)
        {
            try
            {
                _filterBy.Invoke(_instance, new object[] { predicate });
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("viewlist-filter", "Applying the quest filter failed: " + ex.Message);
            }
        }

        internal void SetOrder(IEnumerable<QuestClass> orderedQuests)
        {
            if (orderedQuests == null)
            {
                return;
            }

            try
            {
                _updateOrder.Invoke(_instance, new object[] { orderedQuests });
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("viewlist-order", "Reordering the quest list failed: " + ex.Message);
            }
        }
    }
}
