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

                TMP_Text source = field.textComponent;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(rect, worldPositionStays: false);

                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                label.text = "X";
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.enableWordWrapping = false;

                if (source != null)
                {
                    if (source.font != null)
                    {
                        label.font = source.font;
                    }

                    label.fontSize = Mathf.Clamp(source.fontSize, 10f, Size);

                    Color color = source.color;
                    color.a *= 0.55f;
                    label.color = color;
                }

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

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
    }
}
