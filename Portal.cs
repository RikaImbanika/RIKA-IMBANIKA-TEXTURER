
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_IMBANIKA_TEXTURER
{
    public struct Portal
    {
        public int IslandA;          // индекс первого острова
        public int LocalA1;          // локальный индекс первой вершины ребра в IslandA
        public int LocalA2;          // локальный индекс второй вершины ребра в IslandA
        public int IslandB;          // индекс второго острова
        public int LocalB1;          // локальный индекс первой вершины ребра в IslandB
        public int LocalB2;          // локальный индекс второй вершины ребра в IslandB
    }
}
