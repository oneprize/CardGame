using System.Collections.Generic;
using UnityEngine;
using CardGame.UI;

namespace CardGame.Drawing
{
    // UI counterpart to GuideShapeView (which draws in world space for the M1 standalone demo).
    // Generates the same shapes from GuideShapeLibrary but scales them in UI pixels and renders
    // them with a UILineRenderer so they live inside a Canvas popup.
    public class GuideShapeViewUI : MonoBehaviour
    {
        [SerializeField] private UILineRenderer lineRenderer;
        [SerializeField] private float uiSize = 130f;
        [SerializeField] private int resolution = 80;

        private readonly List<Vector2> _points = new List<Vector2>();

        public IReadOnlyList<Vector2> Points => _points;

        public void ShowShape(GuideShapeType type)
        {
            var localPoints = GuideShapeLibrary.Generate(type, resolution);
            _points.Clear();
            foreach (var p in localPoints)
                _points.Add(p * uiSize);

            lineRenderer.SetPoints(_points);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            lineRenderer.Clear();
            _points.Clear();
            gameObject.SetActive(false);
        }
    }
}
