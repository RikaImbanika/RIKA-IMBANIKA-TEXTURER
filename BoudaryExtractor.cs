using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class BoundaryExtractor
{
    public static List<List<Vector2>> GetBoundaryContours(List<Vector2> uvs, List<(int, int, int)> triangles)
    {
        var edgeCount = new Dictionary<(int, int), int>();
        foreach (var (a, b, c) in triangles)
        {
            AddEdge(a, b, edgeCount);
            AddEdge(b, c, edgeCount);
            AddEdge(c, a, edgeCount);
        }

        var adj = new Dictionary<int, List<int>>();
        foreach (var kv in edgeCount.Where(kv => kv.Value == 1))
        {
            var (u, v) = kv.Key;
            if (!adj.ContainsKey(u)) adj[u] = new List<int>();
            if (!adj.ContainsKey(v)) adj[v] = new List<int>();
            adj[u].Add(v);
            adj[v].Add(u);
        }

        var visitedVertices = new HashSet<int>();
        var cyclesIndices = new List<List<int>>();

        foreach (var start in adj.Keys.ToList())
        {
            if (visitedVertices.Contains(start)) continue;

            var cycle = new List<int>();
            int current = start;
            int prev = -1;

            do
            {
                cycle.Add(current);
                visitedVertices.Add(current);

                var nextCandidates = adj[current].Where(n => n != prev).ToList();
                if (nextCandidates.Count == 0) break;
                int next = nextCandidates[0];

                prev = current;
                current = next;
            }
            while (current != start && !visitedVertices.Contains(current));

            if (current == start)
            {
                cyclesIndices.Add(cycle);
            }
        }

        float SignedArea(List<int> indices)
        {
            float area = 0;
            int n = indices.Count;
            for (int i = 0; i < n; i++)
            {
                var p1 = uvs[indices[i]];
                var p2 = uvs[indices[(i + 1) % n]];
                area += (p2.X - p1.X) * (p2.Y + p1.Y);
            }
            return area * 0.5f;
        }

        bool PointInPolygon(Vector2 point, List<int> polyIndices)
        {
            int n = polyIndices.Count;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                var vi = uvs[polyIndices[i]];
                var vj = uvs[polyIndices[j]];

                if (((vi.Y > point.Y) != (vj.Y > point.Y)) &&
                    (point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        int cycleCount = cyclesIndices.Count;
        var parent = new int[cycleCount];
        for (int i = 0; i < cycleCount; i++) parent[i] = -1;

        for (int i = 0; i < cycleCount; i++)
        {
            for (int j = 0; j < cycleCount; j++)
            {
                if (i == j) continue;
                
                if (PointInPolygon(uvs[cyclesIndices[i][0]], cyclesIndices[j]))
                {
                    parent[i] = j;
                    break;
                }
            }
        }

        var result = new List<List<Vector2>>();
        for (int i = 0; i < cycleCount; i++)
        {
            float area = SignedArea(cyclesIndices[i]);
            bool isOuter = parent[i] == -1;

            List<int> finalIndices;
            if (isOuter && area < 0)
                finalIndices = Enumerable.Reverse(cyclesIndices[i]).ToList();
            else if (!isOuter && area > 0)
                finalIndices = Enumerable.Reverse(cyclesIndices[i]).ToList();
            else
                finalIndices = cyclesIndices[i];

            var contour = finalIndices.Select(idx => uvs[idx]).ToList();
            result.Add(contour);
        }

        return result;
    }

    private static void AddEdge(int u, int v, Dictionary<(int, int), int> dict)
    {
        if (u > v) (u, v) = (v, u);
        var key = (u, v);
        dict.TryGetValue(key, out int count);
        dict[key] = count + 1;
    }
}