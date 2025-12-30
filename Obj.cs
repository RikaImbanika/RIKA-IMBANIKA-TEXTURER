using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace RIKA_IMBANIKA_TEXTURER
{
    public class Obj
    {
        public List<Vector3> Vertices { get; } = new();
        public List<Vector2> TexCoords { get; } = new();
        public List<Vector3> Normals { get; } = new();
        public List<Face> Faces { get; } = new();

        public static Obj Parse(string path)
        {
            var obj = new Obj();
            var lines = File.ReadAllLines(path);

            foreach (var line in lines)
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                switch (parts[0])
                {
                    case "v":
                        obj.Vertices.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                        break;

                    case "vt":
                        obj.TexCoords.Add(new Vector2(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture)));
                        break;

                    case "vn":
                        obj.Normals.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                        break;

                    case "f":
                        var face = new Face();
                        for (int i = 1; i < parts.Length; i++)
                        {
                            var indices = parts[i].Split('/');
                            face.VertexIndices.Add(int.Parse(indices[0]) - 1);

                            if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
                                face.TexCoordIndices.Add(int.Parse(indices[1]) - 1);

                            if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
                                face.NormalIndices.Add(int.Parse(indices[2]) - 1);
                        }
                        obj.Faces.Add(face);
                        break;
                }
            }

            return obj;
        }

        public void Triangulate()
        {
            var triangulatedFaces = new List<Face>();

            foreach (var face in Faces)
            {
                if (face.VertexIndices.Count == 3)
                {
                    triangulatedFaces.Add(face);
                    continue;
                }

                for (int i = 1; i < face.VertexIndices.Count - 1; i++)
                {
                    var newFace = new Face();

                    newFace.VertexIndices.Add(face.VertexIndices[0]);
                    newFace.VertexIndices.Add(face.VertexIndices[i]);
                    newFace.VertexIndices.Add(face.VertexIndices[i + 1]);

                    if (face.TexCoordIndices.Count > 0)
                    {
                        newFace.TexCoordIndices.Add(face.TexCoordIndices[0]);
                        newFace.TexCoordIndices.Add(face.TexCoordIndices[i]);
                        newFace.TexCoordIndices.Add(face.TexCoordIndices[i + 1]);
                    }

                    if (face.NormalIndices.Count > 0)
                    {
                        newFace.NormalIndices.Add(face.NormalIndices[0]);
                        newFace.NormalIndices.Add(face.NormalIndices[i]);
                        newFace.NormalIndices.Add(face.NormalIndices[i + 1]);
                    }

                    triangulatedFaces.Add(newFace);
                }
            }

            Faces.Clear();
            Faces.AddRange(triangulatedFaces);
        }

        public float GetMaxSize()
        {
            if (Vertices.Count == 0)
                return 0f;

            float minX = Vertices.Min(v => v.X);
            float maxX = Vertices.Max(v => v.X);
            float minY = Vertices.Min(v => v.Y);
            float maxY = Vertices.Max(v => v.Y);
            float minZ = Vertices.Min(v => v.Z);
            float maxZ = Vertices.Max(v => v.Z);

            return Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
        }
    }
}
