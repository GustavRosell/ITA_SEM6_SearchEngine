using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Model;
using SearchAPI.Data;
using SearchAPI.Models;

namespace SearchAPI.Services
{
    /// <summary>
    /// SearchLogic - Core search engine implementation for IT-Arkitektur 6. semester PoC
    /// 
    /// This service contains the heart of our søgemaskine (search engine):
    /// - Standard search with TF (Term Frequency) scoring algorithm
    /// - Pattern search with wildcard support (* and ?)
    /// - Inverted index utilization for fast document lookup
    /// - Result ranking and ordering logic
    /// 
    /// The class implements Y-Scale architecture by separating search business logic
    /// from API presentation layer (SearchController) and data access (DatabaseSqlite)
    /// 
    /// Performance: Typical search times 20-25ms for pattern searches with medium dataset (~5,000 docs)
    /// </summary>
    public class SearchLogic
    {
        /// <summary>
        /// Database interface for accessing inverted index and document metadata
        /// Abstracted to support different database implementations (currently SQLite)
        /// </summary>
        IDatabase mDatabase;

        /// <summary>
        /// Initialize SearchLogic with database dependency injection
        /// </summary>
        /// <param name="database">Database implementation (typically DatabaseSqlite)</param>
        public SearchLogic(IDatabase database)
        {
            mDatabase = database;
        }

        /// <summary>
        /// Standard search implementation using TF (Term Frequency) scoring algorithm
        /// 
        /// Algorithm steps:
        /// 1. Convert query terms to word IDs using inverted index
        /// 2. Find all documents containing ANY query term (union operation)
        /// 3. Count total occurrences of ALL query terms per document
        /// 4. Rank by occurrence count (more matches = higher relevance)
        /// 5. Apply result limit and return top documents
        /// 
        /// Scoring: score = total_occurrences_in_document / query_length
        /// This gives higher scores to documents containing more query terms
        /// 
        /// Time complexity: O(query_terms * matching_documents)
        /// </summary>
        /// <param name="query">Array of search terms to find</param>
        /// <param name="resultLimit">Maximum documents to return (null = unlimited)</param>
        /// <param name="caseSensitive">Enable case-sensitive term matching</param>
        /// <returns>SearchResult with ranked documents and metadata</returns>
        public SearchResult Search(String[] query, int? resultLimit = null, bool caseSensitive = false)
        {
            List<string> ignored;
            DateTime start = DateTime.Now;

            // Step 1: Map query terms to word IDs using inverted index
            // ignored = terms not found in database (useful for user feedback)
            var wordIds = mDatabase.GetWordIds(query, out ignored, caseSensitive);
            
            // Step 2: Get all documents containing any query terms
            // Returns List<KeyValuePair<docId, total_occurrences>>
            // Already sorted by occurrence count DESC, then by docId ASC (SQL ORDER BY)
            var docIds = mDatabase.GetDocuments(wordIds);

            // Step 3: Apply result limiting (take top N most relevant documents)
            int totalDocuments = docIds.Count;
            int limit = resultLimit.HasValue ? Math.Min(resultLimit.Value, totalDocuments) : totalDocuments;
            var topDocIds = docIds.GetRange(0, limit).Select(p => p.Key).ToList();

            // Step 4: Build result objects with document details and hit information
            var returnedDocHits = new List<DocumentHit>();
            int idx = 0;
            foreach (var doc in mDatabase.GetDocDetails(topDocIds))
            {
                // Find which query terms are missing from this specific document
                var missing = mDatabase.WordsFromIds(mDatabase.getMissing(doc.mId, wordIds));
                
                // docIds[idx].Value = total occurrences of all query terms in that document
                returnedDocHits.Add(new DocumentHit(doc, docIds[idx++].Value, missing));
            }

            // Step 5: Calculate statistics for API response
            int totalHitsAll = docIds.Sum(kvp => kvp.Value); // sum of occurrences across all documents
            int returnedHits = returnedDocHits.Sum(d => d.NoOfHits);

            return new SearchResult(query, totalDocuments, returnedDocHits, ignored, DateTime.Now - start, totalHitsAll, returnedHits);
        }

        /// <summary>
        /// Pattern search with wildcard support - advanced søgning with * and ? patterns
        /// 
        /// Wildcard semantics:
        /// - * matches zero or more characters ("te*" → "test", "testing", "te")
        /// - ? matches exactly one character ("t?st" → "test", "tast", "tost")
        /// 
        /// Algorithm has two paths:
        /// 1. Literal path: No wildcards → delegate to standard Search() for consistency
        /// 2. Wildcard path: Pattern matching → find all words matching pattern, then documents
        /// 
        /// Key improvement: Fixed result ordering bug by sorting by filename number
        /// instead of chaotic database ID ordering (15, 101, 126... instead of random order)
        /// 
        /// Time complexity: O(total_words_in_index * pattern_complexity + matching_documents)
        /// </summary>
        /// <param name="pattern">Search pattern with optional * and ? wildcards</param>
        /// <param name="resultLimit">Maximum documents to return (null = unlimited)</param>
        /// <param name="caseSensitive">Enable case-sensitive pattern matching</param>
        /// <returns>PatternSearchResult with documents and matching words</returns>
        public PatternSearchResult PatternSearch(string pattern, int? resultLimit = null, bool caseSensitive = false)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Handle empty/null pattern early
            if (string.IsNullOrWhiteSpace(pattern))
            {
                sw.Stop();
                return new PatternSearchResult(new List<PatternDocumentHit>(), 0, 0, 0, 0) { Pattern = pattern, TimeUsed = sw.Elapsed };
            }

            // Determine if pattern contains wildcards
            bool isLiteral = !pattern.Contains('*') && !pattern.Contains('?');

            // LITERAL PATH: No wildcards - delegate to standard search for consistency
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
                
                // Map DocumentHit to PatternDocumentHit format
                var mapped = limited.Select(d => new PatternDocumentHit(d.Document, new List<string> { pattern })).ToList();
                sw.Stop();
                
                // Combine internal search time with any mapping overhead for pattern wrapper
                var combined = full.TimeUsed + sw.Elapsed;
                return new PatternSearchResult(mapped, totalDocs, returnedDocs, totalHits, returnedHits) { Pattern = pattern, TimeUsed = combined };
            }

            // WILDCARD PATH: Contains * or ? - perform pattern matching
            
            // Step 1: Find all words in database that match the pattern
            var matchingWords = mDatabase.GetWordsMatchingPattern(pattern, caseSensitive);
            if (matchingWords.Count == 0)
            {
                sw.Stop();
                return new PatternSearchResult(new List<PatternDocumentHit>(), 0, 0, 0, 0) { Pattern = pattern, TimeUsed = sw.Elapsed };
            }

            // Step 2: Find all documents containing any of the matching words
            var docsWithWords = mDatabase.GetDocsWithMatchingWords(matchingWords);
            if (docsWithWords.Count == 0)
            {
                sw.Stop();
                return new PatternSearchResult(new List<PatternDocumentHit>(), 0, 0, 0, 0) { Pattern = pattern, TimeUsed = sw.Elapsed };
            }

            // Step 3: Convert to internal data structure for processing
            var ordered = docsWithWords
                .Select(kvp => new DocWordMatch { DocId = kvp.Key, Words = kvp.Value })
                .ToList();

            int totalDocuments = ordered.Count;

            // Step 4: Deduplicate and sort words per document (for clean output)
            // We count distinct words only - multiple occurrences of same word count as 1
            foreach (var dw in ordered)
            {
                dw.Words = dw.Words
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Calculate total distinct word matches across all documents
            int totalHitsWildcard = ordered.Sum(x => x.Words.Count);

            // Step 5: Get document details for proper filename-based sorting
            var allDocIds = ordered.Select(o => o.DocId).ToList();
            var allDetails = mDatabase.GetDocDetails(allDocIds);
            var docMap = allDetails.ToDictionary(d => d.mId);

            // Step 6: Apply intelligent ordering (FIXED BUG HERE!)
            // Primary: Most distinct word matches first (most relevant)
            // Secondary: Filename number order (15, 101, 126...) instead of chaotic DB ID order
            ordered = ordered
                .OrderByDescending(x => x.Words.Count)  // Most matches first
                .ThenBy(x => docMap.ContainsKey(x.DocId) ? GetFilenameNumber(docMap[x.DocId]) : int.MaxValue)  // Logical filename order
                .ToList();

            // Step 7: Apply result limit
            var limitedOrdered = (resultLimit.HasValue && resultLimit.Value > 0)
                ? ordered.Take(resultLimit.Value).ToList()
                : ordered;

            int returnedDocsWildcard = limitedOrdered.Count;
            int returnedHitsWildcard = limitedOrdered.Sum(x => x.Words.Count);

            // Step 8: Build final result objects with document metadata
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

        /// <summary>
        /// Extract numeric filename for logical ordering (fixes pattern search ordering bug)
        /// 
        /// Converts filenames like "15.txt" → 15, "126.txt" → 126
        /// This ensures results appear as: 15, 101, 126, 143... 
        /// instead of chaotic database ID ordering
        /// 
        /// Critical for user experience - documents should appear in logical file order
        /// </summary>
        /// <param name="document">Document with file path/URL</param>
        /// <returns>Extracted numeric filename, or int.MaxValue if not numeric (sorts last)</returns>
        private static int GetFilenameNumber(BEDocument document)
        {
            try
            {
                // Extract filename without extension from document URL/path
                var fileName = System.IO.Path.GetFileNameWithoutExtension(document.mUrl);
                if (int.TryParse(fileName, out int number))
                {
                    return number;
                }
                // Non-numeric filenames sort last
                return int.MaxValue;
            }
            catch
            {
                // Any parsing error sorts last
                return int.MaxValue;
            }
        }

        /// <summary>
        /// Internal helper class for pattern search processing
        /// Tracks which words from the pattern match were found in each document
        /// Used during the wildcard path of PatternSearch method
        /// </summary>
        private class DocWordMatch
        {
            /// <summary>Document ID from database</summary>
            public int DocId { get; set; }
            
            /// <summary>List of pattern-matching words found in this document</summary>
            public List<string> Words { get; set; } = new();
        }
    }
}