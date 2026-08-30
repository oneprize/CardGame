using System;
using UnityEngine;
using UnityEngine.UI;
using CardGame.Combat;

namespace CardGame.UI
{
    // Rest-site screen shown once before the final boss stage. One-time full heal, then continue.
    public class BonfireController : MonoBehaviour
    {
        [SerializeField] private CombatController combatController;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text hpText;
        [SerializeField] private Button restButton;
        [SerializeField] private Button continueButton;

        private Action _onClose;
        private bool _rested;

        private void Awake()
        {
            restButton.onClick.AddListener(Rest);
            continueButton.onClick.AddListener(Close);
        }

        public void Open(Action onClose)
        {
            _onClose = onClose;
            _rested = false;
            popupRoot.SetActive(true);
            Refresh();
        }

        private void Rest()
        {
            if (_rested) return;
            _rested = true;
            combatController.HealPlayer(combatController.PlayerMaxHP);
            Refresh();
        }

        private void Refresh()
        {
            if (hpText != null)
                hpText.text = $"HP {combatController.PlayerCurrentHP}/{combatController.PlayerMaxHP}";
            restButton.interactable = !_rested;
        }

        private void Close()
        {
            popupRoot.SetActive(false);
            var callback = _onClose;
            _onClose = null;
            callback?.Invoke();
        }
    }
}
