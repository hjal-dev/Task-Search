using System;
using System.Collections.Generic;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Eft;
using TaskSearch.Logging;
using TaskSearch.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSearch.UI
{
    internal sealed class TaskSearchController : MonoBehaviour
    {
        private static readonly TaskSearchIndex Index = new TaskSearchIndex();

        private const float DebounceSeconds = 0.12f;

        private TasksPanel _panel;
        private TMP_InputField _field;

        private RectTransform _fieldRoot;

        private Button _clearButton;
        private Button _pinButton;
        private List<GameObject> _searchIcons;
        private FavoriteQuests _pinned;
        private bool _pinnedOnly;
        private bool _pinSpritesLoaded;
        private Sprite _pinOffSprite;
        private Sprite _pinOnSprite;
        private Color _pinOffColor;
        private Color _pinOnColor;
        private TMP_Text _descriptionText;
        private bool _descriptionRichText;
        private string _highlightSource;
        private string _highlightResult;
        private readonly RowHighlighter _rowHighlighter = new RowHighlighter();
        private ScrollRect _list;
        private GameObject _noResultsObject;
        private Transform _favoriteSeparator;

        private QuestViewList _viewList;
        private List<Quest> _unfilteredOrder = new List<Quest>();
        private TaskSearchQuery _query = TaskSearchQuery.Empty;

        private bool _separatorHidden;
        private bool _separatorWasActive;
        private int _separatorSiblingIndex = -1;
        private bool _noResultsWasActive;
        private bool _searchPending;

        internal bool IsFieldFocused
        {
            get { return _field != null && _field.isFocused; }
        }

        internal static TaskSearchController For(TasksPanel panel)
        {
            return panel == null ? null : panel.GetComponent<TaskSearchController>();
        }

        internal static TaskSearchController Attach(TasksPanel panel, QuestController questController)
        {
            if (panel == null)
            {
                return null;
            }

            TaskSearchController controller = panel.GetComponent<TaskSearchController>()
                                               ?? panel.gameObject.AddComponent<TaskSearchController>();

            controller._panel = panel;
            controller.RebuildIndex(questController);
            controller.EnsureField();
            controller.ResetSearchState();
            controller.OnQuestListRebuilt();

            return controller;
        }

        private void RebuildIndex(QuestController questController)
        {
            if (questController == null)
            {
                return;
            }

            try
            {
                List<Quest> quests = new List<Quest>();

                if (questController.Quests != null)
                {
                    foreach (object item in questController.Quests)
                    {
                        Quest quest = item as Quest;

                        if (quest != null)
                        {
                            quests.Add(quest);
                        }
                    }
                }

                Index.Rebuild(quests);
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Failed to rebuild the quest search index", ex);
            }
        }

        private void EnsureField()
        {
            if (_field != null)
            {
                ApplyLayout();
                return;
            }

            _list = ResolvePrivate<ScrollRect>("_scrollRect");
            _noResultsObject = ResolvePrivate<GameObject>("_noActiveTasksObject");
            _favoriteSeparator = ResolvePrivate<Transform>("_favoriteQuestSeparator");

            TMP_InputField template = TaskSearchFieldFactory.FindTemplate(_panel);

            if (template == null)
            {
                return;
            }

            RectTransform layoutRoot;
            _field = TaskSearchFieldFactory.Create(_panel, template, _list, out layoutRoot);

            if (_field == null)
            {
                return;
            }

            _fieldRoot = layoutRoot;

            _searchIcons = TaskSearchFieldFactory.CollectDecorationIcons(_fieldRoot.gameObject, _field);

            _field.onValueChanged.AddListener(OnSearchTextChanged);
            _clearButton = TaskSearchClearButton.Create(_field, ClearSearch);
            _pinButton = TaskSearchPinButton.Create(_field, TogglePinnedOnly);

            TaskSearchConfig.LayoutChanged += ApplyLayout;
            TaskSearchConfig.SearchedFieldsChanged += OnSearchedFieldsChanged;
        }

        private void OnSearchedFieldsChanged()
        {
            Index.Invalidate();
            Index.IndexMissing(_unfilteredOrder);
            ApplyQuery();
        }

        private void ApplyLayout()
        {
            if (_fieldRoot == null)
            {
                return;
            }

            TaskSearchFieldFactory.ApplyLayout(
                _fieldRoot,
                _list != null ? _list.GetComponent<RectTransform>() : null);
        }

        private T ResolvePrivate<T>(string fieldName) where T : class
        {
            try
            {
                return AccessTools.Field(typeof(TasksPanel), fieldName)?.GetValue(_panel) as T;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal void OnQuestListRebuilt()
        {
            _separatorHidden = false;

            bool listIsEmpty = _noResultsObject != null && _noResultsObject.activeSelf;
            _noResultsWasActive = listIsEmpty;

            if (listIsEmpty)
            {
                _viewList = null;
                _unfilteredOrder = new List<Quest>();
                return;
            }

            _pinned = FavoriteQuests.FromTasksPanel(_panel);
            _viewList = QuestViewList.FromTasksPanel(_panel);

            if (_viewList == null)
            {
                _unfilteredOrder = new List<Quest>();
                return;
            }

            _unfilteredOrder = _viewList.GetQuests();

            EnsurePinSprite();
            Index.IndexMissing(_unfilteredOrder);

            if (!_query.IsEmpty)
            {
                ApplyQuery();
            }
        }

        internal void OnPanelClosed()
        {
            _viewList = null;
            _unfilteredOrder = new List<Quest>();
            _separatorHidden = false;

            ResetSearchState();
        }

        private void ResetSearchState()
        {
            _query = TaskSearchQuery.Empty;
            _searchPending = false;
            CancelInvoke(nameof(RunPendingSearch));

            if (_field != null && !string.IsNullOrEmpty(_field.text))
            {
                _field.text = string.Empty;
            }

            UpdateClearButton();
        }

        private void OnSearchTextChanged(string text)
        {
            if (!TaskSearchConfig.Enabled.Value)
            {
                return;
            }

            _query = TaskSearchQuery.Parse(text);
            UpdateClearButton();

            RemoveHighlight();
            _rowHighlighter.Clear();

            if (DebounceSeconds <= 0f || _query.IsEmpty)
            {
                CancelInvoke(nameof(RunPendingSearch));
                _searchPending = false;
                ApplyQuery();
                return;
            }

            if (!_searchPending)
            {
                _searchPending = true;
            }
            else
            {
                CancelInvoke(nameof(RunPendingSearch));
            }

            Invoke(nameof(RunPendingSearch), DebounceSeconds);
        }

        private void RunPendingSearch()
        {
            _searchPending = false;
            ApplyQuery();
        }

        private void Update()
        {
            if (_query.IsEmpty || !TaskSearchConfig.Enabled.Value)
            {
                return;
            }

            HighlightDescription();

            _rowHighlighter.Apply(
                _panel,
                _query,
                TaskSearchConfig.SearchQuestNames.Value,
                TaskSearchConfig.SearchLocations.Value);
        }

        private void HighlightDescription()
        {
            if (!TaskSearchConfig.SearchDescriptions.Value)
            {
                return;
            }

            TMP_Text target = ResolveDescriptionText();

            if (target == null)
            {
                return;
            }

            string current = target.text;

            if (string.IsNullOrEmpty(current) || current == _highlightResult)
            {
                return;
            }

            _highlightSource = current;
            _highlightResult = SearchHighlighter.Apply(current, _query, _descriptionRichText);

            if (_highlightResult != current)
            {
                target.richText = true;
                target.text = _highlightResult;
            }
        }

        private void RemoveHighlight()
        {
            if (_descriptionText != null)
            {
                if (_highlightResult != null
                    && _highlightSource != null
                    && _descriptionText.text == _highlightResult)
                {
                    _descriptionText.text = _highlightSource;
                }

                _descriptionText.richText = _descriptionRichText;
            }

            _highlightSource = null;
            _highlightResult = null;
        }

        private TMP_Text ResolveDescriptionText()
        {
            if (_descriptionText != null)
            {
                return _descriptionText;
            }

            if (_panel == null)
            {
                return null;
            }

            try
            {
                NotesTaskDescriptionShort description =
                    _panel.GetComponentInChildren<NotesTaskDescriptionShort>(true);

                if (description == null)
                {
                    return null;
                }

                _descriptionText = AccessTools.Field(typeof(NotesTaskDescriptionShort), "_description")
                    ?.GetValue(description) as TMP_Text;

                if (_descriptionText != null)
                {
                    _descriptionRichText = _descriptionText.richText;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return _descriptionText;
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

        private void EnsurePinSprite()
        {
            if (_pinButton == null)
            {
                return;
            }

            if (!_pinSpritesLoaded)
            {
                if (!TaskSearchPinButton.TryReadSprites(
                        _panel,
                        out _pinOffSprite, out _pinOffColor,
                        out _pinOnSprite, out _pinOnColor))
                {
                    return;
                }

                _pinSpritesLoaded = true;
            }

            ApplyPinState();
        }

        private void ApplyPinState()
        {
            if (!_pinSpritesLoaded)
            {
                return;
            }

            TaskSearchPinButton.SetState(
                _pinButton,
                _pinnedOnly ? _pinOnSprite : _pinOffSprite,
                _pinnedOnly ? _pinOnColor : _pinOffColor);
        }

        private void ApplyQuery()
        {
            if (_viewList == null || !TaskSearchConfig.Enabled.Value)
            {
                return;
            }

            if (_query.IsEmpty && !_pinnedOnly)
            {
                RestoreUnfilteredList();
                return;
            }

            HashSet<Quest> matched = new HashSet<Quest>();
            List<Quest> ordered = new List<Quest>(_unfilteredOrder.Count);

            if (_query.IsEmpty)
            {
                foreach (Quest quest in _unfilteredOrder)
                {
                    if (IsPinnedIfRequired(quest))
                    {
                        matched.Add(quest);
                        ordered.Add(quest);
                    }
                }
            }
            else
            {
                List<TaskSearchResult> results = Index.Search(_unfilteredOrder, _query);

                foreach (TaskSearchResult result in results)
                {
                    if (IsPinnedIfRequired(result.Quest))
                    {
                        matched.Add(result.Quest);
                        ordered.Add(result.Quest);
                    }
                }
            }

            _viewList.Filter(quest => quest != null && matched.Contains(quest));

            foreach (Quest quest in _unfilteredOrder)
            {
                if (!matched.Contains(quest))
                {
                    ordered.Add(quest);
                }
            }

            _viewList.SetOrder(ordered);

            HideFavoriteSeparator();
            SetNoResultsVisible(matched.Count == 0);
        }

        private bool IsPinnedIfRequired(Quest quest)
        {
            if (!_pinnedOnly)
            {
                return true;
            }

            if (_pinned == null || quest == null)
            {
                return false;
            }

            string questId;

            try
            {
                questId = quest.Id;
            }
            catch (Exception)
            {
                return false;
            }

            return _pinned.IsPinned(questId);
        }

        private void TogglePinnedOnly()
        {
            _pinnedOnly = !_pinnedOnly;
            ApplyPinState();
            ApplyQuery();
        }

        private void RestoreUnfilteredList()
        {
            _viewList.Filter(null);

            _viewList.SetOrder(_unfilteredOrder);

            RestoreFavoriteSeparator();
            SetNoResultsVisible(_noResultsWasActive);
        }

        private void HideFavoriteSeparator()
        {
            if (_favoriteSeparator == null || _separatorHidden)
            {
                return;
            }

            _separatorWasActive = _favoriteSeparator.gameObject.activeSelf;
            _separatorSiblingIndex = _favoriteSeparator.GetSiblingIndex();
            _favoriteSeparator.gameObject.SetActive(false);
            _separatorHidden = true;
        }

        private void RestoreFavoriteSeparator()
        {
            if (_favoriteSeparator == null || !_separatorHidden)
            {
                return;
            }

            Transform parent = _favoriteSeparator.parent;

            if (parent != null && _separatorSiblingIndex >= 0 && _separatorSiblingIndex < parent.childCount)
            {
                _favoriteSeparator.SetSiblingIndex(_separatorSiblingIndex);
            }

            _favoriteSeparator.gameObject.SetActive(_separatorWasActive);
            _separatorHidden = false;
        }

        private void SetNoResultsVisible(bool visible)
        {
            if (_noResultsObject == null)
            {
                return;
            }

            _noResultsObject.SetActive(visible);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(RunPendingSearch));
            _searchPending = false;
        }

        private void OnDestroy()
        {
            _rowHighlighter.Clear();
            CancelInvoke(nameof(RunPendingSearch));
            TaskSearchConfig.LayoutChanged -= ApplyLayout;
            TaskSearchConfig.SearchedFieldsChanged -= OnSearchedFieldsChanged;

            if (_field != null)
            {
                _field.onValueChanged.RemoveListener(OnSearchTextChanged);
            }
        }
    }
}
