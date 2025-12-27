using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public static class CircleToPointSolver
    {
        private static int maxIterations = 500;
        private static ushort _island;

        public static Vector2? FindCenterPoint(Vector2 dir, Vector2 point, ushort island)
        {
            _island = island;
            // F(t) = t - R(point + t*dir)
            // Need to find t > 0 such that F(t) >= 0 (distance <= radius)

            // Find initial interval [a, b] where F(a) < 0 and F(b) >= 0
            if (!FindInitialInterval(dir, point, out float a, out float b))
                return null;

            // Binary search for the boundary where F(t) becomes >= 0
            float t = BinarySearch(dir, point, a, b);

            // Round to nearest grid cell coordinates
            Vector2 result = point + t * dir;
            return new Vector2((int)Math.Round(result.X), (int)Math.Round(result.Y));
        }

        private static float CalculateF(float t, Vector2 dir, Vector2 point)
        {
            Vector2 testPoint = point + t * dir;
            int x = (int)Math.Round(testPoint.X);
            int y = (int)Math.Round(testPoint.Y);

            if (Texturer.GetIsland(x, y) != _island)
                return 0;

            float radius = Texturer.GetRadius(x, y);
            return t - radius;  // t >= radius -> F(t) >= 0
        }

        private static bool FindInitialInterval(Vector2 dir, Vector2 point, out float a, out float b)
        {
            a = 0;
            b = 1.0f; // Start with grid step = 1

            float fa = CalculateF(a, dir, point);

            // If already at boundary
            if (fa >= 0)
            {
                b = a;
                return true;
            }

            int maxDoubling = 20;

            for (int i = 0; i < maxDoubling; i++)
            {
                float fb = CalculateF(b, dir, point);

                if (fb >= 0)
                    return true;

                // Double step size
                a = b;
                fa = fb;
                b *= 2;
            }

            a = 0;
            b = 0;
            return false;
        }

        private static float BinarySearch(Vector2 dir, Vector2 point, float a, float b)
        {
            float tolerance = 1;
            int iterations = 0;

            while (b - a > tolerance && iterations < maxIterations)
            {
                float mid = (a + b) * 0.5f;
                float fmid = CalculateF(mid, dir, point);

                if (fmid < 0)
                    a = mid;
                else
                    b = mid;

                iterations++;
            }

            return b; // b is guaranteed to have F(b) >= 0
        }
    }
}
