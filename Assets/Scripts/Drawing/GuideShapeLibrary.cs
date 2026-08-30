using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Drawing
{
    public enum GuideShapeType
    {
        Line,
        Circle,
        Star,
        Pentagon
    }

    // Generates unit-space (roughly -0.5..0.5) point lists for the basic guide shapes.
    public static class GuideShapeLibrary
    {
        public static List<Vector2> Generate(GuideShapeType type, int resolution = 64)
        {
            switch (type)
            {
                case GuideShapeType.Line:
                    return GenerateLine(resolution);
                case GuideShapeType.Circle:
                    return GenerateCircle(resolution);
                case GuideShapeType.Star:
                    return GeneratePolygon(BuildStarVertices(5, 0.5f, 0.225f), resolution);
                case GuideShapeType.Pentagon:
                    return GeneratePolygon(BuildRegularPolygonVertices(5, 0.5f), resolution);
                default:
                    return new List<Vector2>();
            }
        }

        private static List<Vector2> GenerateLine(int resolution)
        {
            var points = new List<Vector2>(resolution);
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);
                points.Add(new Vector2(Mathf.Lerp(-0.5f, 0.5f, t), 0f));
            }
            return points;
        }

        private static List<Vector2> GenerateCircle(int resolution)
        {
            var points = new List<Vector2>(resolution + 1);
            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                float angle = t * Mathf.PI * 2f + Mathf.PI * 0.5f;
                points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f);
            }
            return points;
        }

        private static List<Vector2> BuildRegularPolygonVertices(int sides, float radius)
        {
            var vertices = new List<Vector2>(sides);
            for (int i = 0; i < sides; i++)
            {
                float angle = (i / (float)sides) * Mathf.PI * 2f + Mathf.PI * 0.5f;
                vertices.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return vertices;
        }

        // Alternates outer/inner radius vertices to form a 5-pointed star outline.
        private static List<Vector2> BuildStarVertices(int points, float outerRadius, float innerRadius)
        {
            int total = points * 2;
            var vertices = new List<Vector2>(total);
            for (int i = 0; i < total; i++)
            {
                float angle = (i / (float)total) * Mathf.PI * 2f + Mathf.PI * 0.5f;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                vertices.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            return vertices;
        }

        // Walks the closed polygon defined by `vertices`, distributing `resolution` points evenly
        // by arc length around the perimeter so dense samples work the same as Circle's for matching.
        private static List<Vector2> GeneratePolygon(List<Vector2> vertices, int resolution)
        {
            var points = new List<Vector2>(resolution + 1);
            int vertexCount = vertices.Count;

            float perimeter = 0f;
            for (int i = 0; i < vertexCount; i++)
                perimeter += Vector2.Distance(vertices[i], vertices[(i + 1) % vertexCount]);

            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                float targetDistance = t * perimeter;

                float traveled = 0f;
                for (int v = 0; v < vertexCount; v++)
                {
                    Vector2 a = vertices[v];
                    Vector2 b = vertices[(v + 1) % vertexCount];
                    float edgeLength = Vector2.Distance(a, b);

                    if (traveled + edgeLength >= targetDistance || v == vertexCount - 1)
                    {
                        float edgeT = edgeLength > Mathf.Epsilon ? (targetDistance - traveled) / edgeLength : 0f;
                        points.Add(Vector2.Lerp(a, b, Mathf.Clamp01(edgeT)));
                        break;
                    }
                    traveled += edgeLength;
                }
            }

            return points;
        }
    }
}
