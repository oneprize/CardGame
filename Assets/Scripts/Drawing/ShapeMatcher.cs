using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Drawing
{
    // Compares a player-drawn stroke against a guide shape and returns a 0~100 similarity score.
    public static class ShapeMatcher
    {
        public static List<Vector2> Resample(IReadOnlyList<Vector2> points, int targetCount)
        {
            var result = new List<Vector2>(targetCount);

            if (points == null || points.Count == 0 || targetCount < 2)
                return result;

            if (points.Count == 1)
            {
                for (int i = 0; i < targetCount; i++) result.Add(points[0]);
                return result;
            }

            float totalLength = 0f;
            for (int i = 1; i < points.Count; i++)
                totalLength += Vector2.Distance(points[i - 1], points[i]);

            if (totalLength <= Mathf.Epsilon)
            {
                for (int i = 0; i < targetCount; i++) result.Add(points[0]);
                return result;
            }

            float interval = totalLength / (targetCount - 1);
            result.Add(points[0]);

            float accumulated = 0f;
            int srcIndex = 1;
            Vector2 prev = points[0];

            while (result.Count < targetCount && srcIndex < points.Count)
            {
                Vector2 current = points[srcIndex];
                float segmentLength = Vector2.Distance(prev, current);

                if (segmentLength <= Mathf.Epsilon)
                {
                    srcIndex++;
                    continue;
                }

                if (accumulated + segmentLength >= interval)
                {
                    float remaining = interval - accumulated;
                    float t = remaining / segmentLength;
                    Vector2 newPoint = Vector2.Lerp(prev, current, t);
                    result.Add(newPoint);
                    prev = newPoint;
                    accumulated = 0f;
                }
                else
                {
                    accumulated += segmentLength;
                    prev = current;
                    srcIndex++;
                }
            }

            while (result.Count < targetCount)
                result.Add(points[points.Count - 1]);

            return result;
        }

        // Centers on centroid and uniformly scales so the longest bounding-box side becomes 1.
        // Uniform (not per-axis) scaling keeps aspect ratio, so a squashed shape can't cheat a match.
        public static List<Vector2> Normalize(IReadOnlyList<Vector2> points)
        {
            var result = new List<Vector2>(points.Count);
            if (points.Count == 0) return result;

            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < points.Count; i++) centroid += points[i];
            centroid /= points.Count;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i] - centroid;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            float size = Mathf.Max(Mathf.Max(maxX - minX, maxY - minY), 0.0001f);

            for (int i = 0; i < points.Count; i++)
                result.Add((points[i] - centroid) / size);

            return result;
        }

        public static float PathLength(IReadOnlyList<Vector2> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
                length += Vector2.Distance(points[i - 1], points[i]);
            return length;
        }

        // distanceTolerance is the average normalized point distance treated as a 0% match.
        // Tune this after playtesting M1 - it is the main knob controlling how forgiving 80% feels.
        //
        // Also searches every cyclic shift of the drawn points (plus reversed) before scoring.
        // This makes closed shapes (e.g. a circle) start-point independent: a circle traced from
        // any point on its rim can still line up with a guide that starts elsewhere. For open
        // shapes (e.g. a line) a shifted comparison folds the path back on itself, so it does not
        // produce false positives - only the near-zero shift stays competitive.
        //
        // minCoverageRatio guards against a short partial stroke: since normalization rescales
        // both strokes to the same bounding box, drawing only half of a line would otherwise
        // stretch to fill the box and score as a perfect match. Comparing raw (pre-normalize)
        // path length - guide and drawn share the same world scale - catches that before it can.
        public static float Score(IReadOnlyList<Vector2> guide, IReadOnlyList<Vector2> drawn, int sampleCount = 64, float distanceTolerance = 0.35f, float minCoverageRatio = 0.75f)
        {
            if (guide == null || drawn == null || guide.Count < 2 || drawn.Count < 2)
                return 0f;

            var normalizedGuide = Normalize(Resample(guide, sampleCount));
            var normalizedDrawn = Normalize(Resample(drawn, sampleCount));
            var reversedDrawn = new List<Vector2>(normalizedDrawn);
            reversedDrawn.Reverse();

            float bestAverageDistance = float.MaxValue;
            for (int shift = 0; shift < sampleCount; shift++)
            {
                bestAverageDistance = Mathf.Min(bestAverageDistance, AverageDistance(normalizedGuide, normalizedDrawn, shift));
                bestAverageDistance = Mathf.Min(bestAverageDistance, AverageDistance(normalizedGuide, reversedDrawn, shift));
            }

            float similarity = 1f - (bestAverageDistance / distanceTolerance);
            float shapeScore = Mathf.Clamp01(similarity) * 100f;

            float guideLength = PathLength(guide);
            float drawnLength = PathLength(drawn);
            float coverageRatio = guideLength > Mathf.Epsilon ? drawnLength / guideLength : 1f;
            float coverageFactor = Mathf.Clamp01(coverageRatio / minCoverageRatio);

            return shapeScore * coverageFactor;
        }

        private static float AverageDistance(List<Vector2> guide, List<Vector2> drawn, int shift)
        {
            int count = Mathf.Min(guide.Count, drawn.Count);
            float total = 0f;
            for (int i = 0; i < count; i++)
                total += Vector2.Distance(guide[i], drawn[(i + shift) % count]);
            return total / count;
        }
    }
}
