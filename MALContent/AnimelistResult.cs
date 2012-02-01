using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MALContent
{
    public class AnimelistResult
    {
        public AnimelistResponse Response { get; internal set; }
        public IList<AnimeRating> Ratings { get; internal set; }
    }
}
