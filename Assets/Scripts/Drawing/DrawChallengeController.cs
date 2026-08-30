using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Drawing
{
    // Runs one on-demand drawing challenge (as opposed to DrawingRoundController, which auto-cycles
    // a fixed shape sequence for the M1 standalone demo). A caller supplies the shape/time/threshold
    // per attempt - this is what lets combat drive the challenge per card.
    //
    // Shown as a real UI popup (white card panel, drawn with the *UI counterparts of the guide/input
    // components) instead of a dim over a world-space LineRenderer - a UI Image/Panel would otherwise
    // render on top of and hide a world-space line, since Screen Space - Overlay canvases always
    // composite after the camera's scene render.
    public class DrawChallengeController : MonoBehaviour
    {
        [SerializeField] private GuideShapeViewUI guideView;
        [SerializeField] private DrawInputCaptureUI inputCapture;

        [Header("Popup")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text timerText;

        private static readonly Color DefaultInstructionColor = new Color(0.15f, 0.16f, 0.2f, 1f);
        private static readonly Color SuccessColor = new Color(0.15f, 0.55f, 0.25f, 1f);
        private static readonly Color FailureColor = new Color(0.75f, 0.2f, 0.2f, 1f);
        private const float ResultFlashSeconds = 0.6f;

        public event Action<float> OnTimeRemainingChanged;

        private Action<bool, float> _onComplete;
        private float _timeRemaining;
        private float _successThreshold;
        private bool _active;

        public bool IsActive => _active;

        private void Awake()
        {
            inputCapture.OnDrawEnded += HandleDrawEnded;
        }

        private void OnDestroy()
        {
            inputCapture.OnDrawEnded -= HandleDrawEnded;
        }

        public void BeginChallenge(GuideShapeType shape, float timeLimit, float successThreshold, Action<bool, float> onComplete, string cardLabel = null)
        {
            _onComplete = onComplete;
            _successThreshold = successThreshold;

            if (popupRoot != null) popupRoot.SetActive(true);

            guideView.ShowShape(shape);
            inputCapture.ClearVisual();
            inputCapture.SetInputEnabled(true);

            if (instructionText != null)
            {
                instructionText.text = string.IsNullOrEmpty(cardLabel) ? "가이드를 따라 그리세요" : $"{cardLabel} - 가이드를 따라 그리세요";
                instructionText.color = DefaultInstructionColor;
            }

            _timeRemaining = timeLimit;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;

            _timeRemaining -= Time.deltaTime;
            float remaining = Mathf.Max(_timeRemaining, 0f);
            OnTimeRemainingChanged?.Invoke(remaining);
            if (timerText != null)
                timerText.text = $"{remaining:0.0}s";

            if (_timeRemaining <= 0f)
                Finish(false, 0f);
        }

        private void HandleDrawEnded(List<Vector2> drawnPoints)
        {
            if (!_active) return;
            float score = ShapeMatcher.Score(guideView.Points, drawnPoints);
            Finish(score >= _successThreshold, score);
        }

        // Flashes a clear 성공/실패 result inside the popup itself for a beat before closing,
        // instead of just relying on the battle log line at the bottom of the screen.
        private void Finish(bool success, float score)
        {
            _active = false;
            inputCapture.SetInputEnabled(false);
            guideView.Hide();

            if (instructionText != null)
            {
                instructionText.text = success ? $"성공! (싱크율 {score:0.0}%)" : "실패...";
                instructionText.color = success ? SuccessColor : FailureColor;
            }
            if (timerText != null)
                timerText.text = string.Empty;

            var callback = _onComplete;
            _onComplete = null;
            StartCoroutine(ClosePopupAfterDelay(success, score, callback));
        }

        private IEnumerator ClosePopupAfterDelay(bool success, float score, Action<bool, float> callback)
        {
            yield return new WaitForSeconds(ResultFlashSeconds);
            if (popupRoot != null) popupRoot.SetActive(false);
            callback?.Invoke(success, score);
        }
    }
}
