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
        public static List<int> _border;

        public static int _t;

        public static List<((double x, double y) point, (double x, double y) direction, int nextCirId, int prevCirId)> _nextPoints;

        public static void Do(int texSize, float scaler, int circlesLimit)
        {
            Thread mt = new Thread(MT);
            mt.Start();

            void MT()
            {
                _texSize = texSize;
                _scaler = scaler;
                _radiuses = new float[texSize * texSize];
                _islands = new ushort[texSize * texSize];
                _nextPoints = new List<((double x, double y) point, (double x, double y) direction, int nextCirId, int prevCirId)>();

                WriteableBitmap wbmp = WBMP.Create(_texSize);

                List<TextureIsland> islands = IslandDetector.DetectIslands(_obj);
                for (ushort island = 1; island <= islands.Count; island++)
                {
                    List<((double x, double y) point, (double x, double y) direction)> newOnes = new List<((double x, double y) point, (double x, double y) direction)>();

                    while (newOnes.Count < 2)
                    {
                        FillSizes(islands[island - 1].FaceIndices, island);
                        Rect bounds = islands[island - 1].Bounds;

                        _circles = new List<Vector2>();
                        _circlesRadiuses = new List<float>();
                        _border = new List<int>();
                        List<Vector2> nextPoints = new List<Vector2>();

                        Vector2 point = GetStartPoint(bounds, island);

                        _circles.Add(point);
                        _circlesRadiuses.Add(GetRadius(point));
                        _border.Add(0);

                        point = GetSecondPoint(bounds, island, point, _circlesRadiuses[0]);

                        _circles.Add(point);
                        _circlesRadiuses.Add(GetRadius(point));
                        _border.Add(1);

                        newOnes = CirclesLogic.FindIntersectionPointsWithNormals(_circles[0].X, _circles[0].Y, _circlesRadiuses[0], _circles[1].X, _circles[1].Y, _circlesRadiuses[1]);

                        if (newOnes.Count < 2)
                            continue;

                        _nextPoints.Add((newOnes[0].point, newOnes[0].direction, 1, 0));
                        _nextPoints.Add((newOnes[1].point, newOnes[1].direction, 0, 1));

                        int lim = 0;
                        while (_nextPoints.Count > 0 && lim < circlesLimit)
                        {                           
                            MoreMoreMore(_nextPoints[0].point, _nextPoints[0].direction, island, _nextPoints[0].nextCirId, _nextPoints[0].prevCirId);
                            _nextPoints.RemoveAt(0);
                            lim++;
                        }

                        for (int i = 0; i < _circles.Count; i++)
                        {
                            Color color = Rainbow.GetRainbowColor(_t);
                            WBMP.FillCircle(wbmp, _circles[i], _circlesRadiuses[i], color);
                            _t++;
                        }
                    }
                }

                _resImg = WBMP.ConvertToBitmapImage(wbmp);

                WindowsManager._mainWindow.Dispatcher.Invoke(() =>
                {
                    WindowsManager._mainWindow.img.Source = _resImg;
                });

                WBMP.SaveToPng(wbmp, $"{Disk._programFiles}Result.png");

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
                            Vector2 res = CircleToPointSolver.FindCenterPoint(dir, point, islandIndex) ?? Vector2.Zero;

                            if (GetIsland((int)res.X, (int)res.Y) == islandIndex)
                                return res;
                        }
                    }

                    return center + new Vector2(3, 0);
                }

                void MoreMoreMore((double x, double y) point, (double x, double y) direction, ushort islandIndex, int nextCirId, int prevCirId)
                {
                    //1

                    int circleId = _circles.Count;

                    int nextBorderId = _border.IndexOf(nextCirId);
                    int prevBorderId = _border.IndexOf(prevCirId);

                    int prevBorderId2 = nextBorderId - 1;
                    int nextBorderId2 = prevBorderId + 1;

                    if (nextBorderId2 == _border.Count)
                        nextBorderId2 = 0;

                    if (prevBorderId2 < 0)
                        prevBorderId2 = _border.Count - 1;

                    if (nextBorderId != nextBorderId2 || prevBorderId != prevBorderId2)
                        return; //This is if previous border already changed too much

                    //2

                    Vector2 dir = new Vector2((float)direction.x, (float)direction.y);
                    Vector2 pos = new Vector2((float)point.x, (float)point.y);

                    Vector2 res = CircleToPointSolver.FindCenterPoint(dir, pos, islandIndex) ?? Vector2.Zero;
                    (double x, double y) resT = (res.X, res.Y);

                    //3

                    if (GetIsland((int)res.X, (int)res.Y) == islandIndex)
                    {
                        float radius = GetRadius(res);

                        Forward();

                        //4

                        Backward();

                        //5

                        _circles.Add(res);
                        _circlesRadiuses.Add(radius);

                        _border.Insert(nextBorderId, circleId);

                        void Forward()
                        {
                            float minAngle = 1000f;
                            int bestBorderId = 0;
                            int bestSkip = 0;
                            ((double x, double y) point, (double x, double y) direction) bestPoint = ((0, 0), (0, 0));

                            if (nextBorderId == _border.Count)
                                nextBorderId = 0;

                            int localBorderId = nextBorderId;

                            for (int skip = 0; skip < _border.Count; skip++)
                            {
                                nextCirId = _border[localBorderId];
                                Vector2 bufdir = res - _circles[nextCirId];
                                bufdir /= bufdir.Length();
                                ((double x, double y) point, (double x, double y) direction) p = CirclesLogic.FindIntersection(res, radius, _circles[nextCirId], _circlesRadiuses[nextCirId], bufdir) ?? ((-404, -404), (1, 1));

                                if (p.point.x != -404)
                                {
                                    Vector2 v2 = new Vector2((float)p.point.x, (float)p.point.y) - res;
                                    float angle = CirclesLogic.GetAngleCounterClockwise(dir, v2);

                                    if (angle < minAngle)
                                    {
                                        if (skip > 0)
                                        {

                                        }

                                        minAngle = angle;
                                        bestBorderId = localBorderId;
                                        bestPoint = p;
                                        bestSkip = skip;
                                    }
                                }
                                
                                localBorderId++;

                                if (localBorderId == _border.Count)
                                    localBorderId = 0;
                            }

                            if (bestSkip > 0)
                            {
                                localBorderId = nextBorderId;

                                for (int skip = 0; skip < bestSkip; skip++)
                                {
                                    _border.RemoveAt(localBorderId);

                                    if (localBorderId == _border.Count)
                                        localBorderId = 0;
                                }
                            }

                            nextBorderId = localBorderId;

                            prevBorderId = nextBorderId - 1;
                            if (prevBorderId < 0)
                                prevBorderId = _border.Count - 1;

                            nextCirId = _border[nextBorderId];
                            prevCirId = _border[prevBorderId];

                            if (GetIsland((int)bestPoint.point.x, (int)bestPoint.point.y) == islandIndex)
                            {
                                _nextPoints.Add(((bestPoint.point.x, bestPoint.point.y), bestPoint.direction, nextCirId, circleId));
                            }

                            if (nextBorderId < 0)
                            {

                            }
                        }

                        void Backward()
                        {
                            float minAngle = 1000f;
                            int bestBorderId = 0;
                            int bestSkip = 0;
                            ((double x, double y) point, (double x, double y) direction) bestPoint = ((0, 0), (0, 0));

                            if (prevBorderId < 0)
                                prevBorderId = _border.Count - 1;

                            int localBorderId = prevBorderId;

                            for (int skip = 0; skip < _border.Count; skip++)
                            {
                                prevCirId = _border[localBorderId];
                                Vector2 bufdir = _circles[prevCirId] - res;
                                bufdir /= bufdir.Length();
                                ((double x, double y) point, (double x, double y) direction) p = CirclesLogic.FindIntersection(_circles[prevCirId], _circlesRadiuses[prevCirId], res, radius, bufdir) ?? ((-404, -404), (1, 1));

                                if (p.point.x != -404)
                                {
                                    Vector2 v2 = new Vector2((float)p.point.x, (float)p.point.y) - res;
                                    float angle = CirclesLogic.GetAngleClockwise(dir, v2);

                                    if (angle < minAngle)
                                    {
                                        if (skip > 0)
                                        {

                                        }

                                        minAngle = angle;
                                        bestBorderId = localBorderId;
                                        bestPoint = p;
                                        bestSkip = skip;
                                    }
                                }

                                localBorderId--;

                                if (localBorderId < 0)
                                    localBorderId = _border.Count - 1;
                            }

                            if (bestSkip > 0)
                            {
                                localBorderId = prevBorderId;

                                for (int skip = 0; skip < bestSkip; skip++)
                                {
                                    _border.RemoveAt(localBorderId);

                                    localBorderId--;

                                    if (localBorderId < 0)
                                        localBorderId = _border.Count - 1;
                                }
                            }

                            prevBorderId = localBorderId;

                            nextBorderId = prevBorderId + 1;

                            if (nextBorderId == _border.Count)
                                nextBorderId = 0;

                            prevCirId = _border[prevBorderId];

                            nextCirId = _border[nextBorderId];

                            if (GetIsland((int)bestPoint.point.x, (int)bestPoint.point.y) == islandIndex)
                            {
                                _nextPoints.Add(((bestPoint.point.x, bestPoint.point.y), bestPoint.direction, circleId, prevCirId)); //Don't even try to touch
                            }

                            if (nextBorderId < 0)
                            {

                            }
                        }
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
