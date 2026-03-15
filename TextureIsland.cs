using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Numerics;

namespace RIKA_IMBANIKA_TEXTURER
{
    public class TextureIsland
    {
        public List<int> FaceIndices { get; } = new();
        public List<Vector2> UVs { get; } = new();
        public List<(int, int, int)> Triangles { get; } = new();
        public Rect Bounds { get; private set; }
        public Dictionary<int, int> GlobalUvToLocal { get; } = new();

        public void CalculateBounds()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var uv in UVs)
            {
                minX = Math.Min(minX, uv.X);
                maxX = Math.Max(maxX, uv.X);
                minY = Math.Min(minY, uv.Y);
                maxY = Math.Max(maxY, uv.Y);
            }

            Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static bool IsInside(Vector2 center, float radius, List<Vector2> polygon)
        {
            const float Epsilon = 1e-6f;

            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % n];
                float dist = DistanceToSegment(center, a, b);
                if (dist < radius - Epsilon)
                    return false;
            }

            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % n];
                area += a.X * b.Y - b.X * a.Y;
            }
            bool isClockwise = area < 0;

            int winding = WindingNumber(center, polygon);

            return isClockwise ? winding != 0 : winding == 0;

            int WindingNumber(Vector2 point, List<Vector2> polygon)
            {
                int winding = 0;
                int n = polygon.Count;

                for (int i = 0; i < n; i++)
                {
                    Vector2 a = polygon[i];
                    Vector2 b = polygon[(i + 1) % n];

                    if (a.Y <= point.Y)
                    {
                        if (b.Y > point.Y && IsLeft(a, b, point) > 0)
                            winding++;
                    }
                    else
                    {
                        if (b.Y <= point.Y && IsLeft(a, b, point) < 0)
                            winding--;
                    }
                }

                return winding;
            }

            float IsLeft(Vector2 a, Vector2 b, Vector2 p)
            {
                return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
            }

            float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 ab = b - a;
                Vector2 ap = p - a;

                float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
                t = Math.Clamp(t, 0f, 1f);

                Vector2 closest = a + t * ab;
                return Vector2.Distance(p, closest);
            }
        }
    }
}
