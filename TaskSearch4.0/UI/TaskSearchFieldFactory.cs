using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Eft;
using TaskSearch.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSearch.UI
{
    internal static class TaskSearchFieldFactory
    {
        internal static TMP_InputField FindTemplate(TasksPanel panel)
        {
            if (panel == null)
            {
                return null;
            }

            TasksScreen screen = panel.GetComponentInParent<TasksScreen>();

            if (screen != null)
            {
                try
                {
                    FieldInfo info = AccessTools.Field(typeof(TasksScreen), "_searchField");

                    if (info != null)
                    {
                        TMP_InputField field = info.GetValue(screen) as TMP_InputField;

                        if (field != null)
                        {
                            return field;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return FindTemplateUnder(panel.transform.root);
        }

        internal static TMP_InputField FindSearchTemplate(Transform root)
        {
            try
            {
                TasksScreen[] screens = Resources.FindObjectsOfTypeAll<TasksScreen>();
                FieldInfo info = AccessTools.Field(typeof(TasksScreen), "_searchField");

                if (info != null && screens != null)
                {
                    for (int i = 0; i < screens.Length; i++)
                    {
                        if (screens[i] == null)
                        {
                            continue;
                        }

                        TMP_InputField field = info.GetValue(screens[i]) as TMP_InputField;

                        if (field != null)
                        {
                            return field;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return FindTemplateUnder(root);
        }

        internal static TMP_InputField FindTemplateUnder(Transform root)
        {
            if (root != null)
            {
                TMP_InputField[] fields = root.GetComponentsInChildren<TMP_InputField>(true);

                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] != null)
                    {
                        return fields[i];
                    }
                }
            }

            TaskSearchLog.Warn(
                "No TMP_InputField available to clone; the quest search field cannot be created.");

            return null;
        }

        internal static TMP_InputField Create(
            TasksPanel panel, TMP_InputField template, ScrollRect list,
            out RectTransform layoutRoot)
        {
            layoutRoot = null;

            if (panel == null || template == null)
            {
                return null;
            }

            RectTransform listRect = list != null ? list.GetComponent<RectTransform>() : null;
            Transform parent = listRect != null ? listRect.parent : panel.transform;

            return CreateUnder(parent, template, listRect, out layoutRoot);
        }

        internal static TMP_InputField CreateUnder(
            Transform parent, TMP_InputField template, RectTransform list,
            out RectTransform layoutRoot)
        {
            layoutRoot = null;

            if (parent == null || template == null)
            {
                return null;
            }

            try
            {
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent, worldPositionStays: false);
                clone.name = "TaskSearchField";
                clone.SetActive(true);

                TMP_InputField field = clone.GetComponent<TMP_InputField>()
                                       ?? clone.GetComponentInChildren<TMP_InputField>(includeInactive: true);

                if (field == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    TaskSearchLog.Warn("Cloned object had no TMP_InputField; aborting field creation.");
                    return null;
                }

                DetachInheritedListeners(field);

                field.text = string.Empty;
                field.lineType = TMP_InputField.LineType.SingleLine;
                field.richText = false;
                field.characterLimit = 128;

                ApplyPlaceholder(field);

                layoutRoot = clone.GetComponent<RectTransform>();
                ApplyLayout(layoutRoot, list);

                return field;
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Failed to create the quest search field", ex);
                return null;
            }
        }

        private const float DecorationEdgeMargin = 10f;

        internal static List<GameObject> CollectDecorationIcons(GameObject clone, TMP_InputField field)
        {
            List<GameObject> icons = new List<GameObject>();

            if (clone == null || field == null)
            {
                return icons;
            }

            Image root = clone.GetComponent<Image>();
            Image[] images = clone.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null || image == root || image == field.targetGraphic)
                {
                    continue;
                }

                if (image.sprite != null && image.gameObject.activeSelf)
                {
                    PlaceAtEdge(image.rectTransform, field.transform);
                    icons.Add(image.gameObject);
                }
            }

            return icons;
        }

        private static void PlaceAtEdge(RectTransform rect, Transform fieldTransform)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 size = rect.sizeDelta;
            rect.SetParent(fieldTransform, worldPositionStays: false);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(-DecorationEdgeMargin, 0f);
        }

        private static void DetachInheritedListeners(TMP_InputField field)
        {
            field.onValueChanged = new TMP_InputField.OnChangeEvent();
            field.onEndEdit = new TMP_InputField.SubmitEvent();
            field.onSubmit = new TMP_InputField.SubmitEvent();
            field.onSelect = new TMP_InputField.SelectionEvent();
            field.onDeselect = new TMP_InputField.SelectionEvent();
        }

        private static void ApplyPlaceholder(TMP_InputField field)
        {
            TMP_Text placeholder = field.placeholder as TMP_Text;

            if (placeholder == null)
            {
                return;
            }

            string[] keys = new string[] { "Search", "SEARCH", "Search..." };

            for (int i = 0; i < keys.Length; i++)
            {
                string localized;

                if (EftLocalization.TryLocalize(keys[i], out localized))
                {
                    placeholder.text = localized;
                    return;
                }
            }
        }

        internal static void ApplyLayout(RectTransform field, RectTransform list)
        {
            if (field == null)
            {
                return;
            }

            try
            {
                float height = TaskSearchConfig.FieldHeight.Value;
                float width = TaskSearchConfig.FieldWidth.Value;
                float offsetX = TaskSearchConfig.FieldOffsetX.Value;
                float offsetY = TaskSearchConfig.FieldOffsetY.Value;

                Transform parent = field.parent;
                LayoutGroup group = parent != null ? parent.GetComponent<LayoutGroup>() : null;

                if (group != null && !(group is GridLayoutGroup))
                {
                    LayoutElement element = field.gameObject.GetComponent<LayoutElement>()
                                            ?? field.gameObject.AddComponent<LayoutElement>();
                    element.minHeight = height;
                    element.preferredHeight = height;
                    element.flexibleHeight = 0f;

                    if (list != null)
                    {
                        field.SetSiblingIndex(list.GetSiblingIndex());
                    }

                    return;
                }

                if (list == null)
                {
                    field.anchorMin = new Vector2(0f, 1f);
                    field.anchorMax = new Vector2(1f, 1f);
                    field.pivot = new Vector2(0.5f, 1f);
                    field.offsetMin = new Vector2(offsetX, -height + offsetY);
                    field.offsetMax = new Vector2(offsetX, offsetY);
                    return;
                }

                float top = list.offsetMax.y + offsetY;

                field.anchorMin = new Vector2(list.anchorMax.x, list.anchorMax.y);
                field.anchorMax = new Vector2(list.anchorMax.x, list.anchorMax.y);
                field.pivot = new Vector2(1f, 1f);
                field.offsetMin = new Vector2(list.offsetMax.x + offsetX - width, top - height);
                field.offsetMax = new Vector2(list.offsetMax.x + offsetX, top);
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Failed to lay out the quest search field", ex);
            }
        }
    }
}
