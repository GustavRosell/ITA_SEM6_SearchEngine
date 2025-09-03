using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Model;
using SearchAPI.Data;
using SearchAPI.Models;

namespace SearchAPI.Services
{
    public class SearchLogic
    {
        IDatabase mDatabase;

        public SearchLogic(IDatabase database)
        {
            mDatabase = database;
        }

        /* Perform search of documents containing words from query. The result will
         * contain details about amost maxAmount of documents.
         */
        public SearchResult Search(String[] query, int? resultLimit = null, bool caseSensitive = false)
        {
            List<string> ignored;
            DateTime start = DateTime.Now;

            var wordIds = mDatabase.GetWordIds(query, out ignored, caseSensitive);
            var docIds = mDatabase.GetDocuments(wordIds); // list of KeyValuePair<docId, occurrences>

            int totalDocuments = docIds.Count;
            int limit = resultLimit.HasValue ? Math.Min(resultLimit.Value, totalDocuments) : totalDocuments;
            var topDocIds = docIds.GetRange(0, limit).Select(p => p.Key).ToList();

            var returnedDocHits = new List<DocumentHit>();
            int idx = 0;
            foreach (var doc in mDatabase.GetDocDetails(topDocIds))
            {
                var missing = mDatabase.WordsFromIds(mDatabase.getMissing(doc.mId, wordIds));
                // docIds[idx].Value = total occurrences of all query terms in that doc
                returnedDocHits.Add(new DocumentHit(doc, docIds[idx++].Value, missing));
            }

            int totalHitsAll = docIds.Sum(kvp => kvp.Value); // sum of occurrences across all documents
            int returnedHits = returnedDocHits.Sum(d => d.NoOfHits);

            return new SearchResult(query, totalDocuments, returnedDocHits, ignored, DateTime.Now - start, totalHitsAll, returnedHits);
        }

        public PatternSearchResult PatternSearch(string pattern, int? resultLimit = null, bool caseSensitive = false)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (string.IsNullOrWhiteSpace(pattern))
            {
                sw.Stop();
                return new PatternSearchResult(new List<PatternDocumentHit>(), 0, 0, 0, 0) { Pattern = pattern, TimeUsed = sw.Elapsed };
            }

            bool isLiteral = !pattern.Contains('*') && !pattern.Contains('?');

            if (isLiteral)
            {
                // Full search (no limit) to compute totals (already measures its own time)
                var full = Search(new[] { pattern }, null, caseSensitive);
                int totalDocs = full.TotalDocuments;
                int totalHits = full.TotalHits;

                // Apply limit to already materialized returned docs from 'full'
                var fullReturned = full.ReturnedDocuments; // contains all docs because we called with no limit
                var limited = (resultLimit.HasValue && resultLimit.Value > 0)
                    ? fullReturned.Take(resultLimit.Value).ToList()
                    : fullReturned.ToList();

                int returnedDocs = limited.Count;
                int returnedHits = limited.Sum(h => h.NoOfHits);
                var mapped = limited.Select(d => new PatternDocumentHit(d.Document, new List<string> { pattern })).ToList();
                sw.Stop();
                // Combine internal search time with any mapping overhead for pattern wrapper
                var combined = full.TimeUsed + sw.Elapsed;
                return new PatternSearchResult(mapped, totalDocs, returnedDocs, totalHits, returnedHits) { Pattern = pattern, TimeUsed = combined };
            }

            // Wildcard path
            var matchingWords = mDatabase.GetWordsMatchingPattern(pattern, caseSensitive);
            if (matchingWords.Count == 0)
            {
                sw.Stop();
                return new PatternSearchResult(new List<PatternDocumentHit>(), 0, 0, 0, 0) { Pattern = pattern, TimeUsed = sw.Elapsed };
            }

            var docsWithWords = mDatabase.GetDocsWithMatchingWords(matchingWords);
            if (docsWithWords.Count == 0)
            {
                sw.Stop();
                return new PatternSearchResult(new List<PatternDocumentHit>(), 0, 0, 0, 0) { Pattern = pattern, TimeUsed = sw.Elapsed };
            }

            var ordered = docsWithWords
                .Select(kvp => new DocWordMatch { DocId = kvp.Key, Words = kvp.Value })
                .ToList();

            int totalDocuments = ordered.Count;

            // Compute distinct words per doc (before ordering) because ordering metric will be count of distinct
            foreach (var dw in ordered)
            {
                dw.Words = dw.Words
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Total hits = sum of distinct words counts pre-limit
            int totalHitsWildcard = ordered.Sum(x => x.Words.Count);

            // Get document details for sorting by filename
            var allDocIds = ordered.Select(o => o.DocId).ToList();
            var allDetails = mDatabase.GetDocDetails(allDocIds);
            var docMap = allDetails.ToDictionary(d => d.mId);

            // Order: most distinct matches first, then by filename number
            ordered = ordered
                .OrderByDescending(x => x.Words.Count)
                .ThenBy(x => docMap.ContainsKey(x.DocId) ? GetFilenameNumber(docMap[x.DocId]) : int.MaxValue)
                .ToList();

            var limitedOrdered = (resultLimit.HasValue && resultLimit.Value > 0)
                ? ordered.Take(resultLimit.Value).ToList()
                : ordered;

            int returnedDocsWildcard = limitedOrdered.Count;
            int returnedHitsWildcard = limitedOrdered.Sum(x => x.Words.Count);

            var finalDocIds = limitedOrdered.Select(o => o.DocId).ToList();
            var details = mDatabase.GetDocDetails(finalDocIds);
            var map = details.ToDictionary(d => d.mId);

            var hits = new List<PatternDocumentHit>();
            foreach (var o in limitedOrdered)
            {
                if (!map.TryGetValue(o.DocId, out var doc))
                    continue;
                hits.Add(new PatternDocumentHit(doc, o.Words));
            }

            sw.Stop();
            return new PatternSearchResult(hits, totalDocuments, returnedDocsWildcard, totalHitsWildcard, returnedHitsWildcard)
            {
                Pattern = pattern,
                TimeUsed = sw.Elapsed
            };
        }

        private static int GetFilenameNumber(BEDocument document)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(document.mUrl);
                if (int.TryParse(fileName, out int number))
                {
                    return number;
                }
                return int.MaxValue;
            }
            catch
            {
                return int.MaxValue;
            }
        }

        private class DocWordMatch
        {
            public int DocId { get; set; }
            public List<string> Words { get; set; } = new();
        }
    }
}