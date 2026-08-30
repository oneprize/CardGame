using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Cards;

namespace CardGame.UI
{
    // Shows 3 random cards from the pool after a win; the player picks one (added to the run's
    // deck) or skips. Reuses DeckRuntime.Shuffle just to pick a random sample without duplicates.
    public class RewardController : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private RectTransform cardButtonContainer;
        [SerializeField] private Button skipButton;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private Action<CardData> _onChosen;

        private void Awake()
        {
            skipButton.onClick.AddListener(() => Choose(null));
        }

        public void ShowReward(IReadOnlyList<CardData> pool, int optionCount, Action<CardData> onChosen)
        {
            _onChosen = onChosen;

            var options = new List<CardData>(pool);
            DeckRuntime.Shuffle(options);
            int count = Mathf.Min(optionCount, options.Count);

            Clear();
            for (int i = 0; i < count; i++)
            {
                CardData card = options[i];
                GameObject entry = CardButtonFactory.Create(card, new Vector2(170, 220), true, false, () => Choose(card));
                entry.transform.SetParent(cardButtonContainer, false);
                _spawned.Add(entry);
            }

            popupRoot.SetActive(true);
        }

        private void Choose(CardData card)
        {
            popupRoot.SetActive(false);
            Clear();

            var callback = _onChosen;
            _onChosen = null;
            callback?.Invoke(card);
        }

        private void Clear()
        {
            foreach (var go in _spawned)
                Destroy(go);
            _spawned.Clear();
        }
    }
}
