using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Cards;
using CardGame.Combat;

namespace CardGame.UI
{
    // Read-only popup listing every card currently in the run's deck (draw pile + hand + discard
    // combined), grouped by card so duplicates show as "찌르기 x3" instead of one line each.
    public class DeckViewController : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void Awake()
        {
            openButton.onClick.AddListener(Show);
            closeButton.onClick.AddListener(Hide);
        }

        private void Show()
        {
            Render();
            popupRoot.SetActive(true);
        }

        private void Hide()
        {
            popupRoot.SetActive(false);
        }

        private void Render()
        {
            Clear();

            var counts = new Dictionary<CardData, int>();
            var order = new List<CardData>();
            foreach (var card in runManager.CurrentDeck)
            {
                if (!counts.ContainsKey(card))
                {
                    counts[card] = 0;
                    order.Add(card);
                }
                counts[card]++;
            }

            foreach (var card in order)
            {
                var entry = CardButtonFactory.Create(card, new Vector2(560, 54), true, false, null);
                var button = entry.GetComponent<Button>();
                button.interactable = false;

                var text = entry.transform.Find("Label").GetComponent<Text>();
                text.text = $"{card.displayName} x{counts[card]}   [{card.cost}] {card.GetStatsText()}\n{card.description}";
                text.alignment = TextAnchor.MiddleLeft;

                entry.transform.SetParent(listContainer, false);
                _spawned.Add(entry);
            }
        }

        private void Clear()
        {
            foreach (var go in _spawned)
                Destroy(go);
            _spawned.Clear();
        }
    }
}
