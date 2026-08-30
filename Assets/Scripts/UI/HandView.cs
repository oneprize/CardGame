using System;
using System.Collections.Generic;
using UnityEngine;
using CardGame.Cards;

namespace CardGame.UI
{
    // Cards have no art yet, so each hand entry is just a plain button built from text.
    public class HandView : MonoBehaviour
    {
        [SerializeField] private RectTransform handContainer;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public void Render(IReadOnlyList<CardData> hand, int currentEnergy, Action<CardData> onCardClicked)
        {
            Clear();
            foreach (var card in hand)
            {
                bool affordable = currentEnergy >= card.cost;
                GameObject entry = CardButtonFactory.Create(card, new Vector2(150, 90), affordable, false, () => onCardClicked(card));
                entry.transform.SetParent(handContainer, false);
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
