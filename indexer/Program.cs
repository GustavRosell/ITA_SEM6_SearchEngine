using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Indexer
{
    class Program
    {
        static void Main(string[] args)
        {
            string dataset;
            
            if (args.Length > 0)
            {
                dataset = args[0].ToLower();
            }
            else
            {
                Console.WriteLine("Select dataset:");
                Console.WriteLine("1. small  - 5 files (quick test)");
                Console.WriteLine("2. medium - ~5,000 emails (functional + performance)");
                Console.WriteLine("3. large  - ~50,000 emails (performance testing)");
                Console.Write("Enter choice (small/medium/large): ");
                
                string input = Console.ReadLine()?.ToLower().Trim();
                dataset = input;
            }

            if (dataset != "small" && dataset != "medium" && dataset != "large")
            {
                Console.WriteLine($"Invalid dataset '{dataset}'. Valid options: small, medium, large");
                return;
            }

            Console.WriteLine($"Starting indexing with '{dataset}' dataset...");
            new App().Run(dataset);

            //new Renamer().Crawl(new DirectoryInfo(@"/Users/ole/data"));
        }

        
        
    }
}