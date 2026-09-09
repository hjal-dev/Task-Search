using System;
using System.Collections.Generic;
using EFT.Quests;
using EFT.Trading;
using EFT.UI;
using TaskSearch.Logging;
using TaskSearch.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSearch.UI
{
    internal sealed class TraderTaskSearchController : MonoBehaviour
    {
        private readonly TaskSearchIndex _index = new TaskSearchIndex();

        private QuestsListView _view;
        private string _traderId;
        private TMP_InputField _field;
        private RectTransform _fieldRoot;
        private Button _clearButton;
        private List<GameObject> _searchIcons;
        private TaskSearchQuery _query = TaskSearchQuery.Empty;

        private RectTransform _list;
        private bool _listCaptured;
        private float _listTop;
        private float _filtersBottom;
        private bool _filtersCaptured;
        private bool _subscribed;

        private readonly RowHighlighter _highlighter = new RowHighlighter();
        private NotesTaskDescription _descPanel;
        private System.Reflection.FieldInfo _titleField;
        private System.Reflection.FieldInfo _descField;
        private System.Reflection.FieldInfo _locField;
        private System.Reflection.FieldInfo _questViewField;
        private bool _hlFieldsResolved;

        internal static TraderTaskSearchController For(QuestsListView view)
        {
            return view == null ? null : view.GetComponent<TraderTaskSearchController>();
        }

        internal static TraderTaskSearchController Attach(QuestsListView view, Trader trader)
        {
            if (view == null || trader == null || !TaskSearchConfig.Enabled.Value)
            {
                return null;
            }

            TraderTaskSearchController controller = view.GetComponent<TraderTaskSearchController>()
                                                    ?? view.gameObject.AddComponent<TraderTaskSearchController>();

            controller._view = view;

            try
            {
                controller._traderId = trader.Id;
            }
            catch (Exception)
            {
                controller._traderId = null;
            }

            controller.EnsureSubscribed();
            controller.Reconcile();
            return controller;
        }

        private void EnsureSubscribed()
        {
            if (_subscribed)
            {
                return;
            }

            TaskSearchConfig.TraderEnabledChanged += OnTraderEnabledChanged;
            _subscribed = true;
        }

        private void OnTraderEnabledChanged()
        {
            Reconcile();
        }

        private void Reconcile()
        {
            if (TaskSearchConfig.Enabled.Value && TaskSearchConfig.TraderSearchEnabled.Value)
            {
                EnsureField();
                ResetQuery();
            }
            else
            {
                DisableSearch();
            }
        }

        private void DisableSearch()
        {
            _query = TaskSearchQuery.Empty;
            _highlighter.Clear();

            if (_field != null)
            {
                if (!string.IsNullOrEmpty(_field.text))
                {
                    _field.text = string.Empty;
                }

                UpdateClearButton();

                if (_view != null)
                {
                    try
                    {
                        _view.UpdateVisibility();
                    }
                    catch (Exception)
                    {
                    }
                }

                _field.gameObject.SetActive(false);
            }

            RestoreList();
        }

        private void RestoreList()
        {
            if (_list != null && _listCaptured)
            {
                _list.offsetMax = new Vector2(_list.offsetMax.x, _listTop);
            }
        }

        private void EnsureField()
        {
            if (_field != null)
            {
                _field.gameObject.SetActive(true);
                ApplyLayout();
                return;
            }

            TMP_InputField template = TaskSearchFieldFactory.FindSearchTemplate(_view.transform.root);

            if (template == null)
            {
                return;
            }

            RectTransform layoutRoot;
            _field = TaskSearchFieldFactory.CreateUnder(_view.transform, template, null, out layoutRoot);

            if (_field == null)
            {
                return;
            }

            _fieldRoot = layoutRoot;
            ApplyTraderLayout();
            _searchIcons = TaskSearchFieldFactory.CollectDecorationIcons(_fieldRoot.gameObject, _field);

            _field.onValueChanged.AddListener(OnTextChanged);
            _clearButton = TaskSearchClearButton.Create(_field, ClearSearch);

            TaskSearchConfig.LayoutChanged += ApplyLayout;
        }

        private void ApplyLayout()
        {
            ApplyTraderLayout();
        }

        private void ApplyTraderLayout()
        {
            if (_fieldRoot == null
                || !TaskSearchConfig.Enabled.Value
                || !TaskSearchConfig.TraderSearchEnabled.Value)
            {
                return;
            }

            float height = TaskSearchConfig.TraderFieldHeight.Value;
            float offsetX = TaskSearchConfig.TraderFieldOffsetX.Value;
            float offsetY = TaskSearchConfig.TraderFieldOffsetY.Value;

            EnsureListCaptured();

            if (_list != null && _filtersCaptured)
            {
                float barTop = _filtersBottom - 6f + offsetY;

                _fieldRoot.anchorMin = new Vector2(_list.anchorMin.x, 1f);
                _fieldRoot.anchorMax = new Vector2(_list.anchorMax.x, 1f);
                _fieldRoot.pivot = new Vector2(0f, 1f);
                _fieldRoot.offsetMin = new Vector2(_list.offsetMin.x, barTop - height);
                _fieldRoot.offsetMax = new Vector2(_list.offsetMax.x, barTop);

                _list.offsetMax = new Vector2(_list.offsetMax.x, barTop - height - 6f);
                return;
            }

            float width = TaskSearchConfig.TraderFieldWidth.Value;

            _fieldRoot.anchorMin = new Vector2(0f, 1f);
            _fieldRoot.anchorMax = new Vector2(0f, 1f);
            _fieldRoot.pivot = new Vector2(0f, 1f);
            _fieldRoot.sizeDelta = new Vector2(width, height);

            if (_list != null)
            {
                float gap = height + 10f;
                _list.offsetMax = new Vector2(_list.offsetMax.x, _listTop - gap);
                _fieldRoot.anchoredPosition = new Vector2(offsetX, _listTop - 5f + offsetY);
                return;
            }

            _fieldRoot.anchoredPosition = new Vector2(offsetX, offsetY);
        }

        private void OnTextChanged(string text)
        {
            if (!TaskSearchConfig.Enabled.Value || !TaskSearchConfig.TraderSearchEnabled.Value)
            {
                return;
            }

            _query = TaskSearchQuery.Parse(text);
            UpdateClearButton();
            _highlighter.Clear();
            ApplyFilter();
        }

        private void ClearSearch()
        {
            if (_field == null)
            {
                return;
            }

            _field.text = string.Empty;
            _field.ActivateInputField();
        }

        private void ResetQuery()
        {
            _query = TaskSearchQuery.Empty;

            if (_field != null && !string.IsNullOrEmpty(_field.text))
            {
                _field.text = string.Empty;
            }

            UpdateClearButton();
        }

        private void UpdateClearButton()
        {
            bool hasText = _field != null && !string.IsNullOrEmpty(_field.text);

            if (_clearButton != null)
            {
                _clearButton.gameObject.SetActive(hasText);
            }

            if (_searchIcons != null)
            {
                for (int i = 0; i < _searchIcons.Count; i++)
                {
                    if (_searchIcons[i] != null)
                    {
                        _searchIcons[i].SetActive(!hasText);
                    }
                }
            }
        }

        private void ApplyFilter()
        {
            if (_view == null)
            {
                return;
            }

            try
            {
                _view.UpdateVisibility();
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Trader task filter failed", ex);
            }
        }

        internal void OnVisibilityUpdated()
        {
            if (_view == null || _query.IsEmpty
                || !TaskSearchConfig.Enabled.Value
                || !TaskSearchConfig.TraderSearchEnabled.Value)
            {
                return;
            }

            QuestListItem[] rows = _view.GetComponentsInChildren<QuestListItem>(true);

            List<Quest> candidates = new List<Quest>(rows.Length);

            for (int i = 0; i < rows.Length; i++)
            {
                Quest quest = rows[i] != null ? rows[i].Quest : null;

                if (quest != null && BelongsToTrader(quest))
                {
                    candidates.Add(quest);
                }
            }

            _index.IndexMissing(candidates);
            List<TaskSearchResult> results = _index.Search(candidates, _query);

            HashSet<string> matched = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < results.Count; i++)
            {
                string id = SafeId(results[i].Quest);

                if (id != null)
                {
                    matched.Add(id);
                }
            }

            for (int i = 0; i < rows.Length; i++)
            {
                QuestListItem row = rows[i];

                if (row == null || row.Quest == null || !row.gameObject.activeSelf)
                {
                    continue;
                }

                string id = SafeId(row.Quest);

                if (id == null || !matched.Contains(id))
                {
                    row.gameObject.SetActive(false);
                }
            }
        }

        internal void OnClosed()
        {
            _query = TaskSearchQuery.Empty;
            _highlighter.Clear();

            if (_field != null && !string.IsNullOrEmpty(_field.text))
            {
                _field.text = string.Empty;
            }

            UpdateClearButton();
        }

        private void Update()
        {
            if (_query.IsEmpty
                || _view == null
                || !TaskSearchConfig.Enabled.Value
                || !TaskSearchConfig.TraderSearchEnabled.Value)
            {
                return;
            }

            EnsureHighlightFields();

            if (TaskSearchConfig.SearchQuestNames.Value)
            {
                QuestListItem[] rows = _view.GetComponentsInChildren<QuestListItem>(false);

                for (int i = 0; i < rows.Length; i++)
                {
                    _highlighter.HighlightLabel(ReadText(_titleField, rows[i]), _query);
                }
            }

            NotesTaskDescription panel = ResolveDescriptionPanel();

            if (panel != null)
            {
                if (TaskSearchConfig.SearchDescriptions.Value)
                {
                    _highlighter.HighlightLabel(ReadText(_descField, panel), _query);
                }

                if (TaskSearchConfig.SearchLocations.Value)
                {
                    _highlighter.HighlightLabel(ReadText(_locField, panel), _query);
                }
            }
        }

        private void EnsureHighlightFields()
        {
            if (_hlFieldsResolved)
            {
                return;
            }

            _hlFieldsResolved = true;
            _titleField = HarmonyLib.AccessTools.Field(typeof(QuestListItem), "_title");
            _descField = HarmonyLib.AccessTools.Field(typeof(NotesTaskDescription), "_description");
            _locField = HarmonyLib.AccessTools.Field(typeof(NotesTaskDescription), "_location");
            _questViewField = HarmonyLib.AccessTools.Field(typeof(QuestsListView), "_questView");
        }

        private NotesTaskDescription ResolveDescriptionPanel()
        {
            if (_descPanel != null)
            {
                return _descPanel;
            }

            try
            {
                Component questView = _questViewField != null
                    ? _questViewField.GetValue(_view) as Component
                    : null;

                if (questView != null)
                {
                    _descPanel = questView.GetComponentInChildren<NotesTaskDescription>(true);
                }
            }
            catch (Exception)
            {
            }

            return _descPanel;
        }

        private static TMP_Text ReadText(System.Reflection.FieldInfo field, object owner)
        {
            if (field == null || owner == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(owner) as TMP_Text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool BelongsToTrader(Quest quest)
        {
            if (string.IsNullOrEmpty(_traderId))
            {
                return true;
            }

            try
            {
                return quest != null && quest.Template != null && quest.Template.TraderId == _traderId;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void EnsureListCaptured()
        {
            if (_listCaptured)
            {
                return;
            }

            Transform content = FieldTransform("_questListContainer");

            if (content == null)
            {
                return;
            }

            Transform node = content;

            while (node != null && node.parent != _view.transform)
            {
                node = node.parent;
            }

            _list = node as RectTransform;

            if (_list == null)
            {
                return;
            }

            _listTop = _list.offsetMax.y;
            _listCaptured = true;

            Transform toggle = FieldTransform("_toggleShowCompleted");
            RectTransform filters = toggle != null ? toggle.parent as RectTransform : null;

            if (filters != null && filters.parent == _view.transform)
            {
                _filtersBottom = filters.offsetMin.y;
                _filtersCaptured = true;
            }
        }

        private Transform FieldTransform(string name)
        {
            try
            {
                System.Reflection.FieldInfo f = HarmonyLib.AccessTools.Field(typeof(QuestsListView), name);
                object v = f == null ? null : f.GetValue(_view);
                Component c = v as Component;
                return c != null ? c.transform : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string SafeId(Quest quest)
        {
            try
            {
                return quest == null ? null : quest.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnDestroy()
        {
            _highlighter.Clear();
            TaskSearchConfig.LayoutChanged -= ApplyLayout;
            TaskSearchConfig.TraderEnabledChanged -= OnTraderEnabledChanged;

            if (_field != null)
            {
                _field.onValueChanged.RemoveListener(OnTextChanged);
            }
        }
    }
}
