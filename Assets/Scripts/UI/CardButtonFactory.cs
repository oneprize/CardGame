using System;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Cards;

namespace CardGame.UI
{
    // Shared by HandView (small, no description) and RewardController (bigger, with description) -
    // both just need "a clickable rectangle summarizing a CardData".
    public static class CardButtonFactory
    {
        private static Font _koreanFont;
        private static Font KoreanFont => _koreanFont != null ? _koreanFont : (_koreanFont = Resources.Load<Font>("Fonts/JayeonSans-Regular"));

        public static GameObject Create(CardData card, Vector2 size, bool interactable, bool showDescription, Action onClick)
        {
            var root = new GameObject(card.displayName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(RectMask2D));

            var layoutElement = root.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            // Lets a HorizontalLayoutGroup with childControlWidth/Height on shrink cards toward
            // this floor (instead of overflowing the container) when there are too many to fit.
            layoutElement.minWidth = size.x * 0.25f;
            layoutElement.minHeight = size.y * 0.6f;

            var image = root.GetComponent<Image>();
            image.color = interactable ? new Color(0.25f, 0.32f, 0.42f) : new Color(0.15f, 0.15f, 0.15f);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(root.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8, 8);
            labelRect.offsetMax = new Vector2(-8, -8);

            var text = label.GetComponent<Text>();
            string body = $"{card.displayName}\n[{card.cost}] {card.GetStatsText()}";
            if (showDescription && !string.IsNullOrEmpty(card.description))
                body += $"\n\n{card.description}";
            text.text = body;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = KoreanFont;
            text.fontSize = 16;
            text.color = Color.white;
            // When a crowded hand shrinks this button below its preferred size, shrink the font
            // to match instead of clipping/wrapping unreadably.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = 16;

            var button = root.GetComponent<Button>();
            button.interactable = interactable;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            return root;
        }
    }
}
