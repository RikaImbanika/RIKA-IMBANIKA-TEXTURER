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

namespace RIKA_IMBANIKA_TEXTURER
{
    public static class Texturer
    {
        public static WriteableBitmap _wbmp;
        public static WriteableBitmap _wbmp2;
        public static BitmapImage _tex;
        public static BitmapImage _resImg;
        public static Obj _obj;
        public static float[] _radiuses;
        public static ushort[] _islandsMap;
        public static List<TextureIsland> _islands;
        public static bool _islandsDetected;
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
                    WBMP.SaveToPng(_cache3[(scaleId, angleId)], $"{S.PF}CacheTest.png");
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
                if (texSize != _texSize)
                    _islandsDetected = false;

                _texSize = texSize;
                _scaler = scaler * DefineScaleCoefficient();

                _wbmp = WBMP.Create(_texSize);
                _daemon = WBMP.Create(_texSize);

                TryDetectIslands();

                for (ushort island = 1; island <= _islands.Count; island++)
                {
                    Rect bounds = _islands[island - 1].Bounds;

                    _circles = new List<Vector2>();
                    _circlesRadiuses = new List<float>();
                    List<Vector2> nextPoints = new List<Vector2>();

                    for (int i = 0; i < startCount; i++)
                        StartFill();

                    while (_nextPoints.Count > 0)
                    {
                        int id = 0;

                        if (fillType == "Random")
                        {
                            id = S.Rnd.Next(_nextPoints.Count - 1);
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

                        int rotationId = S.Rnd.Next(32);

                        WriteableBitmap img = GetCachedImage(rotationId, scaleId);

                        WBMP.FillTextureCircleWithAlpha(_wbmp, pos, img, island);

                        _t++;

                        if (_t % 105 == 0)
                        {
                            _resImg = WBMP.ConvertToBitmapImage(_wbmp);

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

                WBMP.SaveToPng(_daemon, $"{S.PF}Result1.png");

                _resImg = WBMP.ConvertToBitmapImage(_wbmp);

                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    WindowsManager._mainWindow.img.Source = _resImg;
                });

                WBMP.SaveToPng(_wbmp, $"{S.PF}Result2.png");

                ClearCache();

                Smooth(_texSize);

                Vector2 GetStartPoint(Rect bounds, ushort islandIndex)
                {
                    while (true)
                    {
                        int x = S.Rnd.Next((int)(bounds.Left * texSize), (int)(bounds.Right * texSize));
                        int y = S.Rnd.Next((int)(bounds.Top * texSize), (int)(bounds.Bottom * texSize));
                        if (_islandsMap[x + y * texSize] == islandIndex)
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
            }
        }

        public static void Do2(int texSize, float scaler)
        {
            //IDK, this was an idea

            Thread mt = new Thread(MT);
            mt.Start();

            void MT()
            {
                _texSize = texSize;
                _scaler = scaler * DefineScaleCoefficient();
                _radiuses = new float[texSize * texSize];
                _islandsMap = new ushort[texSize * texSize];

                WriteableBitmap wbmp = WBMP.Create(_texSize);
            }
        }

        public static void Smooth(int texSize)
        {
            if (texSize != _texSize)
                _islandsDetected = false;
            _texSize = texSize;

            Thread mt = new Thread(MT);
            mt.Start();

            void MT()
            {
                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    _wbmp2 = new WriteableBitmap(_resImg);
                });

                TryDetectIslands();

                for (ushort iid = 0; iid < _islands.Count; iid++)
                {
                    FillSizes(_islands[iid].FaceIndices, (ushort)(iid + 1));

                    TextureIsland island = _islands[iid];
                    List<List<Vector2>> boundaries = BoundaryExtractor.GetBoundaryContours(island.UVs, island.Triangles);

                    for (int bi = 0; bi < boundaries.Count; bi++)
                    {
                        List<Vector2> boundary = boundaries[bi];

                        for (int li = 0; li < boundary.Count; li++)
                        {
                            Vector2 point1 = boundary[li];
                            Vector2 point2;

                            if (li < boundary.Count - 1)
                                point2 = boundary[li + 1];
                            else
                                point2 = boundary[0];

                            Vector2 dir = point2 - point1;

                            float distance = dir.Length();

                            Vector2 dir0 = dir / distance;

                            Vector2 truePoint1 = point1 * _texSize;

                            float radius = GetRadius2((int)truePoint1.X, (int)truePoint1.Y);

                            float t = 0.000001f;

                            Vector2 right = new Vector2(dir.Y, -dir.X) / distance;

                            int vertexIndex1 = GetVertexIndex(island, point1);
                            int vertexIndex2 = GetVertexIndex(island, point2);

                            (int island, int v1, int v2)? adj = PortalDetector.GetAdjacentEdge(iid, vertexIndex1, vertexIndex2);

                            if (adj != null)
                            {
                                int otherIsland = adj.Value.island;
                                int ov1 = adj.Value.v1;
                                int ov2 = adj.Value.v2;

                                Vector2 a1 = _islands[iid].UVs[vertexIndex1];
                                Vector2 a2 = _islands[iid].UVs[vertexIndex2];
                                Vector2 b1 = _islands[otherIsland].UVs[ov1];
                                Vector2 b2 = _islands[otherIsland].UVs[ov2];

                                Vector2 va = a2 - a1;
                                Vector2 vb = b2 - b1;

                                float scale = vb.Length() / va.Length();
                                float angle = (float)Math.Atan2(vb.Y, vb.X) - (float)Math.Atan2(va.Y, va.X);
                                angle = -angle;

                                Vector2 otherDir = vb;
                                float otherDistance = otherDir.Length();
                                Vector2 otherDir0 = otherDir / otherDistance;

                                while (t < distance)
                                {
                                    Vector2 point = point1 + dir0 * t;

                                    Vector2 truePoint = point * _texSize;

                                    float pixel = 1f / _texSize;

                                    if (GetIsland(truePoint) == iid + 1)
                                    {
                                        float radius2 = GetRadius2((int)truePoint.X, (int)truePoint.Y);
                                        if (radius2 > pixel)
                                            radius = radius2;
                                    }

                                    float otherT = t * scale;

                                    Vector2 otherPoint = b1 + otherDir0 * otherT;

                                    Vector2 step = (1f + S.Rnd.NextSingle()) * right * radius;

                                    while (!Inside() && radius > pixel)
                                    {
                                        radius *= 0.75f;

                                        step = (1f + S.Rnd.NextSingle()) * right * radius;
                                    }

                                    if (radius > pixel)
                                        Draw();
                                    else
                                        radius = pixel;

                                    t += radius * 0.5f;

                                    void Draw()
                                    {
                                        float trueRadius = radius * _texSize;
                                        Vector2 trueOtherPoint = otherPoint * _texSize;

                                        Vector2 from = (point + step) * _texSize;

                                        WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                                        {
                                            ushort iid2 = Convert.ToUInt16(iid + 1);
                                            WBMP.CloneWithMask(_wbmp2, _wbmp2, truePoint, from, trueRadius, iid2);
                                            
                                            ushort oid = Convert.ToUInt16(otherIsland + 1);
                                            WBMP.CloneWithMask(_wbmp2, _wbmp2, trueOtherPoint, from, trueRadius, oid, scale, angle);
                                        });
                                    }

                                    bool Inside()
                                    {
                                        for (int k = 0; k < boundaries.Count; k++)
                                            if (!TextureIsland.IsInside(point + step, radius, boundaries[k]))
                                                return false;

                                        return true;
                                    }
                                }
                            }

                            int GetVertexIndex(TextureIsland island, Vector2 point, float epsilon = 1e-6f)
                            {
                                for (int i = 0; i < island.UVs.Count; i++)
                                    if (Vector2.DistanceSquared(island.UVs[i], point) < epsilon * epsilon)
                                        return i;
                                return -1;
                            }
                        }
                    }

                    Show();
                }

                Bleed();

                Show();

                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    WBMP.SaveToPng(_wbmp2, $"{S.PF}Result3.png");
                });

                void Show()
                {
                    WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                    {
                        _resImg = WBMP.ConvertToBitmapImage(_wbmp2);

                        WindowsManager._mainWindow.img.Source = _resImg;
                    });
                }
            }
        }

        public static float DefineScaleCoefficient()
        {
            return _obj.GetMaxSize() / 3f;
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

        public static float GetRadius2(int px, int py)
        {
            float rad = GetRadius(px, py) / _texSize;
            rad *= 0.2f * (1f + S.Rnd.NextSingle() * 4f);
            return rad;
        }

        public static ushort GetIsland(int x, int y)
        {
            if (x < 0 || x >= _texSize || y < 0 || y >= _texSize)
                return ushort.MaxValue;

            return _islandsMap[x + y * _texSize];
        }

        public static ushort GetIsland(Vector2 point)
        {
            return GetIsland((int)point.X, (int)point.Y);
        }

        public static void TryDetectIslands()
        {
            if (!_islandsDetected)
            {
                _islands = IslandDetector.DetectIslands(_obj);
                _radiuses = new float[_texSize * _texSize];
                _islandsMap = new ushort[_texSize * _texSize];
                _islandsDetected = true;
                PortalDetector.DetectPortals(_obj, _islands);

                for (ushort island = 1; island <= _islands.Count; island++)
                {
                    FillSizes(_islands[island - 1].FaceIndices, island);
                }
            }
        }

        public static void FillSizes(List<int> faceIndexes, ushort islandIndex)
        {
            for (int i = 0; i < faceIndexes.Count; i++)
            {
                var face = _obj.Faces[faceIndexes[i]];
                var v1 = _obj.Vertices[face.VertexIndices[0]];
                var v2 = _obj.Vertices[face.VertexIndices[1]];
                var v3 = _obj.Vertices[face.VertexIndices[2]];
                Vector2 tc1 = _obj.TexCoords[face.TexCoordIndices[0]] * _texSize;
                Vector2 tc2 = _obj.TexCoords[face.TexCoordIndices[1]] * _texSize;
                Vector2 tc3 = _obj.TexCoords[face.TexCoordIndices[2]] * _texSize;
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
                        _islandsMap[x + y * _texSize] = islandIndex;
                    }
                );
            }
        }

        public static void Bleed()
        {
            if (_wbmp2 == null)
                return;
            if (_islandsMap == null)
                return;

            WindowsManager._mainWindow.Dispatcher.Invoke(() =>
            {
                int w = _wbmp2.PixelWidth;
                int h = _wbmp2.PixelHeight;

                _wbmp2.Lock();
                try
                {
                    unsafe
                    {
                        int stride = _wbmp2.BackBufferStride;
                        int bytesPerPixel = (_wbmp2.Format.BitsPerPixel + 7) / 8;
                        byte* buffer = (byte*)_wbmp2.BackBuffer;

                        bool[] visited = new bool[w * h];
                        Queue<(int x, int y)> queue = new Queue<(int x, int y)>();

                        for (int y = 0; y < h; y++)
                            for (int x = 0; x < w; x++)
                            {
                                int idx = x + y * w;
                                if (_islandsMap[idx] != 0)
                                {
                                    queue.Enqueue((x, y));
                                    visited[idx] = true;
                                }
                            }

                        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
                        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

                        while (queue.Count > 0)
                        {
                            var (x, y) = queue.Dequeue();
                            int pixelOffset = y * stride + x * bytesPerPixel;

                            Color color = Color.FromArgb(
                                a: buffer[pixelOffset + 3],
                                r: buffer[pixelOffset + 2],
                                g: buffer[pixelOffset + 1],
                                b: buffer[pixelOffset]
                            );

                            for (int i = 0; i < 8; i++)
                            {
                                int nx = x + dx[i];
                                int ny = y + dy[i];
                                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                                int nIdx = nx + ny * w;
                                if (visited[nIdx]) continue;

                                if (_islandsMap[nIdx] == 0)
                                {
                                    int nOffset = ny * stride + nx * bytesPerPixel;
                                    buffer[nOffset] = color.B;
                                    buffer[nOffset + 1] = color.G;
                                    buffer[nOffset + 2] = color.R;
                                    buffer[nOffset + 3] = color.A;
                                }

                                visited[nIdx] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }

                        _wbmp2.AddDirtyRect(new Int32Rect(0, 0, w, h));
                    }
                }
                finally
                {
                    _wbmp2.Unlock();
                }
            });
        }
    }
}
