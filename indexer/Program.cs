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
            int? partitionNumber = null;
            int? totalPartitions = null;

            if (args.Length > 0)
            {
                dataset = args[0].ToLower();

                // Check for partition parameters: dotnet run <dataset> <partition_number> <total_partitions>
                if (args.Length >= 3)
                {
                    if (int.TryParse(args[1], out int pNum) && int.TryParse(args[2], out int pTotal))
                    {
                        partitionNumber = pNum;
                        totalPartitions = pTotal;

                        if (partitionNumber < 1 || partitionNumber > totalPartitions)
                        {
                            Console.WriteLine($"Error: Partition number must be between 1 and {totalPartitions}");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: Partition parameters must be integers");
                        Console.WriteLine("Usage: dotnet run <dataset> [partition_number] [total_partitions]");
                        return;
                    }
                }
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

            if (partitionNumber.HasValue && totalPartitions.HasValue)
            {
                Console.WriteLine($"Starting indexing with '{dataset}' dataset - Partition {partitionNumber}/{totalPartitions}...");
            }
            else
            {
                Console.WriteLine($"Starting indexing with '{dataset}' dataset...");
            }

            new App().Run(dataset, partitionNumber, totalPartitions);

            //new Renamer().Crawl(new DirectoryInfo(@"/Users/ole/data"));
        }

        
        
    }
}