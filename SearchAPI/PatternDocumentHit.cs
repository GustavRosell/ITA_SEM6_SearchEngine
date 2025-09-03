using Shared.Model;
using System.Collections.Generic;

namespace SearchAPI
{
    public class PatternDocumentHit
    {
        public BEDocument Document { get; private set; }
        public List<string> MatchingWords { get; private set; }

        public PatternDocumentHit(BEDocument document, List<string> matchingWords)
        {
            Document = document;
            MatchingWords = matchingWords;
        }
    }
}