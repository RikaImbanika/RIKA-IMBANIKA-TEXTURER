using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public class GraphPoint
    {
        public int _number;
        public Vector3 _value;
        public List<int> _neighbours;
        public List<float> _distances;
    }
}
