using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_IMBANIKA_TEXTURER
{
    public class IslandDetector
    {
        public static List<TextureIsland> DetectIslands(Obj obj)
        {
            var islands = new List<TextureIsland>();
            var visitedFaces = new bool[obj.Faces.Count];

            for (int i = 0; i < obj.Faces.Count; i++)
            {
                if (!visitedFaces[i])
                {
                    var island = new TextureIsland();
                    FloodFillIsland(obj, i, visitedFaces, island);
                    island.CalculateBounds();
                    islands.Add(island);
                }
            }

            return islands;
        }

        private static void FloodFillIsland(Obj obj, int startFaceIndex,
        bool[] visitedFaces, TextureIsland island)
        {
            var stack = new Stack<int>();
            stack.Push(startFaceIndex);

            while (stack.Count > 0)
            {
                int faceIdx = stack.Pop();
                if (visitedFaces[faceIdx]) continue;

                visitedFaces[faceIdx] = true;
                island.FaceIndices.Add(faceIdx);

                var face = obj.Faces[faceIdx];

                int[] triIndices = new int[3];
                for (int k = 0; k < face.TexCoordIndices.Count; k++)
                {
                    int oldUvIdx = face.TexCoordIndices[k];
                    if (!island.GlobalUvToLocal.TryGetValue(oldUvIdx, out int localIdx))
                    {
                        localIdx = island.UVs.Count;
                        island.UVs.Add(obj.TexCoords[oldUvIdx]);
                        island.GlobalUvToLocal[oldUvIdx] = localIdx;
                    }
                    triIndices[k] = localIdx;
                }

                island.Triangles.Add((triIndices[0], triIndices[1], triIndices[2]));

                foreach (int neighborIdx in FindNeighborFaces(obj, faceIdx, visitedFaces))
                {
                    if (!visitedFaces[neighborIdx])
                        stack.Push(neighborIdx);
                }
            }
        }

        private static List<int> FindNeighborFaces(Obj obj, int faceIdx, bool[] visited)
        {
            var neighbors = new List<int>();
            var currentFace = obj.Faces[faceIdx];
            var currentUVs = currentFace.TexCoordIndices
                .Select(idx => obj.TexCoords[idx])
                .ToList();

            for (int i = 0; i < obj.Faces.Count; i++)
            {
                if (i == faceIdx || visited[i]) continue;

                var otherFace = obj.Faces[i];
                var otherUVs = otherFace.TexCoordIndices
                    .Select(idx => obj.TexCoords[idx])
                    .ToList();

                if (currentUVs.Intersect(otherUVs).Any())
                    neighbors.Add(i);
            }

            return neighbors;
        }
    }
}
