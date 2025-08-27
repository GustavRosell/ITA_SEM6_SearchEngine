using System;
using System.Collections.Generic;
using System.IO;
using Shared;

namespace Indexer
{
    public class App
    {
        public void Run(string dataset){
            DatabaseSqlite db = new DatabaseSqlite(Paths.DATABASE);
            Crawler crawler = new Crawler(db);

            var root = new DirectoryInfo(Config.GetFolder(dataset));

            DateTime start = DateTime.Now;

            crawler.IndexFilesIn(root, new List<string> { ".txt"});        

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
    }
}
