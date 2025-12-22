using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public static class TriangleRasterizer
    {
        public static void Rasterize(Vector2 v0, Vector2 v1, Vector2 v2, Action<int, int> processPixel)
        {
            if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
            if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
            if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

            int y0 = (int)MathF.Ceiling(v0.Y);
            int y1 = (int)MathF.Ceiling(v1.Y);
            int y2 = (int)MathF.Ceiling(v2.Y);

            float invSlope1 = (v1.Y - v0.Y) > float.Epsilon ? (v1.X - v0.X) / (v1.Y - v0.Y) : 0;
            float invSlope2 = (v2.Y - v0.Y) > float.Epsilon ? (v2.X - v0.X) / (v2.Y - v0.Y) : 0;
            float invSlope3 = (v2.Y - v1.Y) > float.Epsilon ? (v2.X - v1.X) / (v2.Y - v1.Y) : 0;

            float curX1 = v0.X + (y0 - v0.Y) * invSlope1;
            float curX2 = v0.X + (y0 - v0.Y) * invSlope2;

            for (int y = y0; y < y1; y++)
            {
                int startX = (int)MathF.Ceiling(MathF.Min(curX1, curX2));
                int endX = (int)MathF.Floor(MathF.Max(curX1, curX2));

                for (int x = startX; x <= endX; x++)
                {
                    processPixel(x, y);
                }

                curX1 += invSlope1;
                curX2 += invSlope2;
            }

            curX1 = v1.X + (y1 - v1.Y) * invSlope3;

            for (int y = y1; y < y2; y++)
            {
                int startX = (int)MathF.Ceiling(MathF.Min(curX1, curX2));
                int endX = (int)MathF.Floor(MathF.Max(curX1, curX2));

                for (int x = startX; x <= endX; x++)
                {
                    processPixel(x, y);
                }

                curX1 += invSlope3;
                curX2 += invSlope2;
            }
        }
    }
}
