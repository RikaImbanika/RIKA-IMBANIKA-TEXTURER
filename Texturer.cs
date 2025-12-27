using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Numerics;
using System.Windows;
using System.Windows.Threading;

namespace RIKA_TEXTURER
{
    public static class Texturer
    {
        public static BitmapImage _tex;
        public static BitmapImage _resImg;
        public static Obj _obj;
        public static float[] _radiuses;
        public static ushort[] _islands;
        public static Random _rnd;
        public static int _texSize;
        public static float _scaler;

        public static List<Vector2> _circles;
        public static List<float> _circlesRadiuses;

        public static void Do(int texSize, float scaler)
        {
            Thread mt = new Thread(MT);
            mt.Start();

            void MT()
            {
                _texSize = texSize;
                _scaler = scaler;
                _rnd = new Random();
                _radiuses = new float[texSize * texSize];
                _islands = new ushort[texSize * texSize];
                _radiuses = new float[texSize * texSize];

                WriteableBitmap wbmp = WBMP.Create(_texSize);

                List<TextureIsland> islands = IslandDetector.DetectIslands(_obj);
                for (ushort island = 1; island <= islands.Count; island++)
                {
                    FillSizes(islands[island - 1].FaceIndices, island);
                    Rect bounds = islands[island - 1].Bounds;

                    _circles = new List<Vector2>();
                    _circlesRadiuses = new List<float>();
                    List<Vector2> nextPoints = new List<Vector2>();

                    Vector2 point = GetStartPoint(bounds, island);

                    _circles.Add(point);
                    _circlesRadiuses.Add(GetRadius(point));

                    point = GetSecondPoint(bounds, island, point, _circlesRadiuses[0]);

                    _circles.Add(point);
                    _circlesRadiuses.Add(GetRadius(point));

                    List<((double x, double y) point, (double x, double y) direction)> newOnes = CirclesIntersector.FindIntersectionPointsWithNormals(_circles[0].X, _circles[0].Y, _circlesRadiuses[0], _circles[1].X, _circles[1].Y, _circlesRadiuses[1]);
                    GetMore(newOnes[0].point, newOnes[0].direction, island);
                    GetMore(newOnes[1].point, newOnes[1].direction, island);

                    Color color = Color.FromArgb(200, (byte)_rnd.Next(255), (byte)_rnd.Next(255), (byte)_rnd.Next(255));

                    for (int i = 0; i < _circles.Count; i++)
                    {
                        WBMP.FillCircle(wbmp, _circles[i], _circlesRadiuses[i], color);
                    }
                }

                _resImg = WBMP.ConvertToBitmapImage(wbmp);

                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    WindowsManager._mainWindow.img.Source = _resImg;
                });

                //WBMP.SaveToPng(wbmp, $"{Disk._programFiles}Result.png");

                Vector2 GetStartPoint(Rect bounds, ushort islandIndex)
                {
                    while (true)
                    {
                        int x = _rnd.Next((int)(bounds.Left * texSize), (int)(bounds.Right * texSize));
                        int y = _rnd.Next((int)(bounds.Top * texSize), (int)(bounds.Bottom * texSize));
                        if (_islands[x + y * texSize] == islandIndex)
                            return new Vector2(x, y);
                    }
                }

                Vector2 GetSecondPoint(Rect bounds, ushort islandIndex, Vector2 center, float radius)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        float angle = Disk._rnd.NextSingle() * MathF.PI * 2;
                        (float, float) sinCos = MathF.SinCos(angle);
                        Vector2 dir = new Vector2(sinCos.Item1, sinCos.Item2);
                        Vector2 point = dir * radius + center;

                        if (GetIsland((int)point.X, (int)point.Y) == islandIndex)
                        {
                            Vector2 res = PointToCircleSolver.FindCenterPoint(dir, point, islandIndex) ?? Vector2.Zero;

                            if (GetIsland((int)res.X, (int)res.Y) == islandIndex)
                                return res;
                        }
                    }

                    return center + new Vector2(3, 0);
                }

                void GetMore((double x, double y) point, (double x, double y) direction, ushort islandIndex)
                {
                    if (GetIsland((int)point.x, (int)point.y) != islandIndex)
                    {
                        return;
                    }

                    Vector2 dir = new Vector2((float)direction.x, (float)direction.y);
                    Vector2 pos = new Vector2((float)point.x, (float)point.y);

                    Vector2 res = PointToCircleSolver.FindCenterPoint(dir, pos, islandIndex) ?? Vector2.Zero;

                    if (GetIsland((int)res.X, (int)res.Y) == islandIndex)
                    {
                        _circles.Add(res);
                        _circlesRadiuses.Add(GetRadius(res));
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
                        Vector2 tc1 = _obj.TexCoords[face.TexCoordIndices[0]] * texSize;
                        Vector2 tc2 = _obj.TexCoords[face.TexCoordIndices[1]] * texSize;
                        Vector2 tc3 = _obj.TexCoords[face.TexCoordIndices[2]] * texSize;
                        float e12 = (tc1 - tc2).Length() * _scaler / (v1 - v2).Length();
                        float e23 = (tc2 - tc3).Length() * _scaler / (v2 - v3).Length();
                        float e31 = (tc3 - tc1).Length() * _scaler / (v3 - v1).Length();
                        float p1 = (e12 + e23) / 2;
                        float p2 = (e23 + e31) / 2;
                        float p3 = (e31 + e12) / 2;

                        TriangleInterpolator ti = new TriangleInterpolator(tc1, p1, tc2, p2, tc3, p3);

                        TriangleRasterizer.Rasterize(
                            tc1,
                            tc2,
                            tc3,
                            (x, y) =>
                            {
                                _radiuses[x + y * _texSize] = ti.InterpolateOptimized(x, y);
                                _islands[x + y * _texSize] = islandIndex;
                            }
                        );
                    }
                }
            }
        }

        public static float GetRadius(int x, int y)
        {
            if (x < 0 || x >= _texSize || y < 0 || y >= _texSize)
                return 0;

            return _radiuses[x + y * _texSize];
        }

        public static float GetRadius(Vector2 point)
        {
            return GetRadius((int)point.X, (int)point.Y);
        }

        public static ushort GetIsland(int x, int y)
        {
            if (x < 0 || x >= _texSize || y < 0 || y >= _texSize)
                return 0;

            return _islands[x + y * _texSize];
        }

        public static float GetIsland(Vector2 point)
        {
            return GetIsland((int)point.X, (int)point.Y);
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
