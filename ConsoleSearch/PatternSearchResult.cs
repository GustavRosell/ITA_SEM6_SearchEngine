using System.Collections.Generic;

namespace ConsoleSearch
{
    public class PatternSearchResult
    {
        public List<PatternDocumentHit> Hits { get; private set; }

        public PatternSearchResult(List<PatternDocumentHit> hits)
        {
            Hits = hits;
        }
    }
}
