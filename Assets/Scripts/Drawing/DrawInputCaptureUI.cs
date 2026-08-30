using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CardGame.UI;

namespace CardGame.Drawing
{
    // UI counterpart to DrawInputCapture. Same polling approach (checks Mouse.current every frame
    // rather than relying on Unity UI drag events, so a drag that leaves the popup bounds keeps
    // tracking normally) but converts screen position into the drawArea's local rect space instead
    // of world space, since the popup lives entirely in a Screen Space - Overlay canvas.
    public class DrawInputCaptureUI : MonoBehaviour
    {
        [SerializeField] private RectTransform drawArea;
        [SerializeField] private UILineRenderer lineRenderer;
        [SerializeField] private float minPointDistance = 4f;

        public event Action OnDrawStarted;
        public event Action<List<Vector2>> OnDrawEnded;

        private readonly List<Vector2> _points = new List<Vector2>();
        private bool _isDrawing;

        public bool IsDrawing => _isDrawing;

        public void SetInputEnabled(bool value)
        {
            enabled = value;
            if (!value) CancelDraw();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();

            if (!_isDrawing && mouse.leftButton.wasPressedThisFrame)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(drawArea, screenPos, null))
                    BeginDraw(screenPos);
            }
            else if (_isDrawing && mouse.leftButton.isPressed)
            {
                ContinueDraw(screenPos);
            }
            else if (_isDrawing && mouse.leftButton.wasReleasedThisFrame)
            {
                EndDraw();
            }
        }

        private bool ScreenToLocal(Vector2 screenPos, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea, screenPos, null, out local);
        }

        private void BeginDraw(Vector2 screenPos)
        {
            if (!ScreenToLocal(screenPos, out Vector2 local)) return;

            _isDrawing = true;
            _points.Clear();
            _points.Add(local);
            lineRenderer.SetPoints(_points);
            OnDrawStarted?.Invoke();
        }

        private void ContinueDraw(Vector2 screenPos)
        {
            if (!ScreenToLocal(screenPos, out Vector2 local)) return;
            if (Vector2.Distance(local, _points[_points.Count - 1]) < minPointDistance) return;

            _points.Add(local);
            lineRenderer.SetPoints(_points);
        }

        private void EndDraw()
        {
            _isDrawing = false;
            OnDrawEnded?.Invoke(new List<Vector2>(_points));
        }

        public void CancelDraw()
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            _points.Clear();
            lineRenderer.Clear();
        }

        public void ClearVisual()
        {
            _points.Clear();
            lineRenderer.Clear();
        }
    }
}
