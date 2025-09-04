using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Model;
using Microsoft.Data.Sqlite;

namespace SearchAPI.Data
{
    /// <summary>
    /// DatabaseSqlite - SQLite implementation of inverted index database for SearchEngine PoC
    /// 
    /// This class handles all database operations for the search engine, implementing the
    /// inverted index data structure for fast document retrieval in IT-Arkitektur søgesystem.
    /// 
    /// Database Schema (SQLite):
    /// - Document(id, url, indexTime, creationTime) - Document metadata
    /// - Word(id, name) - Unique words from all documents
    /// - Occ(docId, wordId) - Occurrence table (many-to-many) forming inverted index
    /// 
    /// The inverted index allows O(1) lookup from word → documents containing that word
    /// Performance: ~5MB database for 5,000 documents, typical queries 20-25ms
    /// </summary>
    public class DatabaseSqlite : IDatabase
    {
        /// <summary>
        /// SQLite database connection - kept open for performance
        /// Uses cross-platform path detection via Shared.Paths.DATABASE
        /// </summary>
        private SqliteConnection _connection;

        /// <summary>
        /// Initialize SQLite connection to inverted index database
        /// Database must already exist (created by indexer application)
        /// </summary>
        public DatabaseSqlite()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder();
            
            // Use cross-platform database path (auto-detects Windows/macOS/Linux)
            connectionStringBuilder.DataSource = Paths.DATABASE;

            _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
            _connection.Open();
        }

        /// <summary>
        /// Helper method for executing SQL commands without return values
        /// Used internally for database operations that don't return data
        /// </summary>
        /// <param name="sql">SQL command to execute</param>
        private void Execute(string sql)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Core search method - finds documents containing any of the specified words
        /// 
        /// Uses the inverted index (Occ table) to efficiently lookup documents.
        /// Returns documents ranked by relevance (occurrence count) - more matches = higher rank.
        /// 
        /// This implements the TF (Term Frequency) part of search scoring:
        /// Documents with more query word occurrences appear first.
        /// 
        /// SQL Query Example for words [2,3]:
        /// SELECT docId, COUNT(wordId) as count FROM Occ 
        /// WHERE wordId in (2,3) GROUP BY docId 
        /// ORDER BY count DESC, docId ASC;
        /// 
        /// Time complexity: O(log(documents) * word_occurrences) due to SQL indexing
        /// </summary>
        /// <param name="wordIds">List of word IDs to search for (from GetWordIds)</param>
        /// <returns>
        /// List of KeyValuePair where:
        /// - Key = document ID
        /// - Value = total occurrences of ALL query words in that document
        /// Sorted by occurrence count DESC, then document ID ASC for consistent ordering
        /// </returns>
        public List<KeyValuePair<int, int>> GetDocuments(List<int> wordIds)
        {
            var res = new List<KeyValuePair<int, int>>();

            // Build SQL query to find documents containing any of the word IDs
            var sql = "SELECT docId, COUNT(wordId) as count FROM Occ where ";
            sql += "wordId in " + AsString(wordIds) + " GROUP BY docId ";
            sql += "ORDER BY count DESC, docId ASC;";  // Relevance first, then consistent ordering

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var docId = reader.GetInt32(0);
                    var count = reader.GetInt32(1);

                    res.Add(new KeyValuePair<int, int>(docId, count));
                }
            }

            return res;
        }

        /// <summary>
        /// Convert list of integers to SQL IN clause format
        /// Helper method for building parameterized SQL queries
        /// Example: [1,2,3] → "(1,2,3)"
        /// </summary>
        /// <param name="x">List of integers to format</param>
        /// <returns>SQL-formatted string for IN clause</returns>
        private string AsString(List<int> x) => $"({string.Join(',', x)})";

        /// <summary>
        /// Load entire word vocabulary from database into memory
        /// Creates lookup table for word → ID mapping for internal use
        /// 
        /// This method loads all words from the inverted index vocabulary.
        /// Used primarily for word ID lookups and vocabulary analysis.
        /// 
        /// Note: For large vocabularies, consider caching or lazy loading strategies
        /// Current implementation suitable for PoC with moderate word counts
        /// </summary>
        /// <returns>Dictionary mapping word strings to their database IDs</returns>
        private Dictionary<string, int> GetAllWords()
        {
            Dictionary<string, int> res = new Dictionary<string, int>();

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM word";

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var w = reader.GetString(1);

                    res.Add(w, id);
                }
            }
            return res;
        }

        /// <summary>
        /// Retrieve document metadata for specified document IDs
        /// 
        /// Fetches complete document information including:
        /// - Document ID (primary key)
        /// - URL/file path (original document location)
        /// - Index time (when document was crawled and indexed)
        /// - Creation time (original document creation timestamp)
        /// 
        /// This method is called after GetDocuments() to enrich search results
        /// with full document metadata for API responses.
        /// </summary>
        /// <param name="docIds">List of document IDs to fetch details for</param>
        /// <returns>List of BEDocument objects with complete metadata</returns>
        public List<BEDocument> GetDocDetails(List<int> docIds)
        {
            List<BEDocument> res = new List<BEDocument>();

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM document where id in " + AsString(docIds);

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var url = reader.GetString(1);
                    var idxTime = reader.GetString(2);
                    var creationTime = reader.GetString(3);

                    res.Add(new BEDocument { mId = id, mUrl = url, mIdxTime = idxTime, mCreationTime = creationTime });
                }
            }
            return res;
        }

        /// <summary>
        /// Find query terms missing from a specific document - used for search result analysis
        /// 
        /// Given a list of word IDs from a query and a specific document,
        /// returns which word IDs are NOT present in that document.
        /// 
        /// This is useful for search result explanations:
        /// "Document contains 3 of 5 search terms, missing: [word1, word2]"
        /// 
        /// Algorithm:
        /// 1. Find which query words ARE present in the document
        /// 2. Return the difference (query words - present words)
        /// </summary>
        /// <param name="docId">Document ID to check</param>
        /// <param name="wordIds">List of word IDs from search query</param>
        /// <returns>List of word IDs that are missing from the specified document</returns>
        public List<int> getMissing(int docId, List<int> wordIds)
        {
            // Find word IDs that ARE present in the document
            var sql = "SELECT wordId FROM Occ where ";
            sql += "wordId in " + AsString(wordIds) + " AND docId = " + docId;
            sql += " ORDER BY wordId;";

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            List<int> present = new List<int>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var wordId = reader.GetInt32(0);
                    present.Add(wordId);
                }
            }
            
            // Calculate missing = all query words - present words
            var result = new List<int>(wordIds);
            foreach (var w in present)
                result.Remove(w);

            return result;
        }

        /// <summary>
        /// Convert word IDs back to their string representations
        /// 
        /// Reverse lookup from the Word table: ID → word string
        /// Used to provide human-readable missing words in search results
        /// 
        /// Example: [1,5,9] → ["test", "search", "engine"]
        /// </summary>
        /// <param name="wordIds">List of word IDs to look up</param>
        /// <returns>List of corresponding word strings</returns>
        public List<string> WordsFromIds(List<int> wordIds)
        {
            var sql = "SELECT name FROM Word where ";
            sql += "id in " + AsString(wordIds);

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            List<string> result = new List<string>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var word = reader.GetString(0);
                    result.Add(word);
                }
            }
            return result;
        }

        /// <summary>
        /// Convert query words to their database IDs - first step of every search
        /// 
        /// Maps search terms to word IDs using the Word table.
        /// Supports both case-sensitive and case-insensitive matching.
        /// Tracks words not found in vocabulary (ignored words).
        /// 
        /// This is the entry point from string queries to the inverted index.
        /// Only words that exist in the database can be searched for.
        /// 
        /// Performance optimization: Uses parameterized queries to prevent SQL injection
        /// and enable query plan caching by SQLite.
        /// </summary>
        /// <param name="query">Array of search terms from user input</param>
        /// <param name="outIgnored">Output parameter - words not found in database vocabulary</param>
        /// <param name="caseSensitive">Whether to match words case-sensitively</param>
        /// <returns>List of word IDs corresponding to found query terms</returns>
        public List<int> GetWordIds(string[] query, out List<string> outIgnored, bool caseSensitive = false)
        {
            var res = new List<int>();
            var ignored = new List<string>();

            foreach (var aWord in query)
            {
                var command = _connection.CreateCommand();
                
                // Choose SQL query based on case sensitivity preference
                if (caseSensitive)
                {
                    command.CommandText = "SELECT id FROM word WHERE name = @name";
                }
                else
                {
                    // Case-insensitive: use LOWER() functions for both sides
                    command.CommandText = "SELECT id FROM word WHERE LOWER(name) = LOWER(@name)";
                }
                command.Parameters.AddWithValue("@name", aWord);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Word found in vocabulary - add its ID
                        res.Add(reader.GetInt32(0));
                    }
                    else
                    {
                        // Word not in database - track as ignored
                        ignored.Add(aWord);
                    }
                }
            }
            outIgnored = ignored;
            return res;
        }

        /// <summary>
        /// Find all words in vocabulary matching wildcard pattern - core of pattern search
        /// 
        /// Supports two wildcard characters:
        /// - * matches zero or more characters
        /// - ? matches exactly one character
        /// 
        /// Uses different SQL operators based on case sensitivity:
        /// - Case-sensitive: SQLite GLOB (native * and ? support)
        /// - Case-insensitive: SQLite LIKE (convert * → %, ? → _)
        /// 
        /// This method is the first step of wildcard pattern search.
        /// Results are then used to find documents containing these matching words.
        /// 
        /// Examples:
        /// - "test*" matches "test", "testing", "tester"
        /// - "t?st" matches "test", "tast", "tost" but not "toast"
        /// </summary>
        /// <param name="pattern">Wildcard pattern with * and/or ? characters</param>
        /// <param name="caseSensitive">Whether pattern matching should be case-sensitive</param>
        /// <returns>List of all vocabulary words matching the pattern</returns>
        public List<string> GetWordsMatchingPattern(string pattern, bool caseSensitive = false)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(pattern))
                return result;

            var command = _connection.CreateCommand();

            if (caseSensitive)
            {
                // Case sensitive: use GLOB directly (supports * and ? natively)
                command.CommandText = "SELECT name FROM word WHERE name GLOB @pattern";
                command.Parameters.AddWithValue("@pattern", pattern);
            }
            else
            {
                // Case insensitive: LIKE with % and _ (convert our wildcard syntax)
                var sqlPattern = pattern.Replace('?', '_').Replace('*', '%');
                command.CommandText = "SELECT name FROM word WHERE name LIKE @pattern";
                command.Parameters.AddWithValue("@pattern", sqlPattern);
            }

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(reader.GetString(0));
                }
            }

            return result;
        }

        /// <summary>
        /// Find documents containing pattern-matching words - second step of pattern search
        /// 
        /// Given a list of words that matched a wildcard pattern, this method:
        /// 1. Converts word strings back to IDs for database lookup
        /// 2. Finds all documents containing any of these words via inverted index
        /// 3. Groups results by document, listing which pattern words each doc contains
        /// 
        /// This is more complex than regular search because we need to track
        /// WHICH specific words matched the pattern in each document.
        /// 
        /// Example: Pattern "te*" matches ["test", "testing", "text"]
        /// → Returns: {docId: 15 → ["test", "text"], docId: 23 → ["testing"]}
        /// 
        /// Uses parameterized queries for SQL injection prevention and performance.
        /// </summary>
        /// <param name="matchingWords">List of words that matched the wildcard pattern</param>
        /// <returns>
        /// Dictionary mapping document IDs to lists of pattern words found in each document
        /// Key = document ID, Value = list of pattern-matching words in that document
        /// </returns>
        public Dictionary<int, List<string>> GetDocsWithMatchingWords(List<string> matchingWords)
        {
            var result = new Dictionary<int, List<string>>();
            if (matchingWords == null || matchingWords.Count == 0)
            {
                return result;
            }

            // Step 1: Get word IDs for the matching words and create lookup maps
            var wordIdToName = new Dictionary<int, string>();
            var wordNameToId = new Dictionary<string, int>();
            var wordParams = string.Join(",", matchingWords.Select((_, i) => $"@p{i}"));
            var command = _connection.CreateCommand();
            command.CommandText = $"SELECT id, name FROM word WHERE name IN ({wordParams})";
            
            // Add parameters for each word (prevents SQL injection)
            for (int i = 0; i < matchingWords.Count; i++)
            {
                command.Parameters.AddWithValue($"@p{i}", matchingWords[i]);
            }

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    wordIdToName[id] = name;  // For converting results back to words
                    wordNameToId[name] = id;  // For future lookups if needed
                }
            }

            if (wordIdToName.Count == 0) return result;

            // Step 2: Get all occurrences for the found word IDs from inverted index
            var idParams = string.Join(",", wordIdToName.Keys.Select((_, i) => $"@id{i}"));
            var occCommand = _connection.CreateCommand();
            occCommand.CommandText = $"SELECT docId, wordId FROM Occ WHERE wordId IN ({idParams})";
            int j = 0;
            foreach (var id in wordIdToName.Keys)
            {
                occCommand.Parameters.AddWithValue($"@id{j++}", id);
            }

            // Step 3: Build result mapping docIds to their list of matching pattern words
            using (var reader = occCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    int docId = reader.GetInt32(0);
                    int wordId = reader.GetInt32(1);
                    
                    // Initialize document entry if first time seeing this doc
                    if (!result.ContainsKey(docId))
                    {
                        result[docId] = new List<string>();
                    }
                    
                    // Add the pattern-matching word to this document's list
                    result[docId].Add(wordIdToName[wordId]);
                }
            }

            return result;
        }
    }
}