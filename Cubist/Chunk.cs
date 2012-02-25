using System;

namespace Cubist
{
    public abstract class Chunk
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        public Chunk(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }

        public int this[int x, int y, int z]
        {
            abstract get;
            abstract set;
        }

        public bool TryPlaceBlock(int x, int y, int z);
    }
}

