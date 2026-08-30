using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.UI
{
    // Draws a polyline inside a UI hierarchy by stretching a plain Image between each pair of
    // consecutive points and rotating it to match. Lets the guide/stroke render as normal UI
    // graphics (inside a Canvas, above a white popup panel) instead of a world-space LineRenderer,
    // which a UI panel would otherwise draw over.
    public class UILineRenderer : MonoBehaviour
    {
        [SerializeField] private float thickness = 5f;
        [SerializeField] private Color color = Color.black;

        private readonly List<RectTransform> _segments = new List<RectTransform>();

        public void SetPoints(IReadOnlyList<Vector2> localPoints)
        {
            int neededSegments = Mathf.Max(0, localPoints.Count - 1);
            EnsureSegmentCount(neededSegments);

            for (int i = 0; i < neededSegments; i++)
            {
                Vector2 a = localPoints[i];
                Vector2 b = localPoints[i + 1];
                Vector2 diff = b - a;
                float distance = diff.magnitude;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                RectTransform segment = _segments[i];
                segment.gameObject.SetActive(true);
                segment.anchoredPosition = a + diff * 0.5f;
                segment.sizeDelta = new Vector2(Mathf.Max(distance, 0.01f), thickness);
                segment.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            for (int i = neededSegments; i < _segments.Count; i++)
                _segments[i].gameObject.SetActive(false);
        }

        public void Clear()
        {
            foreach (var segment in _segments)
                segment.gameObject.SetActive(false);
        }

        private void EnsureSegmentCount(int count)
        {
            while (_segments.Count < count)
            {
                var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var image = go.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;

                _segments.Add(rect);
            }
        }
    }
}
