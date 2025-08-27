using System;
using System.Collections.Generic;

namespace ConsoleSearch
{
    public class App
    {
        public App()
        {
        }

        public void Run()
        {
            SearchLogic mSearchLogic = new SearchLogic(new DatabaseSqlite());
            Console.WriteLine("Console Search");

            while (true)
            {
                DisplayMenu();
                string input = Console.ReadLine();
                if (input.Equals("q")) break;

                switch (input)
                {
                    case "1":
                        Config.CaseSensitive = !Config.CaseSensitive;
                        continue;
                    case "2":
                        Config.ViewTimeStamps = !Config.ViewTimeStamps;
                        continue;
                    case "3":
                        Console.Write("Enter new result limit (e.g., 15, or 'all'): ");
                        string limitInput = Console.ReadLine();
                        if (limitInput.ToLower() == "all")
                        {
                            Config.ResultLimit = null;
                        }
                        else if (int.TryParse(limitInput, out int limit) && limit > 0)
                        {
                            Config.ResultLimit = limit;
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Limit unchanged.");
                        }
                        continue;
                }

                if (input.StartsWith("/"))
                {
                    HandleCommand(input);
                    continue;
                }

                var query = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var result = mSearchLogic.Search(query);

                if (result.Ignored.Count > 0)
                {
                    Console.WriteLine($"Ignored: {string.Join(',', result.Ignored)}");
                }

                int idx = 1;
                foreach (var doc in result.DocumentHits)
                {
                    Console.WriteLine($"{idx} : {doc.Document.mUrl} -- contains {doc.NoOfHits} search terms");
                    if (Config.ViewTimeStamps)
                    {
                        Console.WriteLine("Index time: " + doc.Document.mIdxTime);
                    }
                    Console.WriteLine($"Missing: {ArrayAsString(doc.Missing.ToArray())}");
                    idx++;
                }
                Console.WriteLine("Documents: " + result.Hits + ". Time: " + result.TimeUsed.TotalMilliseconds);
            }
        }

        private void DisplayMenu()
        {
            Console.WriteLine("\n----------------------------------------------------------------");
            string caseStatus = Config.CaseSensitive ? "ON" : "OFF";
            string timeStatus = Config.ViewTimeStamps ? "ON" : "OFF";
            string limitStatus = Config.ResultLimit.HasValue ? Config.ResultLimit.Value.ToString() : "ALL";

            Console.WriteLine($"OPTIONS: [1] Case Sensitive: {caseStatus}   [2] Timestamps: {timeStatus}   [3] Result Limit: {limitStatus}");
            Console.WriteLine("Enter a search query, a number to change an option, or 'q' to quit.");
            Console.Write("> ");
        }

        private void HandleCommand(string input)
        {
            var parts = input.Split('=');
            var command = parts[0].ToLower();
            var value = parts.Length > 1 ? parts[1].ToLower() : "";

            if (command == "/casesensitive")
            {
                if (value == "on" || value == "true") Config.CaseSensitive = true;
                else if (value == "off" || value == "false") Config.CaseSensitive = false;
                Console.WriteLine($"Case sensitivity set to {Config.CaseSensitive}");
            }
            else if (command == "/timestamp")
            {
                if (value == "on" || value == "true") Config.ViewTimeStamps = true;
                else if (value == "off" || value == "false") Config.ViewTimeStamps = false;
                Console.WriteLine($"Timestamp display set to {Config.ViewTimeStamps}");
            }
            else if (command == "/results")
            {
                if (value == "all") Config.ResultLimit = null;
                else if (int.TryParse(value, out int limit)) Config.ResultLimit = limit;
                string limitStatus = Config.ResultLimit.HasValue ? Config.ResultLimit.Value.ToString() : "ALL";
                Console.WriteLine($"Result limit set to {limitStatus}");
            }
            else
            {
                Console.WriteLine($"Unknown command: {command}");
            }
        }

        string ArrayAsString(string[] s) {
            return s.Length == 0?"[]":$"[{String.Join(',', s)}]";
            //foreach (var str in s)
            //    res += str + ", ";
            //return res.Substring(0, res.Length - 2) + "]";
        }
    }
}
