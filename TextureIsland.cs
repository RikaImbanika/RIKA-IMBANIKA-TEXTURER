using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RIKA_IMBANIKA_TEXTURER
{
    public class TextureIsland
    {
        public List<int> FaceIndices { get; } = new();
        public List<System.Numerics.Vector2> UVs { get; } = new();
        public Rect Bounds { get; private set; }

        public void CalculateBounds()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var uv in UVs)
            {
                minX = Math.Min(minX, uv.X);
                maxX = Math.Max(maxX, uv.X);
                minY = Math.Min(minY, uv.Y);
                maxY = Math.Max(maxY, uv.Y);
            }

            Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
