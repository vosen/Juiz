using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MALContent
{
    public class ExtractionResult
    {
        public ExtractionResultType Response { get; internal set; }
        public IList<AnimeRating> Ratings { get; internal set; }
    }
}
