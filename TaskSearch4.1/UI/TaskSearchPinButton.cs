using System;
using EFT.UI;
using HarmonyLib;
using TaskSearch.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSearch.UI
{
    internal static class TaskSearchPinButton
    {
        private const float Size = 22f;
        private const float RightMargin = 36f;

        internal static Button Create(TMPro.TMP_InputField field, Action onClick)
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

        internal static bool TryReadSprites(
            TasksPanel panel,
            out Sprite offSprite, out Color offColor,
            out Sprite onSprite, out Color onColor)
        {
            offSprite = null;
            onSprite = null;
            offColor = Color.white;
            onColor = Color.white;

            if (panel == null)
            {
                return false;
            }

            try
            {
                NotesTaskFavoriteButton favorite =
                    panel.GetComponentInChildren<NotesTaskFavoriteButton>(true);

                if (favorite == null)
                {
                    return false;
                }

                GameObject notRoot =
                    AccessTools.Field(typeof(NotesTaskFavoriteButton), "_notFavoriteRoot")
                        ?.GetValue(favorite) as GameObject;
                GameObject favRoot =
                    AccessTools.Field(typeof(NotesTaskFavoriteButton), "_favoriteRoot")
                        ?.GetValue(favorite) as GameObject;

                bool gotOff = ReadGlyph(notRoot, out offSprite, out offColor);
                bool gotOn = ReadGlyph(favRoot, out onSprite, out onColor);

                return gotOff && gotOn;
            }
            catch (Exception ex)
            {
                TaskSearchLog.WarnOnce("pin-sprite", "Could not read the in-game pin icon: " + ex.Message);
                return false;
            }
        }

        private static bool ReadGlyph(GameObject root, out Sprite sprite, out Color color)
        {
            sprite = null;
            color = Color.white;

            if (root == null)
            {
                return false;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            Image fallback = null;

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];

                if (image == null || image.sprite == null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = image;
                }

                if (image.type != Image.Type.Sliced)
                {
                    sprite = image.sprite;
                    color = image.color;
                    return true;
                }
            }

            if (fallback != null)
            {
                sprite = fallback.sprite;
                color = fallback.color;
                return true;
            }

            return false;
        }

        internal static void SetState(Button button, Sprite sprite, Color color)
        {
            if (button == null)
            {
                return;
            }

            Image icon = button.GetComponent<Image>();

            if (icon == null)
            {
                return;
            }

            icon.sprite = sprite;
            icon.color = sprite == null ? new Color(1f, 1f, 1f, 0f) : color;
        }
    }
}
