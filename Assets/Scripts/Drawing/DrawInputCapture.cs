using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardGame.Drawing
{
    [RequireComponent(typeof(LineRenderer))]
    public class DrawInputCapture : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float minPointDistance = 0.08f;

        public event Action OnDrawStarted;
        public event Action<List<Vector2>> OnDrawEnded;

        private LineRenderer _line;
        private readonly List<Vector2> _points = new List<Vector2>();
        private bool _isDrawing;

        public bool IsDrawing => _isDrawing;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 0;
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void SetInputEnabled(bool value)
        {
            enabled = value;
            if (!value) CancelDraw();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (!_isDrawing && mouse.leftButton.wasPressedThisFrame)
            {
                BeginDraw(mouse.position.ReadValue());
            }
            else if (_isDrawing && mouse.leftButton.isPressed)
            {
                ContinueDraw(mouse.position.ReadValue());
            }
            else if (_isDrawing && mouse.leftButton.wasReleasedThisFrame)
            {
                EndDraw();
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            float distanceFromCamera = -targetCamera.transform.position.z;
            Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));
            return world;
        }

        private void BeginDraw(Vector2 screenPos)
        {
            _isDrawing = true;
            _points.Clear();
            Vector2 world = ScreenToWorld(screenPos);
            _points.Add(world);
            _line.positionCount = 1;
            _line.SetPosition(0, world);
            OnDrawStarted?.Invoke();
        }

        private void ContinueDraw(Vector2 screenPos)
        {
            Vector2 world = ScreenToWorld(screenPos);
            if (Vector2.Distance(world, _points[_points.Count - 1]) < minPointDistance)
                return;

            _points.Add(world);
            _line.positionCount = _points.Count;
            _line.SetPosition(_points.Count - 1, world);
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
            _line.positionCount = 0;
        }

        public void ClearVisual()
        {
            _points.Clear();
            _line.positionCount = 0;
        }
    }
}
