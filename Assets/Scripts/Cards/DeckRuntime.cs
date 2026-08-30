using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Cards
{
    // Plain runtime deck state: draw pile / hand / discard pile. Not a MonoBehaviour -
    // owned and driven by whatever controller runs the battle.
    public class DeckRuntime
    {
        public readonly List<CardData> DrawPile = new List<CardData>();
        public readonly List<CardData> Hand = new List<CardData>();
        public readonly List<CardData> DiscardPile = new List<CardData>();

        public DeckRuntime(IEnumerable<CardData> startingCards)
        {
            DrawPile.AddRange(startingCards);
            Shuffle(DrawPile);
        }

        public static void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Reshuffles the discard pile back in when the draw pile runs dry, like most deckbuilders.
        public void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0) return;
                    DrawPile.AddRange(DiscardPile);
                    DiscardPile.Clear();
                    Shuffle(DrawPile);
                }

                int lastIndex = DrawPile.Count - 1;
                CardData card = DrawPile[lastIndex];
                DrawPile.RemoveAt(lastIndex);
                Hand.Add(card);
            }
        }

        // Exhausting cards (e.g. Catalyst) are removed from the deck for the rest of the battle
        // instead of going to the discard pile.
        public void PlayCard(CardData card)
        {
            Hand.Remove(card);
            if (!card.exhausts)
                DiscardPile.Add(card);
        }

        // Discards the hand at end of turn, keeping the first `retainCount` cards (Retain effect)
        // in hand for next turn instead of discarding them.
        public void DiscardHand(int retainCount = 0)
        {
            int keep = Mathf.Min(Mathf.Max(0, retainCount), Hand.Count);
            for (int i = Hand.Count - 1; i >= keep; i--)
            {
                DiscardPile.Add(Hand[i]);
                Hand.RemoveAt(i);
            }
        }
    }
}
