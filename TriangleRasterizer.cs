using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_IMBANIKA_TEXTURER
{
    public static class TriangleRasterizer
    {
        public static void Rasterize(Vector2 v0, Vector2 v1, Vector2 v2, Action<int, int> processPixel)
        {
            // Sort vertices by Y (v0.Y <= v1.Y <= v2.Y)
            if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
            if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
            if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

            // Compute integer Y range that covers all pixels touched by the triangle
            int yMin = (int)MathF.Floor(v0.Y);
            int yMax = (int)MathF.Floor(v2.Y);

            // Precompute edge vectors for convenience
            Vector2 e0 = v1 - v0;
            Vector2 e1 = v2 - v1;
            Vector2 e2 = v0 - v2; // actually v2 - v0? Let's keep consistent: edges v0->v1, v1->v2, v2->v0
                                  // We'll need edges for intersection tests: (v0,v1), (v1,v2), (v2,v0)
                                  // So define them as pairs
            (Vector2 a, Vector2 b)[] edges = new (Vector2, Vector2)[]
            {
            (v0, v1),
            (v1, v2),
            (v2, v0)
            };

            // For each scanline (pixel row)
            for (int y = yMin; y <= yMax; y++)
            {
                float lower = y;          // bottom of pixel cell
                float upper = y + 1;      // top of pixel cell

                // Collect all X coordinates where the triangle intersects the horizontal strip [y, y+1]
                List<float> xCandidates = new List<float>();

                // 1. Check vertices inside the strip
                if (v0.Y >= lower && v0.Y <= upper) xCandidates.Add(v0.X);
                if (v1.Y >= lower && v1.Y <= upper) xCandidates.Add(v1.X);
                if (v2.Y >= lower && v2.Y <= upper) xCandidates.Add(v2.X);

                // 2. Check intersections of edges with the lower and upper boundaries
                foreach (var edge in edges)
                {
                    Vector2 p1 = edge.Item1;
                    Vector2 p2 = edge.Item2;

                    // Edge from p1 to p2
                    // Check intersection with lower line y = lower
                    if (p1.Y != p2.Y) // non-horizontal edge
                    {
                        float tLower = (lower - p1.Y) / (p2.Y - p1.Y);
                        if (tLower >= 0 && tLower <= 1)
                        {
                            float xLower = p1.X + tLower * (p2.X - p1.X);
                            xCandidates.Add(xLower);
                        }

                        // Check intersection with upper line y = upper
                        float tUpper = (upper - p1.Y) / (p2.Y - p1.Y);
                        if (tUpper >= 0 && tUpper <= 1)
                        {
                            float xUpper = p1.X + tUpper * (p2.X - p1.X);
                            xCandidates.Add(xUpper);
                        }
                    }
                    else
                    {
                        // Horizontal edge: if its Y lies inside the strip, the whole segment is inside.
                        // In that case the x-range of the edge is relevant, but we handle it via the
                        // endpoints already (vertices). If the edge is exactly on lower or upper,
                        // the vertices are included. If it lies strictly inside, the interior of the
                        // triangle will be captured by other edges. So we can skip adding anything extra.
                    }
                }

                if (xCandidates.Count == 0)
                    continue; // no intersection with this pixel row

                float xMin = float.MaxValue;
                float xMax = float.MinValue;
                foreach (var x in xCandidates)
                {
                    if (x < xMin) xMin = x;
                    if (x > xMax) xMax = x;
                }

                // Now all pixels whose column interval [x, x+1] overlaps [xMin, xMax] are touched.
                int startX = (int)MathF.Floor(xMin);
                int endX = (int)MathF.Floor(xMax);
                for (int x = startX; x <= endX; x++)
                {
                    processPixel(x, y);
                }
            }
        }
    }
}
