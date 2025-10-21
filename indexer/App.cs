using System;
using System.Collections.Generic;
using System.IO;
using Shared;

namespace Indexer
{
    public class App
    {
        public void Run(string dataset, int? partitionNumber = null, int? totalPartitions = null)
        {
            // Determine database file path based on partitioning
            string databasePath = GetDatabasePath(partitionNumber);

            DatabaseSqlite db = new DatabaseSqlite(databasePath);
            Crawler crawler = new Crawler(db);

            var root = new DirectoryInfo(Config.GetFolder(dataset));

            DateTime start = DateTime.Now;

            // If partitioning, filter which subdirectories to index
            if (partitionNumber.HasValue && totalPartitions.HasValue)
            {
                var allSubdirs = root.GetDirectories();
                var partitionSubdirs = GetPartitionDirectories(allSubdirs, partitionNumber.Value, totalPartitions.Value);

                Console.WriteLine($"This partition will index {partitionSubdirs.Count} out of {allSubdirs.Length} subdirectories:");
                foreach (var dir in partitionSubdirs)
                {
                    Console.WriteLine($"  - {dir.Name}");
                }

                // Index only the assigned subdirectories
                foreach (var subdir in partitionSubdirs)
                {
                    crawler.IndexFilesIn(subdir, new List<string> { ".txt" });
                }
            }
            else
            {
                // No partitioning - index everything
                crawler.IndexFilesIn(root, new List<string> { ".txt" });
            }

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            Console.WriteLine($"Indexed {db.DocumentCounts} documents");
            Console.WriteLine($"Total word occurrences: {db.GetTotalOccurrences()}");

            Console.WriteLine("How many of the most frequent words do you want to see?");
            string input = Console.ReadLine();
            if (int.TryParse(input, out int count) && count > 0)
            {
                var frequentWords = db.GetMostFrequentWords(count);
                Console.WriteLine($"The top {count} most frequent words are:");
                foreach (var p in frequentWords)
                {
                    Console.WriteLine($"<{p.Item1}, {p.Item2}> - {p.Item3}");
                }
            }
        }

        private string GetDatabasePath(int? partitionNumber)
        {
            if (partitionNumber.HasValue)
            {
                // Use partitioned database filename in the same directory as default DB
                string baseDir = Path.GetDirectoryName(Paths.DATABASE) ?? "Data";
                // Ensure directory exists
                Directory.CreateDirectory(baseDir);
                return Path.Combine(baseDir, $"searchDB{partitionNumber}.db");
            }
            else
            {
                // Use default database path
                return Paths.DATABASE;
            }
        }

        private List<DirectoryInfo> GetPartitionDirectories(DirectoryInfo[] allDirs, int partitionNumber, int totalPartitions)
        {
            var result = new List<DirectoryInfo>();

            // Distribute directories evenly using modulo
            for (int i = 0; i < allDirs.Length; i++)
            {
                if (i % totalPartitions == (partitionNumber - 1))
                {
                    result.Add(allDirs[i]);
                }
            }

            return result;
        }
    }
}
