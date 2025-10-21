using SearchAPI.Models;
using System.Text.Json;

namespace Coordinator.Services
{
    public class CoordinatorService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string[] _searchAPIInstances;

        public CoordinatorService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;

            // Read SearchAPI URLs from configuration
            _searchAPIInstances = _configuration.GetSection("SearchAPISettings:Instances")
                .Get<string[]>() ?? new[] {
                    "http://localhost:5137",
                    "http://localhost:5138",
                    "http://localhost:5139"
                };
        }

        /// <summary>
        /// Performs a standard search across all data partitions and merges results
        /// </summary>
        public async Task<SearchResult> SearchAsync(string query, bool caseSensitive = false, int? limit = 20, bool includeTimestamps = true)
        {
            var startTime = DateTime.Now;

            // Create parallel tasks for each SearchAPI instance
            var tasks = new List<Task<SearchResult>>();

            foreach (var instanceUrl in _searchAPIInstances)
            {
                var url = $"{instanceUrl}/api/search?query={Uri.EscapeDataString(query)}&caseSensitive={caseSensitive}&limit={limit}&includeTimestamps={includeTimestamps}";
                Console.WriteLine($"[Coordinator] Querying: {url}");
                tasks.Add(FetchSearchResultAsync(url));
            }

            // Wait for all partitions to respond
            var partialResults = await Task.WhenAll(tasks);

            // Merge results from all partitions
            var mergedResult = MergeSearchResults(partialResults, query, startTime);

            return mergedResult;
        }

        /// <summary>
        /// Performs a pattern search across all data partitions and merges results
        /// </summary>
        public async Task<PatternSearchResult> SearchPatternAsync(string pattern, bool caseSensitive = false, int? limit = 20)
        {
            var startTime = DateTime.Now;

            // Create parallel tasks for each SearchAPI instance
            var tasks = new List<Task<PatternSearchResult>>();

            foreach (var instanceUrl in _searchAPIInstances)
            {
                var url = $"{instanceUrl}/api/search/pattern?pattern={Uri.EscapeDataString(pattern)}&caseSensitive={caseSensitive}&limit={limit}";
                Console.WriteLine($"[Coordinator] Querying pattern: {url}");
                tasks.Add(FetchPatternSearchResultAsync(url));
            }

            // Wait for all partitions to respond
            var partialResults = await Task.WhenAll(tasks);

            // Merge results from all partitions
            var mergedResult = MergePatternSearchResults(partialResults, pattern, startTime);

            return mergedResult;
        }

        private async Task<SearchResult> FetchSearchResultAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<SearchResult>(json, options);
                return result ?? throw new Exception("Failed to deserialize search result");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Coordinator] Error fetching from {url}: {ex.Message}");
                // Return empty result on error (graceful degradation)
                return new SearchResult(
                    query: Array.Empty<string>(),
                    totalDocuments: 0,
                    returnedDocuments: new List<DocumentHit>(),
                    ignored: new List<string>(),
                    timeUsed: TimeSpan.Zero,
                    totalHits: 0,
                    returnedHits: 0
                );
            }
        }

        private async Task<PatternSearchResult> FetchPatternSearchResultAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<PatternSearchResult>(json, options);
                return result ?? throw new Exception("Failed to deserialize pattern search result");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Coordinator] Error fetching pattern from {url}: {ex.Message}");
                // Return empty result on error
                return new PatternSearchResult(
                    hits: new List<PatternDocumentHit>(),
                    totalDocuments: 0,
                    returnedDocuments: 0,
                    totalHits: 0,
                    returnedHits: 0
                );
            }
        }

        private SearchResult MergeSearchResults(SearchResult[] partialResults, string query, DateTime startTime)
        {
            // Combine all document hits
            var allDocuments = new List<DocumentHit>();
            var allIgnored = new HashSet<string>();
            int totalDocuments = 0;
            int totalHits = 0;
            int returnedHits = 0;

            foreach (var partial in partialResults)
            {
                allDocuments.AddRange(partial.ReturnedDocuments);
                totalDocuments += partial.TotalDocuments;
                totalHits += partial.TotalHits;
                returnedHits += partial.ReturnedHits;

                // Merge ignored words
                foreach (var ignored in partial.Ignored)
                {
                    allIgnored.Add(ignored);
                }
            }

            // Sort combined results by NoOfHits descending
            var sortedDocuments = allDocuments
                .OrderByDescending(hit => hit.NoOfHits)
                .ToList();

            var timeUsed = DateTime.Now - startTime;

            // Return merged result
            return new SearchResult(
                query: query.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                totalDocuments: totalDocuments,
                returnedDocuments: sortedDocuments,
                ignored: allIgnored.ToList(),
                timeUsed: timeUsed,
                totalHits: totalHits,
                returnedHits: returnedHits
            );
        }

        private PatternSearchResult MergePatternSearchResults(PatternSearchResult[] partialResults, string pattern, DateTime startTime)
        {
            // Combine all pattern document hits
            var allHits = new List<PatternDocumentHit>();
            int totalDocuments = 0;
            int totalHits = 0;
            int returnedHits = 0;

            foreach (var partial in partialResults)
            {
                allHits.AddRange(partial.Hits);
                totalDocuments += partial.TotalDocuments;
                totalHits += partial.TotalHits;
                returnedHits += partial.ReturnedHits;
            }

            // Sort combined results by MatchingWords.Count descending (number of matching words)
            var sortedHits = allHits
                .OrderByDescending(hit => hit.MatchingWords.Count)
                .ToList();

            var timeUsed = DateTime.Now - startTime;

            // Return merged result
            var mergedResult = new PatternSearchResult(
                hits: sortedHits,
                totalDocuments: totalDocuments,
                returnedDocuments: sortedHits.Count,
                totalHits: totalHits,
                returnedHits: returnedHits
            );

            mergedResult.Pattern = pattern;
            mergedResult.TimeUsed = timeUsed;

            return mergedResult;
        }
    }
}
