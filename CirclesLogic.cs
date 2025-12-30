using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_IMBANIKA_TEXTURER
{
    public static class CirclesLogic
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

        
        public static ((double x, double y) point, (double x, double y) direction)?
       FindIntersection(Vector2 mainCenter, float mainRadius,
                       Vector2 secondaryCenter, float secondaryRadius,
                       Vector2 direction)
        {
            Vector2 d = secondaryCenter - mainCenter;
            float distanceSquared = d.LengthSquared();
            float distance = MathF.Sqrt(distanceSquared);

            // No intersection cases
            if (distance > mainRadius + secondaryRadius || distance < MathF.Abs(mainRadius - secondaryRadius))
                return null;

            // Circles coincide
            if (distance == 0 && mainRadius == secondaryRadius)
                return null;

            // Calculate intersection points using geometric formula
            float a = (mainRadius * mainRadius - secondaryRadius * secondaryRadius + distanceSquared) / (2 * distance);
            float hSquared = mainRadius * mainRadius - a * a;

            if (hSquared < 0)
                return null;

            float h = MathF.Sqrt(hSquared);
            Vector2 p2 = mainCenter + a * d / distance;

            Vector2 intersectionPoint;

            // Single tangent point
            if (h < 1e-7f)
            {
                float angle = GetCounterClockwiseAngle(direction, p2 - mainCenter);
                if (angle < 1e-7f || angle > 2 * MathF.PI - 1e-7f)
                    return null;
                intersectionPoint = p2;
            }
            else
            {
                // Two intersection points
                Vector2 perpendicular = new Vector2(-d.Y, d.X) * (h / distance);
                Vector2 intersection1 = p2 + perpendicular;
                Vector2 intersection2 = p2 - perpendicular;

                intersectionPoint = SelectCounterClockwisePoint(intersection1, intersection2, mainCenter, direction);
                if (intersectionPoint == Vector2.Zero)
                    return null;
            }

            // Calculate average tangent direction
            Vector2 tangent1 = RotateClockwise90(intersectionPoint - mainCenter);
            Vector2 tangent2 = RotateCounterClockwise90(intersectionPoint - secondaryCenter);

            Vector2 averageDirection = Vector2.Normalize(tangent1) + Vector2.Normalize(tangent2);
            averageDirection = Vector2.Normalize(averageDirection);

            return ((intersectionPoint.X, intersectionPoint.Y),
                    (averageDirection.X, averageDirection.Y));
        }

        private static Vector2 SelectCounterClockwisePoint(Vector2 p1, Vector2 p2,
                                                          Vector2 mainCenter, Vector2 direction)
        {
            Vector2 v1 = p1 - mainCenter;
            Vector2 v2 = p2 - mainCenter;

            float angle1 = GetCounterClockwiseAngle(direction, v1);
            float angle2 = GetCounterClockwiseAngle(direction, v2);

            const float epsilon = 1e-7f;

            // Select point with smallest positive angle

            if (angle1 < epsilon)
                return p2;

            if (angle2 < epsilon)
                return p1;

            return angle1 < angle2 ? p1 : p2;
        }

        private static float GetCounterClockwiseAngle(Vector2 direction, Vector2 pointDir)
        {
            float angleDir = MathF.Atan2(direction.Y, direction.X);
            float anglePoint = MathF.Atan2(pointDir.Y, pointDir.X);

            float diff = anglePoint - angleDir;
            if (diff < 0) diff += 2 * MathF.PI;

            return diff;
        }

        private static Vector2 RotateClockwise90(Vector2 v)
        {
            return new Vector2(v.Y, -v.X);
        }

        private static Vector2 RotateCounterClockwise90(Vector2 v)
        {
            return new Vector2(-v.Y, v.X);
        }

        public static float GetAngleCounterClockwise(Vector2 v1, Vector2 v2)
        {
            v1 = Vector2.Normalize(v1);
            v2 = Vector2.Normalize(v2);

            float dot = Vector2.Dot(v1, v2);
            float det = v1.X * v2.Y - v1.Y * v2.X;

            float angle = (float)Math.Atan2(det, dot);

            if (angle < 0)
                angle += (float)(2 * Math.PI);

            return angle;
        }

        public static float GetAngleClockwise(Vector2 v1, Vector2 v2)
        {
            v1 = Vector2.Normalize(v1);
            v2 = Vector2.Normalize(v2);

            float dot = Vector2.Dot(v1, v2);
            float det = v1.Y * v2.X - v1.X * v2.Y;

            float angle = (float)Math.Atan2(det, dot);

            if (angle < 0)
                angle += (float)(2 * Math.PI);

            return angle;
        }

        public static bool IsBFirst((double x, double y) a, (double x, double y) dir, (double x, double y) b, (double x, double y) c)
        {
            if (b.Equals(c)) return true;

            double Cross((double x, double y) v1, (double x, double y) v2) => v1.x * v2.y - v1.y * v2.x;
            double Dot((double x, double y) v1, (double x, double y) v2) => v1.x * v2.x + v1.y * v2.y;

            var ab = (b.x - a.x, b.y - a.y);
            var ac = (c.x - a.x, c.y - a.y);

            double crossAb = Cross(dir, ab);
            double crossAc = Cross(dir, ac);

            if (crossAb >= 0 && crossAc >= 0) return crossAb >= crossAc;
            if (crossAb < 0 && crossAc < 0) return crossAb >= crossAc;

            return crossAb >= 0;
        }
    }
}
