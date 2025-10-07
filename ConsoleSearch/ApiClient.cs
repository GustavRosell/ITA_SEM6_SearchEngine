using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Shared.Model;

namespace ConsoleSearch
{
    // Simple DTOs for API responses - no dependency on SearchAPI models
    public class SearchResult
    {
        public string InstanceId { get; set; }
        public string[] Query { get; set; }
        public int TotalDocuments { get; set; }
        public int ReturnedDocuments { get; set; }
        public bool IsTruncated => ReturnedDocuments < TotalDocuments;
        public int TotalHits { get; set; }
        public int ReturnedHits { get; set; }
        public List<DocumentHit> DocumentHits { get; set; }
        public List<string> Ignored { get; set; }
        public TimeSpan TimeUsed { get; set; }

        public SearchResult(string instanceId, string[] query, int totalDocuments, int returnedDocuments, int totalHits, int returnedHits, List<DocumentHit> documents, List<string> ignored, TimeSpan timeUsed)
        {
            InstanceId = instanceId;
            Query = query;
            TotalDocuments = totalDocuments;
            ReturnedDocuments = returnedDocuments;
            TotalHits = totalHits;
            ReturnedHits = returnedHits;
            DocumentHits = documents;
            Ignored = ignored;
            TimeUsed = timeUsed;
        }
    }

    public class DocumentHit
    {
        public BEDocument Document { get; set; }
        public int NoOfHits { get; set; }
        public List<string> Missing { get; set; }

        public DocumentHit(BEDocument doc, int noOfHits, List<string> missing)
        {
            Document = doc;
            NoOfHits = noOfHits;
            Missing = missing;
        }
    }

    public class PatternSearchResult
    {
        public string InstanceId { get; set; }
        public List<PatternDocumentHit> Hits { get; set; }
        public int TotalDocuments { get; set; }
        public int ReturnedDocuments { get; set; }
        public bool IsTruncated { get; set; }
        public int TotalHits { get; set; }
        public int ReturnedHits { get; set; }
        public double TimeUsedMs { get; set; }

        public PatternSearchResult(string instanceId, List<PatternDocumentHit> hits, int totalDocuments, int returnedDocuments, bool isTruncated, int totalHits, int returnedHits, double timeUsedMs)
        {
            InstanceId = instanceId;
            Hits = hits;
            TotalDocuments = totalDocuments;
            ReturnedDocuments = returnedDocuments;
            IsTruncated = isTruncated;
            TotalHits = totalHits;
            ReturnedHits = returnedHits;
            TimeUsedMs = timeUsedMs;
        }
    }

    public class PatternDocumentHit
    {
        public BEDocument Document { get; set; }
        public List<string> MatchingWords { get; set; }
        
        public PatternDocumentHit(BEDocument document, List<string> matchingWords)
        {
            Document = document;
            MatchingWords = matchingWords;
        }
    }

    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiClient()
        {
            _httpClient = new HttpClient();

            // Read API_BASE_URL from environment variable
            // Default: http://localhost:8080 (load balancer)
            // Override: API_BASE_URL=http://localhost:5137 (single instance)
            var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:8080";
            _baseUrl = $"{apiBaseUrl}/api/search";
        }

        public async Task<SearchResult> SearchAsync(string[] query, bool caseSensitive = true, int? limit = 20, bool includeTimestamps = true)
        {
            try
            {
                var queryString = string.Join(" ", query);
                var url = $"{_baseUrl}?query={Uri.EscapeDataString(queryString)}&caseSensitive={caseSensitive}&limit={limit}&includeTimestamps={includeTimestamps}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                return ConvertToSearchResult(apiResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API call failed: {ex.Message}");
                throw;
            }
        }

        public async Task<PatternSearchResult> PatternSearchAsync(string pattern, bool caseSensitive = true, int? limit = 20)
        {
            try
            {
                var url = $"{_baseUrl}/pattern?pattern={Uri.EscapeDataString(pattern)}&caseSensitive={caseSensitive}&limit={limit}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiPatternSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                return ConvertToPatternSearchResult(apiResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API call failed: {ex.Message}");
                throw;
            }
        }

        private SearchResult ConvertToSearchResult(ApiSearchResponse api)
        {
            var documentHits = new List<DocumentHit>();
            foreach (var hit in api.DocumentHits)
            {
                var document = new Shared.Model.BEDocument
                {
                    mId = hit.Document.Id,
                    mUrl = hit.Document.Url,
                    mIdxTime = hit.Document.IndexTime,
                    mCreationTime = hit.Document.CreationTime
                };
                documentHits.Add(new DocumentHit(document, hit.NoOfHits, hit.Missing));
            }
            return new SearchResult(
                api.InstanceId ?? "Unknown",
                api.Query,
                api.TotalDocuments,
                api.ReturnedDocuments,
                api.TotalHits,
                api.ReturnedHits,
                documentHits,
                api.Ignored,
                TimeSpan.FromMilliseconds(api.TimeUsed));
        }

        private PatternSearchResult ConvertToPatternSearchResult(ApiPatternSearchResponse apiResponse)
        {
            var hits = new List<PatternDocumentHit>();

            foreach (var hit in apiResponse.Hits)
            {
                var document = new Shared.Model.BEDocument
                {
                    mId = hit.Document.Id,
                    mUrl = hit.Document.Url,
                    mIdxTime = hit.Document.IndexTime,
                    mCreationTime = hit.Document.CreationTime
                };

                hits.Add(new PatternDocumentHit(document, hit.MatchingWords));
            }

            return new PatternSearchResult(
                apiResponse.InstanceId ?? "Unknown",
                hits,
                apiResponse.TotalDocuments,
                apiResponse.ReturnedDocuments,
                apiResponse.IsTruncated,
                apiResponse.TotalHits,
                apiResponse.ReturnedHits,
                apiResponse.TimeUsed);
        }
    }

    // API Response models
    public class ApiSearchResponse
    {
        public string InstanceId { get; set; }
        public string[] Query { get; set; }
        public int TotalDocuments { get; set; }
        public int ReturnedDocuments { get; set; }
        public bool IsTruncated { get; set; }
        public int TotalHits { get; set; }
        public int ReturnedHits { get; set; }
        public List<ApiDocumentHit> DocumentHits { get; set; }
        public List<string> Ignored { get; set; }
        public double TimeUsed { get; set; }
    }

    public class ApiDocumentHit
    {
        public ApiDocument Document { get; set; }
        public int NoOfHits { get; set; }
        public List<string> Missing { get; set; }
    }

    public class ApiDocument
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string IndexTime { get; set; }
        public string CreationTime { get; set; }
    }

    public class ApiPatternSearchResponse
    {
        public string InstanceId { get; set; }
        public string Pattern { get; set; }
        public int TotalDocuments { get; set; }
        public int ReturnedDocuments { get; set; }
        public bool IsTruncated { get; set; }
        public List<ApiPatternHit> Hits { get; set; }
        public int TotalHits { get; set; }
        public int ReturnedHits { get; set; }
        public double TimeUsed { get; set; }
    }

    public class ApiPatternHit
    {
        public ApiDocument Document { get; set; }
        public List<string> MatchingWords { get; set; }
    }
}