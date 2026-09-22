using System;
using TaskSearch.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSearch.UI
{
    internal static class TaskSearchClearButton
    {
        private const float Size = 22f;

        private const float RightMargin = 10f;

        private const float GlyphLength = 15f;

        private const float GlyphThickness = 2f;

        internal static Button Create(TMP_InputField field, Action onClick)
        {
            if (field == null)
            {
                return null;
            }

            try
            {
                GameObject holder = new GameObject(
                    "TaskSearchClear",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));

                RectTransform rect = holder.GetComponent<RectTransform>();
                rect.SetParent(field.transform, worldPositionStays: false);
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(Size, Size);
                rect.anchoredPosition = new Vector2(-RightMargin, 0f);

                Image background = holder.GetComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0f);
                background.raycastTarget = true;

                Color glyphColor = ResolveGlyphColor(field);
                CreateBar(rect, glyphColor, 45f);
                CreateBar(rect, glyphColor, -45f);

                Button button = holder.GetComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;

                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick());
                }

                holder.SetActive(false);
                return button;
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Failed to create the search clear button", ex);
                return null;
            }
        }

        private static void CreateBar(RectTransform parent, Color color, float angle)
        {
            GameObject bar = new GameObject(
                "Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(GlyphLength, GlyphThickness);
            rect.anchoredPosition = Vector2.zero;
            rect.localEulerAngles = new Vector3(0f, 0f, angle);

            Image image = bar.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static Color ResolveGlyphColor(TMP_InputField field)
        {
            TMP_Text source = field.textComponent;

            if (source == null)
            {
                source = field.placeholder as TMP_Text;
            }

            if (source == null)
            {
                source = field.GetComponentInChildren<TMP_Text>(true);
            }

            if (source != null)
            {
                Color color = source.color;
                color.a = color.a > 0f ? Mathf.Clamp01(color.a) * 0.7f : 0.6f;
                return color;
            }

            return new Color(0.82f, 0.82f, 0.82f, 0.6f);
        }
    }
}
