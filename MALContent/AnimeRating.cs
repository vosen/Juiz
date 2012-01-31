using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MALContent
{
    public struct AnimeRating
    {
        internal int animeId;
        internal byte rating;

        public int AnimeId { get { return animeId; } }
        public byte Rating { get { return rating; } }

        public AnimeRating(int id, byte score)
        {
            animeId = id;
            rating = score;
        }
    }
}
