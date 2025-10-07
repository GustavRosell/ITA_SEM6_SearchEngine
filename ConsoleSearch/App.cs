using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleSearch
{
    public class App
    {
        public App()
        {
        }

        public void Run()
        {
            ApiClient apiClient = new ApiClient();
            Console.WriteLine("Console Search (API Mode)");

            while (true)
            {
                DisplayMenu();
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;

                // Quit
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) break;

                // Help (accept both '?' and optional '6')
                if (input == "?" || input == "6")
                {
                    DisplayHelp();
                    continue;
                }

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
                        if (!string.IsNullOrEmpty(limitInput))
                        {
                            if (limitInput.Equals("all", StringComparison.OrdinalIgnoreCase))
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
                        }
                        continue;
                    case "4":
                        Config.PatternSearch = !Config.PatternSearch;
                        continue;
                    case "5":
                        Config.CompactView = !Config.CompactView;
                        continue;
                }

                if (Config.PatternSearch)
                {
                    var patternResult = apiClient.PatternSearchAsync(input, Config.CaseSensitive, Config.ResultLimit).Result;
                    Console.WriteLine($"[Instance: {patternResult.InstanceId}]");
                    Console.WriteLine("Pattern Search Results:");
                    int patternIdx = 1;
                    foreach (var hit in patternResult.Hits)
                    {
                        if (Config.CompactView)
                        {
                            string fileName = System.IO.Path.GetFileName(hit.Document.mUrl);
                            string matchText = hit.MatchingWords.Count == 1 ? "match" : "matches";
                            Console.WriteLine($"{patternIdx}. {fileName} ({hit.MatchingWords.Count} {matchText}): {string.Join(", ", hit.MatchingWords)}");
                        }
                        else
                        {
                            Console.WriteLine($"{patternIdx}: {hit.Document.mUrl} -- contains {hit.MatchingWords.Count} matching terms:");
                            Console.WriteLine($"    {string.Join(", ", hit.MatchingWords)}");
                        }
                        patternIdx++;
                    }
                    if (patternResult.TotalDocuments == 0)
                    {
                        Console.WriteLine("No documents matched the pattern.");
                    }
                    else if (patternResult.IsTruncated)
                    {
                        Console.WriteLine($"Found {patternResult.TotalDocuments} documents and {patternResult.TotalHits} hits (showing {patternResult.ReturnedDocuments} documents / {patternResult.ReturnedHits} hits due to limit) in {patternResult.TimeUsedMs:F2} ms.");
                    }
                    else
                    {
                        Console.WriteLine($"Found {patternResult.ReturnedDocuments} documents and {patternResult.ReturnedHits} hits in {patternResult.TimeUsedMs:F2} ms.");
                    }
                }
                else if (input.StartsWith("/"))
                {
                    HandleCommand(input);
                }
                else
                {
                    var query = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    var searchResult = apiClient.SearchAsync(query, Config.CaseSensitive, Config.ResultLimit, Config.ViewTimeStamps).Result;

                    Console.WriteLine($"[Instance: {searchResult.InstanceId}]");

                    if (searchResult.Ignored.Count > 0)
                    {
                        Console.WriteLine($"Ignored: {string.Join(',', searchResult.Ignored)}");
                    }

                    int searchIdx = 1;
                    foreach (var doc in searchResult.DocumentHits)
                    {
                        if (Config.CompactView)
                        {
                            string fileName = System.IO.Path.GetFileName(doc.Document.mUrl);
                            string hitText = doc.NoOfHits == 1 ? "match" : "matches";
                            // Get the matching terms (all query terms minus missing ones)
                            var allTerms = query.ToList();
                            var matchingTerms = allTerms.Except(doc.Missing).ToArray();
                            Console.WriteLine($"{searchIdx}. {fileName} ({doc.NoOfHits} {hitText}): {string.Join(", ", matchingTerms)}");
                        }
                        else
                        {
                            Console.WriteLine($"{searchIdx} : {doc.Document.mUrl} -- contains {doc.NoOfHits} search terms");
                            if (Config.ViewTimeStamps)
                            {
                                Console.WriteLine("    Index time: " + doc.Document.mIdxTime);
                            }
                            Console.WriteLine($"    Missing: {ArrayAsString(doc.Missing.ToArray())}");
                        }
                        searchIdx++;
                    }
                    if (searchResult.IsTruncated)
                    {
                        Console.WriteLine($"Found {searchResult.TotalDocuments} documents and {searchResult.TotalHits} hits (showing {searchResult.ReturnedDocuments} documents / {searchResult.ReturnedHits} hits due to limit). Time: {searchResult.TimeUsed.TotalMilliseconds} ms");
                    }
                    else
                    {
                        Console.WriteLine($"Found {searchResult.ReturnedDocuments} documents and {searchResult.ReturnedHits} hits. Time: {searchResult.TimeUsed.TotalMilliseconds} ms");
                    }
                }
            }
        }

        private void DisplayMenu()
        {
            Console.WriteLine("\n----------------------------------------------------------------");
            string caseStatus = Config.CaseSensitive ? "ON" : "OFF";
            string timeStatus = Config.ViewTimeStamps ? "ON" : "OFF";
            string limitStatus = Config.ResultLimit.HasValue ? Config.ResultLimit.Value.ToString() : "ALL";
            string patternStatus = Config.PatternSearch ? "ON" : "OFF";
            string compactStatus = Config.CompactView ? "ON" : "OFF";

            Console.WriteLine($"OPTIONS: [1] Case Sensitive: {caseStatus}   [2] Timestamps: {timeStatus}   [3] Result Limit: {limitStatus}   [4] Pattern Search: {patternStatus}   [5] Compact View: {compactStatus}");
            Console.WriteLine("Enter a search query, a number to toggle/change, '?' for help, or 'q' to quit.");
            Console.Write("> ");
        }

        private void DisplayHelp()
        {
            Console.WriteLine();
            Console.WriteLine("=== SEARCH ENGINE HELP ===");
            Console.WriteLine();
            Console.WriteLine("CURRENT SETTINGS:");
            // Case Sensitive
            Console.WriteLine($"Case Sensitive: {(Config.CaseSensitive ? "ON" : "OFF")} - When ON, letter casing must match exactly.");
            Console.WriteLine($"  Example: Query 'Hello' {(Config.CaseSensitive ? "will NOT match 'hello'" : "will match 'hello'")} in documents.");
            Console.WriteLine("  Toggle via: [1] or /casesensitive=on|off");
            Console.WriteLine();
            // Timestamps
            Console.WriteLine($"Timestamps: {(Config.ViewTimeStamps ? "ON" : "OFF")} - Shows document indexing time under each result.");
            Console.WriteLine("  Example line: 'Index time: 2025-01-15 14:30:22'");
            Console.WriteLine("  Toggle via: [2] or /timestamp=on|off");
            Console.WriteLine();
            // Result Limit
            string limitStatus = Config.ResultLimit.HasValue ? Config.ResultLimit.Value.ToString() : "ALL";
            Console.WriteLine($"Result Limit: {limitStatus} - Maximum number of results displayed (ALL = no limit).");
            Console.WriteLine("  Example: If set to 20, only top 20 matches are shown.");
            Console.WriteLine("  Change via: [3] or /results=NUMBER|all");
            Console.WriteLine();
            // Pattern Search
            Console.WriteLine($"Pattern Search: {(Config.PatternSearch ? "ON" : "OFF")} - Enables wildcard matching using ? and *.");
            Console.WriteLine("  ? matches exactly one character. * matches zero or more characters.");
            Console.WriteLine("  Example pattern: en*gy?  -> matches 'energy1', 'enggy2', 'enxxxgyA'");
            Console.WriteLine("  Toggle via: [4]" + " or /pattern=on|off");
            Console.WriteLine();
            // Compact View
            Console.WriteLine($"Compact View: {(Config.CompactView ? "ON" : "OFF")} - Shows clean, simplified search results.");
            Console.WriteLine("  Removes long file paths and displays results on single lines.");
            Console.WriteLine("  This will also hide timestamps regardless of that setting.");
            Console.WriteLine("  Example: '1. file.txt (3 matches): energy, power, grid' instead of full paths");
            Console.WriteLine("  Toggle via: [5] or /compact=on|off");
            Console.WriteLine();
            // Other commands
            Console.WriteLine("OTHER:");
            Console.WriteLine("- '?' shows this help.");
            Console.WriteLine("- 'q' quits the application.");
            Console.WriteLine();
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey(true);
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
                else if (int.TryParse(value, out int limit) && limit > 0) Config.ResultLimit = limit;
                string limitStatus = Config.ResultLimit.HasValue ? Config.ResultLimit.Value.ToString() : "ALL";
                Console.WriteLine($"Result limit set to {limitStatus}");
            }
            else if (command == "/pattern")
            {
                if (value == "on" || value == "true") Config.PatternSearch = true;
                else if (value == "off" || value == "false") Config.PatternSearch = false;
                Console.WriteLine($"Pattern search set to {(Config.PatternSearch ? "ON" : "OFF")}");
            }
            else if (command == "/compact")
            {
                if (value == "on" || value == "true") Config.CompactView = true;
                else if (value == "off" || value == "false") Config.CompactView = false;
                Console.WriteLine($"Compact view set to {(Config.CompactView ? "ON" : "OFF")}");
            }
            else
            {
                Console.WriteLine($"Unknown command: {command}");
            }
        }

        string ArrayAsString(string[] s)
        {
            return s.Length == 0 ? "[]" : $"[{String.Join(',', s)}]";
        }
    }
}