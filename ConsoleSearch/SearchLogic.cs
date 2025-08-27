using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Model;

namespace ConsoleSearch
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
        public SearchResult Search(String[] query)
        {
            List<string> ignored;

            DateTime start = DateTime.Now;

            // Convert words to wordids
            var wordIds = mDatabase.GetWordIds(query, out ignored);

            // perform the search - get all docIds
            var docIds =  mDatabase.GetDocuments(wordIds);

            // get ids for the first maxAmount             
            var top = new List<int>();
            int limit = Config.ResultLimit.HasValue ? Math.Min(Config.ResultLimit.Value, docIds.Count) : docIds.Count;
            foreach (var p in docIds.GetRange(0, limit))
                top.Add(p.Key);

            // compose the result.
            // all the documentHit
            List<DocumentHit> docresult = new List<DocumentHit>();
            int idx = 0;
            foreach (var doc in mDatabase.GetDocDetails(top))
            {
                var missing = mDatabase.WordsFromIds(mDatabase.getMissing(doc.mId, wordIds));
                  
                docresult.Add(new DocumentHit(doc, docIds[idx++].Value, missing));
            }

            return new SearchResult(query, docIds.Count, docresult, ignored, DateTime.Now - start);
        }

        public PatternSearchResult PatternSearch(string pattern)
        {
            // Step 1: Find all words in the database that match the pattern
            var matchingWords = mDatabase.GetWordsMatchingPattern(pattern);
            if (matchingWords.Count == 0)
            {
                return new PatternSearchResult(new List<PatternDocumentHit>());
            }

            // Step 2: Find which documents contain these words and which specific words are in each
            var docsWithWords = mDatabase.GetDocsWithMatchingWords(matchingWords);

            // Step 3: Get the full details for the documents we found
            var docIds = new List<int>(docsWithWords.Keys);
            var docDetails = mDatabase.GetDocDetails(docIds);
            var docDetailsMap = docDetails.ToDictionary(d => d.mId);

            // Step 4: Assemble the final result
            var hits = new List<PatternDocumentHit>();
            foreach (var entry in docsWithWords)
            {
                int docId = entry.Key;
                List<string> wordsInDoc = entry.Value;
                if (docDetailsMap.ContainsKey(docId))
                {
                    hits.Add(new PatternDocumentHit(docDetailsMap[docId], wordsInDoc));
                }
            }

            return new PatternSearchResult(hits);
        }
    }
}
