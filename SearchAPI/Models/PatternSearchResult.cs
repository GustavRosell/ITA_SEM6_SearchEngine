using System.Collections.Generic;

namespace SearchAPI.Models
{
    public class PatternSearchResult
    {
        public string Pattern { get; set; } = string.Empty;
        public List<PatternDocumentHit> Hits { get; private set; }
        // Total number of documents that contained at least one word matching the pattern (before limit)
        public int TotalDocuments { get; private set; }
        // Number of documents actually returned (after limit)
        public int ReturnedDocuments { get; private set; }
        // Convenience flag to indicate truncation
        public bool IsTruncated => ReturnedDocuments < TotalDocuments;
        // See semantics below.
        public int TotalHits { get; private set; }
        public int ReturnedHits { get; private set; }

        /* Semantics of hits:
         *  - Literal pattern (no * or ?): TotalHits = sum of occurrences of the literal across all matching documents (pre-limit)
         *                                ReturnedHits = sum of occurrences within the returned set
         *  - Wildcard pattern:           TotalHits = sum over all matching documents of the DISTINCT words that matched the pattern
         *                                ReturnedHits = same but only for returned (limited) documents
         * This keeps cost low while still communicating breadth. Upgrade later to true occurrence counts per word if needed.
         */

        public TimeSpan TimeUsed { get; set; }

        public PatternSearchResult(List<PatternDocumentHit> hits, int totalDocuments, int returnedDocuments, int totalHits, int returnedHits)
        {
            Hits = hits;
            TotalDocuments = totalDocuments;
            ReturnedDocuments = returnedDocuments;
            TotalHits = totalHits;
            ReturnedHits = returnedHits;
        }
    }
}