using System;
using EFT.UI;
using TaskSearch.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSearch.UI
{
    internal static class TaskSearchPinButton
    {
        private const float Size = 22f;
        private const float RightMargin = 36f;
        private const float InactiveAlpha = 0.4f;
        private const float ActiveAlpha = 1f;

        internal static Button Create(TMP_InputField field, Action onClick)
        {
            if (field == null)
            {
                return null;
            }

            try
            {
                GameObject holder = new GameObject(
                    "TaskSearchPin",
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

                Image icon = holder.GetComponent<Image>();
                icon.color = new Color(1f, 1f, 1f, 0f);
                icon.raycastTarget = true;
                icon.preserveAspect = true;

                Button button = holder.GetComponent<Button>();
                button.targetGraphic = icon;
                button.transition = Selectable.Transition.None;

                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick());
                }

                return button;
            }
            catch (Exception ex)
            {
                TaskSearchLog.Error("Failed to create the pinned-tasks button", ex);
                return null;
            }
        }

        internal static Sprite FindGamePinSprite(TasksPanel panel)
        {
            if (panel == null)
            {
                return null;
            }

            try
            {
                NotesTaskFavoriteButton favorite =
                    panel.GetComponentInChildren<NotesTaskFavoriteButton>(true);

                if (favorite == null)
                {
                    return null;
                }

                Image[] images = favorite.GetComponentsInChildren<Image>(true);
                Sprite widest = null;

                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i];

                    if (image == null || image.sprite == null)
                    {
                        continue;
                    }

                    if (image.type == Image.Type.Sliced)
                    {
                        continue;
                    }

                    if (widest == null)
                    {
                        widest = image.sprite;
                    }
                }

                return widest;
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("pin-sprite", "Could not read the in-game pin icon: " + ex.Message);
                return null;
            }
        }

        internal static bool HasSprite(Button button)
        {
            Image icon = button == null ? null : button.GetComponent<Image>();
            return icon != null && icon.sprite != null;
        }

        internal static void ApplySprite(Button button, Sprite sprite)
        {
            if (button == null || sprite == null)
            {
                return;
            }

            Image icon = button.GetComponent<Image>();

            if (icon == null)
            {
                return;
            }

            icon.sprite = sprite;
            icon.color = new Color(1f, 1f, 1f, InactiveAlpha);
        }

        internal static void SetActive(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Image icon = button.GetComponent<Image>();

            if (icon == null || icon.sprite == null)
            {
                return;
            }

            Color color = icon.color;
            color.a = active ? ActiveAlpha : InactiveAlpha;
            icon.color = color;
        }
    }
}
