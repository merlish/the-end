using System;
using System.Collections.Generic;

namespace Cubist
{
	public class ChunkMan
	{
        private Dictionary<long,Chunk> chunks = new Dictionary<long,Chunk>();

		public Chunk this[int x, int y, int z]
        {
            // store as z->x->y (b/c we often for .. on y, when telling user about chunks)
            get { return chunks[(16*(z/16)+(x/16))*16+(y/16)]; }
            set { chunks.Add((16*(z/16)+(x/16))*16+(y/16), value); }
        }

        public void Load(Chunk chuck)
        {
            
        }
	}
}

