using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Numerics;
using System.Windows;

namespace RIKA_TEXTURER
{
    public static class Texturer
    {
        public static BitmapImage _img;
        public static Obj _obj;
        public static float[] _sizes;
        public static ushort[] _islands;
        public static Random _rnd;

        public static void Do(int texSize)
        {
            _rnd = new Random();
            _sizes = new float[texSize * texSize];
            _islands = new ushort[texSize * texSize];
            var bitmap = new WriteableBitmap(texSize, texSize, 96, 96, PixelFormats.Bgra32, null);
            _sizes = new float[texSize * texSize];

            List<TextureIsland> islands = IslandDetector.DetectIslands(_obj);
            for (ushort i = 0; i < islands.Count; i++)
            {
                FillSizes(islands[i].FaceIndices, i);
                Rect bounds = islands[i].Bounds;
                Vector2 point = GetStartPoint(bounds, i);
                //I left here.
            }

            Vector2 GetStartPoint(Rect bounds, ushort islandIndex)
            {
                while (true)
                {
                    Vector2 point = new Vector2(_rnd.Next((int)bounds.Left, (int)bounds.Right), _rnd.Next((int)bounds.Top, (int)bounds.Bottom));
                    if (_islands[(int)point.X + (int)point.Y * texSize] == islandIndex)
                        return point;
                }
            }

            void FillSizes(List<int> faceIndexes, ushort islandIndex)
            {
                for (int i = 0; i < faceIndexes.Count; i++)
                {
                    var face = _obj.Faces[faceIndexes[i]];
                    var v1 = _obj.Vertices[face.VertexIndices[0]];
                    var v2 = _obj.Vertices[face.VertexIndices[1]];
                    var v3 = _obj.Vertices[face.VertexIndices[2]];
                    var tc1 = _obj.TexCoords[face.TexCoordIndices[0]];
                    var tc2 = _obj.TexCoords[face.TexCoordIndices[1]];
                    var tc3 = _obj.TexCoords[face.TexCoordIndices[2]];
                    float e12 = (tc1 - tc2).Length() / texSize / (v1 - v2).Length();
                    float e23 = (tc2 - tc3).Length() / texSize / (v2 - v3).Length();
                    float e31 = (tc3 - tc1).Length() / texSize / (v3 - v1).Length();
                    float p1 = (e12 + e23) / 2;
                    float p2 = (e23 + e31) / 2;
                    float p3 = (e31 + e12) / 2;

                    TriangleInterpolator ti = new TriangleInterpolator(tc1, p1, tc2, p2, tc3, p3);

                    TriangleRasterizer.Rasterize(
                        tc1,
                        tc2,
                        tc3,
                        (x, y) => {
                            _sizes[x * texSize + y] = ti.InterpolateOptimized(x, y);
                            _islands[x * texSize + y] = islandIndex;
                        }
                    );
                }
            }
        }

        private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = (b.Y - a.Y) * (p.X - a.X) - (b.X - a.X) * (p.Y - a.Y);
            float s2 = (c.Y - b.Y) * (p.X - b.X) - (c.X - b.X) * (p.Y - b.Y);
            float s3 = (a.Y - c.Y) * (p.X - c.X) - (a.X - c.X) * (p.Y - c.Y);

            return (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
        }
    }
}
