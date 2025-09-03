using System;
using System.Collections.Generic;
using Shared.Model;

namespace SearchAPI.Models
{
    /*
     * A data class representing the result of a search.
     * Hits is the total number of documents containing at least one word from the query.
     * DocumentHits is the documents and the number of words from the query contained in the document - see
     * the class DocumentHit
     * Ignored contains words from the query not present in the document base.
     * TimeUsed is the timespan used to perform the search.
     */
    public class SearchResult
    {
        public SearchResult(String[] query, int totalDocuments, List<DocumentHit> returnedDocuments, List<string> ignored, TimeSpan timeUsed, int totalHits, int returnedHits)
        {
            Query = query;
            TotalDocuments = totalDocuments;
            ReturnedDocuments = returnedDocuments;
            Ignored = ignored;
            TimeUsed = timeUsed;
            TotalHits = totalHits;
            ReturnedHits = returnedHits;
        }

        public String[] Query { get; }
        // Total number of documents that contain at least one query term
        public int TotalDocuments { get; }
        // Documents returned (after limit)
        public List<DocumentHit> ReturnedDocuments { get; }
        // Backwards compatibility: existing code used Hits to mean total documents
        public int Hits => TotalDocuments;
        // For truncation detection
        public bool IsTruncated => ReturnedDocuments.Count < TotalDocuments;
        // Total occurrences of all query terms across all matching documents (sum NoOfHits pre-limit)
        public int TotalHits { get; }
        // Occurrences within returned documents
        public int ReturnedHits { get; }
        public List<string> Ignored { get; }
        public TimeSpan TimeUsed { get; }
    }
}