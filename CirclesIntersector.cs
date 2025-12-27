using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public static class CirclesIntersector
    {
        public static List<((double x, double y) point, (double x, double y) direction)>
    FindIntersectionPointsWithNormals(double x1, double y1, double r1, double x2, double y2, double r2)
        {
            var results = new List<((double, double), (double, double))>();
            double dx = x2 - x1, dy = y2 - y1;
            double d = Math.Sqrt(dx * dx + dy * dy);

            bool scaled = false;
            double originalR1 = r1, originalR2 = r2;

            if (d > r1 + r2 || d < Math.Abs(r1 - r2) || d == 0)
            {
                double k = 1.05;
                r1 *= k;
                r2 *= k;
                scaled = true;
            }

            d = Math.Sqrt(dx * dx + dy * dy);

            if (d > r1 + r2 || d < Math.Abs(r1 - r2) || d == 0) return results;

            double a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
            double hSq = r1 * r1 - a * a;

            if (hSq < 0) hSq = 0;
            double h = Math.Sqrt(hSq);
            double xm = x1 + a * dx / d;
            double ym = y1 + a * dy / d;

            var centerVec = (dx / d, dy / d);
            var perp = (-centerVec.Item2, centerVec.Item1);

            bool firstIsSmaller = originalR1 < originalR2;
            double sign = firstIsSmaller ? 1.0 : -1.0;

            if (h > 0)
            {
                double xs1 = xm + h * dy / d;
                double ys1 = ym - h * dx / d;
                double xs2 = xm - h * dy / d;
                double ys2 = ym + h * dx / d;

                var normal11 = (xs1 - x1, ys1 - y1);
                var normal12 = (xs1 - x2, ys1 - y2);
                var normal1Dir = (
                    (normal11.Item1 / originalR1 + normal12.Item1 / originalR2) * 0.5,
                    (normal11.Item2 / originalR1 + normal12.Item2 / originalR2) * 0.5
                );

                var normal21 = (xs2 - x1, ys2 - y1);
                var normal22 = (xs2 - x2, ys2 - y2);
                var normal2Dir = (
                    (normal21.Item1 / originalR1 + normal22.Item1 / originalR2) * 0.5,
                    (normal21.Item2 / originalR1 + normal22.Item2 / originalR2) * 0.5
                );

                double len1 = Math.Sqrt(normal1Dir.Item1 * normal1Dir.Item1 + normal1Dir.Item2 * normal1Dir.Item2);
                double len2 = Math.Sqrt(normal2Dir.Item1 * normal2Dir.Item1 + normal2Dir.Item2 * normal2Dir.Item2);

                results.Add(((xs1, ys1), (normal1Dir.Item1 / len1, normal1Dir.Item2 / len1)));
                results.Add(((xs2, ys2), (normal2Dir.Item1 / len2, normal2Dir.Item2 / len2)));
            }
            else if (scaled)
            {
                double xs = xm;
                double ys = ym;

                var normal11 = (xs - x1, ys - y1);
                var normal12 = (xs - x2, ys - y2);
                var normal1Dir = (
                    (normal11.Item1 / originalR1 + normal12.Item1 / originalR2) * 0.5,
                    (normal11.Item2 / originalR1 + normal12.Item2 / originalR2) * 0.5
                );

                var normal2Dir = (-normal1Dir.Item1, -normal1Dir.Item2);

                double len1 = Math.Sqrt(normal1Dir.Item1 * normal1Dir.Item1 + normal1Dir.Item2 * normal1Dir.Item2);
                double len2 = Math.Sqrt(normal2Dir.Item1 * normal2Dir.Item1 + normal2Dir.Item2 * normal2Dir.Item2);

                results.Add(((xs, ys), (normal1Dir.Item1 / len1, normal1Dir.Item2 / len1)));
                results.Add(((xs, ys), (normal2Dir.Item1 / len2, normal2Dir.Item2 / len2)));
            }

            return results;
        }
    }
}
