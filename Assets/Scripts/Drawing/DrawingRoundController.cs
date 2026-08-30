using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CardGame.Drawing
{
    public class DrawingRoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GuideShapeView guideView;
        [SerializeField] private DrawInputCapture inputCapture;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text resultText;

        [Header("Settings")]
        [SerializeField] private float timeLimitSeconds = 3f;
        [SerializeField] private float successThreshold = 80f;
        [SerializeField] private GuideShapeType[] shapeSequence = { GuideShapeType.Line, GuideShapeType.Circle };

        private int _shapeIndex;
        private float _timeRemaining;
        private bool _roundActive;
        private bool _resultShown;

        private void Awake()
        {
            inputCapture.OnDrawEnded += HandleDrawEnded;
        }

        private void OnDestroy()
        {
            inputCapture.OnDrawEnded -= HandleDrawEnded;
        }

        private void Start()
        {
            StartRound();
        }

        private void Update()
        {
            if (_roundActive)
            {
                _timeRemaining -= Time.deltaTime;
                if (timerText != null)
                    timerText.text = $"남은 시간: {Mathf.Max(_timeRemaining, 0f):0.0}s";

                if (_timeRemaining <= 0f)
                {
                    FailRound("시간 초과");
                }
            }
            else if (_resultShown)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame))
                {
                    NextRound();
                }
            }
        }

        private void StartRound()
        {
            _resultShown = false;
            if (resultText != null) resultText.text = string.Empty;

            var shape = shapeSequence[_shapeIndex % shapeSequence.Length];
            guideView.ShowShape(shape);
            inputCapture.ClearVisual();
            inputCapture.SetInputEnabled(true);

            _timeRemaining = timeLimitSeconds;
            _roundActive = true;

            if (instructionText != null)
                instructionText.text = $"가이드를 따라 그리세요 ({shape})";
        }

        private void HandleDrawEnded(List<Vector2> drawnPoints)
        {
            if (!_roundActive) return;
            EvaluateRound(drawnPoints);
        }

        private void EvaluateRound(List<Vector2> drawnPoints)
        {
            _roundActive = false;
            inputCapture.SetInputEnabled(false);

            float score = ShapeMatcher.Score(guideView.WorldPoints, drawnPoints);
            bool success = score >= successThreshold;

            ShowResult(success, score);
        }

        private void FailRound(string reason)
        {
            _roundActive = false;
            inputCapture.SetInputEnabled(false);
            ShowResult(false, 0f, reason);
        }

        private void ShowResult(bool success, float score, string reasonOverride = null)
        {
            _resultShown = true;
            string label = success ? "성공" : "실패";
            string reason = reasonOverride ?? $"싱크율 {score:0.0}%";

            if (resultText != null)
                resultText.text = $"{label} ({reason})\nSpace / R : 다음 도형";
        }

        private void NextRound()
        {
            _shapeIndex++;
            StartRound();
        }
    }
}
