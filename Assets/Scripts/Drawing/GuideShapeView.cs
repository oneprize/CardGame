using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Drawing
{
    [RequireComponent(typeof(LineRenderer))]
    public class GuideShapeView : MonoBehaviour
    {
        [SerializeField] private float worldSize = 3f;
        [SerializeField] private int resolution = 80;

        private LineRenderer _line;
        private readonly List<Vector2> _worldPoints = new List<Vector2>();

        public IReadOnlyList<Vector2> WorldPoints => _worldPoints;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
        }

        public void ShowShape(GuideShapeType type)
        {
            var localPoints = GuideShapeLibrary.Generate(type, resolution);
            _worldPoints.Clear();
            _line.positionCount = localPoints.Count;

            for (int i = 0; i < localPoints.Count; i++)
            {
                Vector3 worldPos = transform.TransformPoint(localPoints[i] * worldSize);
                _line.SetPosition(i, worldPos);
                _worldPoints.Add(worldPos);
            }
        }

        public void Hide()
        {
            _line.positionCount = 0;
            _worldPoints.Clear();
        }
    }
}
