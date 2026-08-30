using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Cards;
using CardGame.Combat;

namespace CardGame.UI
{
    // Shop screen shown once after the mid-boss stage. Sells a health potion (heals immediately,
    // never enters the deck) and a handful of random cards from the pool (each buyable once).
    // Left via the continue button whenever the player is done spending.
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private CombatController combatController;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private RectTransform cardButtonContainer;
        [SerializeField] private Button potionButton;
        [SerializeField] private Text potionButtonLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private int potionHealAmount = 15;
        [SerializeField] private int potionPrice = 15;
        [SerializeField] private int cardOptionCount = 4;
        [SerializeField] private int goldPerCardCost = 20;

        private readonly List<GameObject> _spawnedCards = new List<GameObject>();
        private readonly List<CardData> _cardOptions = new List<CardData>();
        private readonly HashSet<CardData> _boughtCards = new HashSet<CardData>();

        private Func<int> _getGold;
        private Action<int> _spendGold;
        private Action<CardData> _onCardBought;
        private Action _onClose;

        private void Awake()
        {
            potionButton.onClick.AddListener(BuyPotion);
            continueButton.onClick.AddListener(Close);
        }

        public void Open(IReadOnlyList<CardData> pool, Func<int> getGold, Action<int> spendGold, Action<CardData> onCardBought, Action onClose)
        {
            _getGold = getGold;
            _spendGold = spendGold;
            _onCardBought = onCardBought;
            _onClose = onClose;

            _boughtCards.Clear();
            _cardOptions.Clear();
            var options = new List<CardData>(pool);
            DeckRuntime.Shuffle(options);
            int count = Mathf.Min(cardOptionCount, options.Count);
            for (int i = 0; i < count; i++)
                _cardOptions.Add(options[i]);

            popupRoot.SetActive(true);
            Refresh();
        }

        private void BuyPotion()
        {
            if (_getGold() < potionPrice) return;
            if (combatController.PlayerCurrentHP >= combatController.PlayerMaxHP) return;

            _spendGold(potionPrice);
            combatController.HealPlayer(potionHealAmount);
            Refresh();
        }

        private void BuyCard(CardData card)
        {
            int price = CardPrice(card);
            if (_boughtCards.Contains(card) || _getGold() < price) return;

            _spendGold(price);
            _boughtCards.Add(card);
            _onCardBought?.Invoke(card);
            Refresh();
        }

        private int CardPrice(CardData card)
        {
            return goldPerCardCost * Mathf.Max(1, card.cost);
        }

        private void Refresh()
        {
            int gold = _getGold();
            if (statusText != null)
                statusText.text = $"보유 골드: {gold}   HP {combatController.PlayerCurrentHP}/{combatController.PlayerMaxHP}";

            bool hpFull = combatController.PlayerCurrentHP >= combatController.PlayerMaxHP;
            potionButton.interactable = gold >= potionPrice && !hpFull;
            if (potionButtonLabel != null)
                potionButtonLabel.text = $"체력 물약 (+{potionHealAmount})\n{potionPrice} 골드";

            foreach (var go in _spawnedCards)
                Destroy(go);
            _spawnedCards.Clear();

            foreach (var card in _cardOptions)
            {
                bool bought = _boughtCards.Contains(card);
                int price = CardPrice(card);
                bool affordable = !bought && gold >= price;

                GameObject entry = CardButtonFactory.Create(card, new Vector2(150, 190), affordable, false, () => BuyCard(card));
                Text label = entry.transform.Find("Label").GetComponent<Text>();
                label.fontSize = 14;
                label.text += bought ? "\n\n[구매완료]" : $"\n\n{price} 골드";

                entry.transform.SetParent(cardButtonContainer, false);
                _spawnedCards.Add(entry);
            }
        }

        private void Close()
        {
            popupRoot.SetActive(false);
            foreach (var go in _spawnedCards)
                Destroy(go);
            _spawnedCards.Clear();

            var callback = _onClose;
            _onClose = null;
            callback?.Invoke();
        }
    }
}
