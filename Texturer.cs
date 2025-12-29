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
        public static int _texSize;
        public static float _scaler;

        public static List<Vector2> _circles;
        public static List<float> _circlesRadiuses;

        public static WriteableBitmap _daemon;

        public static int _t;

        public static List<(int x, int y)> _nextPoints;

        private static Dictionary<(int scale, int angle), WriteableBitmap> _cache3;
        private static Dictionary<int, WriteableBitmap> _cache2;
        private static WriteableBitmap _cache1;

        private static WriteableBitmap GetCachedImage(int angleId, int scaleId)
        {
            if (_cache3.ContainsKey((scaleId, angleId)))
            {

                if (angleId == 5 && scaleId == 10)
                {
                    WBMP.SaveToPng(_cache3[(scaleId, angleId)], $"{Disk._programFiles}CacheTest.png");
                }

                return _cache3[(scaleId, angleId)];
            }
            else if (_cache2.ContainsKey(angleId))
            {
                WriteableBitmap bmp = _cache2[angleId];
                double radius = Math.Pow(scaleId / 32.0, 3) * _texSize;
                int newWidth = (int)(radius * 4);
                bmp = WBMP.ScaleBitmap(bmp, newWidth, newWidth);
                _cache3[(scaleId, angleId)] = bmp;
                return GetCachedImage(angleId, scaleId);
            }
            else if (_cache1 != null)
            {
                float angle = angleId * 11.25f; //360f / 32;
                _cache2[angleId] = WBMP.RotateBitmap(_cache1, angle);
                return GetCachedImage(angleId, scaleId);
            }
            else
            {
                WriteableBitmap bmp = WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    var temp = new WriteableBitmap(_tex);
                    temp.Freeze();
                    return temp;
                });

                bmp = WBMP.Copy(bmp);

                bmp = WBMP.CropToSquare(bmp);

                var mask = WBMP.CreateLogarithmicMask(bmp.PixelWidth);
                _cache1 = WBMP.ApplyMask(bmp, mask);
                return GetCachedImage(angleId, scaleId);
            }
        }

        static Texturer()
        {
            _nextPoints = new List<(int x, int y)>();
            _cache2 = new Dictionary<int, WriteableBitmap>();
            _cache3 = new Dictionary<(int scale, int angle), WriteableBitmap>();
        }

        static void ClearCache()
        {
            foreach (var bitmap in _cache2.Values.Concat(_cache3.Values))
            {
                if (!bitmap.IsFrozen)
                    bitmap.Freeze();
            }

            _cache2.Clear();
            _cache3.Clear();
            _cache1 = null;

            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            _cache2 = new Dictionary<int, WriteableBitmap>();
            _cache3 = new Dictionary<(int scale, int angle), WriteableBitmap>();
        }

        public static void Do(int texSize, float scaler, int startCount, string fillType)
        {
            if (_nextPoints.Count > 0)
                return;

            if (_circles != null && _circles.Count > 0)
                return;

            Thread mt = new Thread(MT);
            mt.Start();

            void MT()
            {
                _texSize = texSize;
                _scaler = scaler;
                _radiuses = new float[texSize * texSize];
                _islands = new ushort[texSize * texSize];

                WriteableBitmap wbmp = WBMP.Create(_texSize);
                _daemon = WBMP.Create(_texSize);

                List<TextureIsland> islands = IslandDetector.DetectIslands(_obj);
                for (ushort island = 1; island <= islands.Count; island++)
                {
                    FillSizes(islands[island - 1].FaceIndices, island);
                    Rect bounds = islands[island - 1].Bounds;

                    _circles = new List<Vector2>();
                    _circlesRadiuses = new List<float>();
                    //_border = new List<int>();
                    List<Vector2> nextPoints = new List<Vector2>();

                    for (int i = 0; i < startCount; i++)
                        StartFill();

                    while (_nextPoints.Count > 0)
                    {
                        int id = 0;

                        if (fillType == "Random")
                        {
                            id = Disk._rnd.Next(_nextPoints.Count - 1);
                        }
                        else if (fillType == "First")
                        {
                            id = 0;
                        }
                        else if (fillType == "Last")
                        {
                            id = _nextPoints.Count - 1;
                        }
                        else if (fillType == "Middle")
                        {
                            id = _nextPoints.Count / 2;
                        }

                        MoreMoreMore(_nextPoints[id], island);
                        _nextPoints.RemoveAt(id);
                    }

                    int c = 0;

                    while(_circles.Count > 0)
                    {
                        DrawCircle();
                        _circles.RemoveAt(0);
                        _circlesRadiuses.RemoveAt(0);
                        c++;
                    }

                    void DrawCircle()
                    {
                        Vector2 pos = _circles[0];
                        float radius = _circlesRadiuses[0];
                        radius += 2;

                        double scale = radius / texSize;
                        scale = Math.Pow(scale, 0.33333333);
                        int scaleId = (int)(scale * 32);

                        int rotationId = Disk._rnd.Next(32);

                        WriteableBitmap img = GetCachedImage(rotationId, scaleId);

                        WBMP.FillTextureCircleWithAlpha(wbmp, pos, img, island);

                        _t++;

                        if (_t % 105 == 0)
                        {
                            _resImg = WBMP.ConvertToBitmapImage(wbmp);

                            WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                            {
                                WindowsManager._mainWindow.img.Source = _resImg;
                            });
                        }
                    }

                    void StartFill()
                    {
                        Vector2 point = GetStartPoint(bounds, island);

                        _circles.Add(point);
                        _circlesRadiuses.Add(GetRadius(point));

                        DrawDaemonCircle(point, GetRadius(point), island);
                    }
                }

                _resImg = WBMP.ConvertToBitmapImage(_daemon);

                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    WindowsManager._mainWindow.img.Source = _resImg;
                });

                WBMP.SaveToPng(_daemon, $"{Disk._programFiles}Result1.png");

                _resImg = WBMP.ConvertToBitmapImage(wbmp);

                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    WindowsManager._mainWindow.img.Source = _resImg;
                });

                WBMP.SaveToPng(wbmp, $"{Disk._programFiles}Result2.png");

                ClearCache();

                Vector2 GetStartPoint(Rect bounds, ushort islandIndex)
                {
                    while (true)
                    {
                        int x = Disk._rnd.Next((int)(bounds.Left * texSize), (int)(bounds.Right * texSize));
                        int y = Disk._rnd.Next((int)(bounds.Top * texSize), (int)(bounds.Bottom * texSize));
                        if (_islands[x + y * texSize] == islandIndex)
                            return new Vector2(x, y);
                    }
                }

                void MoreMoreMore((int x, int y) point, ushort islandId)
                {
                    //1

                    int circleId = _circles.Count;

                    //2

                    Vector2 dir = WBMP.GetDaemonAngleFast(_daemon, point.x, point.y);

                    if (dir == Vector2.Zero)
                        return;

                    Vector2 pos = new Vector2((float)point.x, (float)point.y);

                    //3

                    Vector2 res = CircleToPointSolver.FindCenterPoint(dir, pos, islandId) ?? new Vector2(-1, -1);

                    //4

                    if ((int)res.X != -1)
                    {
                        float radius = GetRadius(res);

                        if (radius == 0)
                            radius = 1;// (pos - res).Length();

                        if (radius == 0)
                            return;

                        //5

                        _circles.Add(res);
                        _circlesRadiuses.Add(radius);

                        DrawDaemonCircle(res, radius, islandId);
                    }
                }

                void DrawDaemonCircle(Vector2 center, float radius, ushort island)
                {
                    Color color = Rainbow.GetRainbowColor(_t);
                    WBMP.DaemonFiller(_daemon, center, radius, island, color);
                    _t++;

                    if (_t % 100 == 0)
                    {
                        _resImg = WBMP.ConvertToBitmapImage(_daemon);

                        WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                        {
                            WindowsManager._mainWindow.img.Source = _resImg;
                        });
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

        public static ushort GetIsland(Vector2 point)
        {
            return GetIsland((ushort)point.X, (ushort)point.Y);
        }
    }
}
