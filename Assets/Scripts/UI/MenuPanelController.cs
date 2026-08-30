using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardGame.UI
{
    // Shared pause/menu popup used in both the Logo and CombatPrototype scenes. "Restart Game" only
    // does something if another script (RunManager, in the game scene) subscribes to
    // OnRestartRequested - in the Logo scene nothing subscribes, so the button is a no-op there.
    // "Return to Title" always reloads the Logo scene, from either scene.
    public class MenuPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private bool openWithQKey;

        public event Action OnRestartRequested;

        private void Awake()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (restartButton != null) restartButton.onClick.AddListener(() => OnRestartRequested?.Invoke());
            if (returnToTitleButton != null) returnToTitleButton.onClick.AddListener(ReturnToTitle);
        }

        private void Update()
        {
            if (openWithQKey && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                Open();
        }

        public void Open()
        {
            if (menuRoot != null) menuRoot.SetActive(true);
        }

        public void Close()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
        }

        private void ReturnToTitle()
        {
            SceneManager.LoadScene("Logo");
        }
    }
}
