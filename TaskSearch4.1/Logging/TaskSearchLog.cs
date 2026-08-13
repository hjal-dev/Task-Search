using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace TaskSearch.Logging
{
    internal static class TaskSearchLog
    {
        private static ManualLogSource _source;
        private static readonly HashSet<string> SeenWarnings = new HashSet<string>(StringComparer.Ordinal);

        internal static void Initialize(ManualLogSource source)
        {
            _source = source;
        }

        internal static void Warn(string message)
        {
            _source?.LogWarning(message);
        }

        internal static void WarnOnce(string key, string message)
        {
            if (_source == null || key == null)
            {
                return;
            }

            lock (SeenWarnings)
            {
                if (!SeenWarnings.Add(key))
                {
                    return;
                }
            }

            _source.LogWarning(message);
        }

        internal static void Error(string message)
        {
            _source?.LogError(message);
        }

        internal static void Error(string message, Exception ex)
        {
            _source?.LogError(message + ": " + ex);
        }
    }
}
