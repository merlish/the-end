using System;

namespace Cubist
{
	public class FullChunk
	{
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        // Representing each block with an int.
        // Y'all can decode 'em in your own time.
		public int[] Blocks = new int[16*16*16];

        public FullChunk(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }
		
		public int this[int x, int y, int z]
		{
			get { return Blocks[((z * 16) + y) * 16 + x]; }
			set { 
                Blocks[((z * 16) + y) * 16 + x] = value;
            }
		}

        public bool TryPlaceBlock(int x, int y, int z, int block)
        {
            if (this[x,y,z] == 0) {
                this[x,y,z] = block; return true;
            } else
                return false;
        }
	}
}

