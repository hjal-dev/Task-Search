using System;
using System.Collections.Generic;
using EFT.UI;
using HarmonyLib;
using System.Reflection;
using TaskSearch.Search;
using TMPro;

namespace TaskSearch.UI
{
    internal sealed class RowHighlighter
    {
        private static FieldInfo _taskLabelField;
        private static FieldInfo _locationLabelField;
        private static bool _fieldsResolved;

        private sealed class State
        {
            public string Source;
            public string Result;
            public bool OriginalRichText;
        }

        private readonly Dictionary<TMP_Text, State> _states = new Dictionary<TMP_Text, State>();

        internal void Apply(TasksPanel panel, TaskSearchQuery query, bool names, bool locations)
        {
            if (panel == null || query == null || query.IsEmpty || (!names && !locations))
            {
                Clear();
                return;
            }

            ResolveFields();

            NotesTask[] rows = panel.GetComponentsInChildren<NotesTask>(false);

            for (int i = 0; i < rows.Length; i++)
            {
                NotesTask row = rows[i];

                if (row == null)
                {
                    continue;
                }

                if (names)
                {
                    Highlight(Read(_taskLabelField, row), query);
                }

                if (locations)
                {
                    Highlight(Read(_locationLabelField, row), query);
                }
            }
        }

        internal void Clear()
        {
            if (_states.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<TMP_Text, State> pair in _states)
            {
                TMP_Text label = pair.Key;
                State state = pair.Value;

                if (label == null)
                {
                    continue;
                }

                if (state.Result != null && state.Source != null && label.text == state.Result)
                {
                    label.text = state.Source;
                }

                label.richText = state.OriginalRichText;
            }

            _states.Clear();
        }

        private void Highlight(TMP_Text label, TaskSearchQuery query)
        {
            if (label == null)
            {
                return;
            }

            string current = label.text;

            if (string.IsNullOrEmpty(current))
            {
                return;
            }

            State state;

            if (!_states.TryGetValue(label, out state))
            {
                state = new State { OriginalRichText = label.richText };
                _states[label] = state;
            }

            if (current == state.Result)
            {
                return;
            }

            state.Source = current;
            string result = SearchHighlighter.Apply(current, query, state.OriginalRichText);
            state.Result = result;

            if (result != current)
            {
                label.richText = true;
                label.text = result;
            }
        }

        private static TMP_Text Read(FieldInfo field, NotesTask row)
        {
            if (field == null || row == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(row) as TMP_Text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ResolveFields()
        {
            if (_fieldsResolved)
            {
                return;
            }

            _fieldsResolved = true;
            _taskLabelField = AccessTools.Field(typeof(NotesTask), "_taskLabel");
            _locationLabelField = AccessTools.Field(typeof(NotesTask), "_locationLabel");
        }
    }
}
