using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_IMBANIKA_TEXTURER
{
    public static class PortalDetector
    {
        public static List<Portal> _portals;
        public static Dictionary<(int island, int v1, int v2), Portal> _portalMap;
        public static void DetectPortals(Obj obj, List<TextureIsland> islands)
        {
            int[] islandIdxForFace = new int[obj.Faces.Count];
            Array.Fill(islandIdxForFace, -1);
            for (int i = 0; i < islands.Count; i++)
                foreach (int faceIdx in islands[i].FaceIndices)
                    islandIdxForFace[faceIdx] = i;

            var edgeToFaces = new Dictionary<(int, int), List<int>>();
            for (int faceIdx = 0; faceIdx < obj.Faces.Count; faceIdx++)
            {
                var face = obj.Faces[faceIdx];
                var verts = face.VertexIndices;
                for (int j = 0; j < 3; j++)
                {
                    int v1 = verts[j];
                    int v2 = verts[(j + 1) % 3];
                    if (v1 > v2) (v1, v2) = (v2, v1);
                    var key = (v1, v2);
                    if (!edgeToFaces.TryGetValue(key, out var list))
                        edgeToFaces[key] = new List<int>();
                    edgeToFaces[key].Add(faceIdx);
                }
            }

            var portals = new List<Portal>();
            foreach (var kv in edgeToFaces)
            {
                var faces = kv.Value;
                if (faces.Count != 2) continue;

                int f1 = faces[0];
                int f2 = faces[1];
                int i1 = islandIdxForFace[f1];
                int i2 = islandIdxForFace[f2];

                if (i1 == -1 || i2 == -1) continue;

                var islandA = islands[i1];
                var islandB = islands[i2];

                var face1 = obj.Faces[f1];
                var face2 = obj.Faces[f2];

                int vA = kv.Key.Item1;
                int vB = kv.Key.Item2;

                int pos1A = -1, pos1B = -1;
                for (int k = 0; k < 3; k++)
                {
                    if (face1.VertexIndices[k] == vA) pos1A = k;
                    if (face1.VertexIndices[k] == vB) pos1B = k;
                }
                int vt1A = face1.TexCoordIndices[pos1A];
                int vt1B = face1.TexCoordIndices[pos1B];
                int localA1 = islandA.GlobalUvToLocal[vt1A];
                int localA2 = islandA.GlobalUvToLocal[vt1B];

                int pos2A = -1, pos2B = -1;
                for (int k = 0; k < 3; k++)
                {
                    if (face2.VertexIndices[k] == vA) pos2A = k;
                    if (face2.VertexIndices[k] == vB) pos2B = k;
                }
                int vt2A = face2.TexCoordIndices[pos2A];
                int vt2B = face2.TexCoordIndices[pos2B];
                int localB1 = islandB.GlobalUvToLocal[vt2A];
                int localB2 = islandB.GlobalUvToLocal[vt2B];

                bool createPortal = false;
                if (i1 != i2)
                {
                    createPortal = true;
                }
                else
                {
                    int a1 = localA1, a2 = localA2;
                    int b1 = localB1, b2 = localB2;
                    if (a1 > a2) (a1, a2) = (a2, a1);
                    if (b1 > b2) (b1, b2) = (b2, b1);
                    if (a1 != b1 || a2 != b2)
                        createPortal = true;
                }

                if (createPortal)
                {
                    portals.Add(new Portal
                    {
                        IslandA = i1,
                        LocalA1 = localA1,
                        LocalA2 = localA2,
                        IslandB = i2,
                        LocalB1 = localB1,
                        LocalB2 = localB2
                    });
                }
            }

            _portals = portals;

            _portalMap = new Dictionary<(int island, int v1, int v2), Portal>();

            foreach (var portal in portals)
            {
                int a1 = portal.LocalA1;
                int a2 = portal.LocalA2;
                if (a1 > a2) (a1, a2) = (a2, a1);
                var keyA = (portal.IslandA, a1, a2);
                if (!_portalMap.ContainsKey(keyA))
                    _portalMap[keyA] = portal;

                int b1 = portal.LocalB1;
                int b2 = portal.LocalB2;
                if (b1 > b2) (b1, b2) = (b2, b1);
                var keyB = (portal.IslandB, b1, b2);
                if (!_portalMap.ContainsKey(keyB))
                    _portalMap[keyB] = portal;
            }
        }

        public static (int island, int v1, int v2)? GetAdjacentEdge(int island, int v1, int v2)
        {
            int min = v1 < v2 ? v1 : v2;
            int max = v1 > v2 ? v1 : v2;
            var key = (island, min, max);

            if (_portalMap.TryGetValue(key, out Portal portal))
            {
                if (portal.IslandA == island)
                {
                    if (v1 == portal.LocalA1 && v2 == portal.LocalA2)
                        return (portal.IslandB, portal.LocalB1, portal.LocalB2);
                    else if (v1 == portal.LocalA2 && v2 == portal.LocalA1)
                        return (portal.IslandB, portal.LocalB2, portal.LocalB1);
                    else
                        return (portal.IslandB, portal.LocalB1, portal.LocalB2);
                }
                else
                {
                    if (v1 == portal.LocalB1 && v2 == portal.LocalB2)
                        return (portal.IslandA, portal.LocalA1, portal.LocalA2);
                    else if (v1 == portal.LocalB2 && v2 == portal.LocalB1)
                        return (portal.IslandA, portal.LocalA2, portal.LocalA1);
                    else
                        return (portal.IslandA, portal.LocalA1, portal.LocalA2);
                }
            }
            return null;
        }
    }
}
