using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Model;
using Microsoft.Data.Sqlite;

namespace SearchAPI
{
    public class DatabaseSqlite : IDatabase
    {
        private SqliteConnection _connection;

        public DatabaseSqlite()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder();

            connectionStringBuilder.DataSource = Paths.DATABASE;


            _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);

            _connection.Open();


        }

        private void Execute(string sql)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }




        // key is the id of the document, the value is number of search words in the document
        public List<KeyValuePair<int, int>> GetDocuments(List<int> wordIds)
        {
            var res = new List<KeyValuePair<int, int>>();

            /* Example sql statement looking for doc id's that
               contain words with id 2 and 3
            
               SELECT docId, COUNT(wordId) as count
                 FROM Occ
                WHERE wordId in (2,3)
             GROUP BY docId
             ORDER BY COUNT(wordId) DESC 
             */

            var sql = "SELECT docId, COUNT(wordId) as count FROM Occ where ";
            sql += "wordId in " + AsString(wordIds) + " GROUP BY docId ";
            sql += "ORDER BY count DESC;";

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

        private string AsString(List<int> x) => $"({string.Join(',', x)})";


       

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

        /* Return a list of id's for words; all them among wordIds, but not present in the document
         */
        public List<int> getMissing(int docId, List<int> wordIds)
        {
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
            var result = new List<int>(wordIds);
            foreach (var w in present)
                result.Remove(w);


            return result;
        }

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
                    var wordId = reader.GetString(0);
                    result.Add(wordId);
                }
            }
            return result;
        }

        public List<int> GetWordIds(string[] query, out List<string> outIgnored, bool caseSensitive = false)
        {
            var res = new List<int>();
            var ignored = new List<string>();

            foreach (var aWord in query)
            {
                var command = _connection.CreateCommand();
                if (caseSensitive)
                {
                    command.CommandText = "SELECT id FROM word WHERE name = @name";
                }
                else
                {
                    command.CommandText = "SELECT id FROM word WHERE LOWER(name) = LOWER(@name)";
                }
                command.Parameters.AddWithValue("@name", aWord);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        res.Add(reader.GetInt32(0));
                    }
                    else
                    {
                        ignored.Add(aWord);
                    }
                }
            }
            outIgnored = ignored;
            return res;
        }

        public List<string> GetWordsMatchingPattern(string pattern, bool caseSensitive = false)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(pattern))
                return result;

            var command = _connection.CreateCommand();

            if (caseSensitive)
            {
                // Case sensitive: use GLOB directly (supports * and ?)
                command.CommandText = "SELECT name FROM word WHERE name GLOB @pattern";
                command.Parameters.AddWithValue("@pattern", pattern);
                Console.WriteLine($"DB DEBUG: GLOB (case sensitive) pattern='{pattern}'");
            }
            else
            {
                // Case insensitive: LIKE with % and _ (convert our * and ?)
                var sqlPattern = pattern.Replace('?', '_').Replace('*', '%');
                command.CommandText = "SELECT name FROM word WHERE name LIKE @pattern";
                command.Parameters.AddWithValue("@pattern", sqlPattern);
                Console.WriteLine($"DB DEBUG: LIKE (case insensitive) orig='{pattern}' conv='{sqlPattern}'");
            }

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(reader.GetString(0));
                }
            }

            Console.WriteLine($"DB DEBUG: Found {result.Count} words (pattern). Examples: [{string.Join(", ", result.Take(5))}]");
            return result;
        }

        public Dictionary<int, List<string>> GetDocsWithMatchingWords(List<string> matchingWords)
        {
            var result = new Dictionary<int, List<string>>();
            if (matchingWords == null || matchingWords.Count == 0)
            {
                Console.WriteLine("DB DEBUG: No matching words provided to GetDocsWithMatchingWords");
                return result;
            }
            
            Console.WriteLine($"DB DEBUG: GetDocsWithMatchingWords called with {matchingWords.Count} words: [{string.Join(", ", matchingWords)}]");

            // Step 1: Get word IDs and create lookup maps
            var wordIdToName = new Dictionary<int, string>();
            var wordNameToId = new Dictionary<string, int>();
            var wordParams = string.Join(",", matchingWords.Select((_, i) => $"@p{i}"));
            var command = _connection.CreateCommand();
            command.CommandText = $"SELECT id, name FROM word WHERE name IN ({wordParams})";
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
                    wordIdToName[id] = name;
                    wordNameToId[name] = id;
                }
            }

            if (wordIdToName.Count == 0) return result;

            // Step 2: Get all occurrences for the found word IDs
            var idParams = string.Join(",", wordIdToName.Keys.Select((_, i) => $"@id{i}"));
            var occCommand = _connection.CreateCommand();
            occCommand.CommandText = $"SELECT docId, wordId FROM Occ WHERE wordId IN ({idParams})";
            int j = 0;
            foreach (var id in wordIdToName.Keys)
            {
                occCommand.Parameters.AddWithValue($"@id{j++}", id);
            }

            // Step 3: Map docIds to the list of matching words they contain
            using (var reader = occCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    int docId = reader.GetInt32(0);
                    int wordId = reader.GetInt32(1);
                    if (!result.ContainsKey(docId))
                    {
                        result[docId] = new List<string>();
                    }
                    result[docId].Add(wordIdToName[wordId]);
                }
            }

            Console.WriteLine($"DB DEBUG: GetDocsWithMatchingWords returning {result.Count} documents");
            return result;
        }
    }
}