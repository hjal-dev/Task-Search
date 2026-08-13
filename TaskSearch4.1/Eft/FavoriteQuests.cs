using System;
using System.Reflection;
using EFT.Quests;
using EFT.UI;
using TaskSearch.Logging;

namespace TaskSearch.Eft
{
    internal sealed class FavoriteQuests
    {
        private static FieldInfo _managerField;
        private static bool _managerFieldResolved;

        private readonly object _instance;
        private readonly MethodInfo _isFavorite;

        private FavoriteQuests(object instance, MethodInfo isFavorite)
        {
            _instance = instance;
            _isFavorite = isFavorite;
        }

        internal static FavoriteQuests FromTasksPanel(TasksPanel panel)
        {
            if (panel == null)
            {
                return null;
            }

            try
            {
                FieldInfo field = ResolveManagerField();

                if (field == null)
                {
                    return null;
                }

                object instance = field.GetValue(panel);

                if (instance == null)
                {
                    return null;
                }

                MethodInfo isFavorite = FindIsFavorite(instance.GetType());

                if (isFavorite == null)
                {
                    return null;
                }

                return new FavoriteQuests(instance, isFavorite);
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("favorites-read", "Could not read the pinned task list: " + ex.Message);
                return null;
            }
        }

        private static FieldInfo ResolveManagerField()
        {
            if (_managerFieldResolved)
            {
                return _managerField;
            }

            _managerFieldResolved = true;

            FieldInfo[] fields = typeof(TasksPanel)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            for (int i = 0; i < fields.Length; i++)
            {
                if (FindIsFavorite(fields[i].FieldType) != null)
                {
                    _managerField = fields[i];
                    return _managerField;
                }
            }

            TaskSearchLog.Warn("Could not find the pinned task manager on TasksPanel. The pin filter is disabled.");
            return null;
        }

        private static MethodInfo FindIsFavorite(Type type)
        {
            if (type == null || type.IsPrimitive || type == typeof(string))
            {
                return null;
            }

            MethodInfo method = type.GetMethod(
                "IsFavorite",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(string) },
                null);

            if (method == null || method.ReturnType != typeof(bool))
            {
                return null;
            }

            return method;
        }

        internal bool IsPinned(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            try
            {
                object result = _isFavorite.Invoke(_instance, new object[] { questId });
                return result is bool && (bool)result;
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("favorites-check", "Reading a pinned task failed: " + ex.Message);
                return false;
            }
        }
    }
}
