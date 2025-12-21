using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public class Face
    {
        public List<int> VertexIndices { get; } = new();
        public List<int> TexCoordIndices { get; } = new();
        public List<int> NormalIndices { get; } = new();
    }
}
