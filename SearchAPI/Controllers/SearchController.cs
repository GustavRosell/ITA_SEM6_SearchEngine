using Microsoft.AspNetCore.Mvc;
using SearchAPI.Data;
using SearchAPI.Services;

namespace SearchAPI.Controllers
{
    /// <summary>
    /// SearchController - RESTful API controller for the SearchEngine PoC
    /// Part of IT-Arkitektur 6. semester Y-Scale architecture pattern implementation
    /// 
    /// Provides HTTP endpoints for document søgning (search) operations:
    /// - Standard search with query terms
    /// - Pattern search with wildcards (* and ?)
    /// 
    /// This controller acts as the API layer in our microservices architecture,
    /// separating search logic from the web UI (SearchWebApp) following Y-Scale principles.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly SearchLogic _searchLogic;
        private readonly string _instanceId;

        /// <summary>
        /// Initialize SearchController with database dependency
        /// Uses SQLite database with inverted index for fast document retrieval
        /// Reads INSTANCE_ID from environment variable for X-Scale load balancing identification
        /// </summary>
        public SearchController()
        {
            _searchLogic = new SearchLogic(new DatabaseSqlite());
            _instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "API-Default";
        }

        /// <summary>
        /// Standard search endpoint - finds documents containing query terms
        /// 
        /// Implements TF (Term Frequency) scoring: score = matching_terms / total_query_terms
        /// Uses inverted index for efficient lookup of documents containing specific words
        /// Results ordered by descending relevance score, then by document ID
        /// 
        /// Perfect for general document søgning with multiple search terms
        /// </summary>
        /// <param name="query">Search terms separated by spaces (required)</param>
        /// <param name="caseSensitive">Enable case-sensitive matching (default: false)</param>
        /// <param name="limit">Maximum results to return (default: 20, null for unlimited)</param>
        /// <param name="includeTimestamps">Include indexing and creation timestamps (default: true)</param>
        /// <returns>
        /// JSON response containing:
        /// - query: Original search terms
        /// - totalDocuments: Total matching documents (before limit)
        /// - returnedDocuments: Number of documents returned (after limit)
        /// - isTruncated: True if results were limited
        /// - totalHits/returnedHits: Word occurrence counts
        /// - documentHits: Array of matching documents with metadata
        /// - ignored: Query terms not found in database
        /// - timeUsed: Search execution time in milliseconds
        /// </returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Query parameter missing or invalid</response>
        /// <response code="500">Internal server error during search</response>
        [HttpGet]
        public IActionResult Search(
            [FromQuery] string query, 
            [FromQuery] bool caseSensitive = false, 
            [FromQuery] int? limit = 20, 
            [FromQuery] bool includeTimestamps = true)
        {
            // Validate required query parameter
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Query parameter is required");
            }

            try
            {
                // Split query into individual search terms (removing empty entries)
                var queryArray = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                // Perform search using inverted index and TF scoring
                var result = _searchLogic.Search(queryArray, limit, caseSensitive);
                
                // Return standardized JSON response format with instance ID for load balancing visibility
                return Ok(new
                {
                    instanceId = _instanceId,
                    query = result.Query,
                    totalDocuments = result.TotalDocuments,
                    returnedDocuments = result.ReturnedDocuments.Count,
                    isTruncated = result.IsTruncated,
                    totalHits = result.TotalHits,
                    returnedHits = result.ReturnedHits,
                    documentHits = result.ReturnedDocuments.Select(hit => new
                    {
                        document = new
                        {
                            id = hit.Document.mId,
                            url = hit.Document.mUrl,
                            indexTime = includeTimestamps ? hit.Document.mIdxTime : null,
                            creationTime = includeTimestamps ? hit.Document.mCreationTime : null
                        },
                        noOfHits = hit.NoOfHits,
                        missing = hit.Missing
                    }),
                    ignored = result.Ignored,
                    timeUsed = result.TimeUsed.TotalMilliseconds
                });
            }
            catch (Exception ex)
            {
                // Return proper HTTP 500 error with message
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Pattern search endpoint - finds documents matching wildcard patterns
        /// 
        /// Supports wildcards: ? (single character) and * (multiple characters)
        /// Examples: "te*" finds "test", "testing" | "t?st" finds "test", "tast"
        /// 
        /// For literal patterns (no wildcards), falls back to standard search for consistency
        /// Results ordered by: most matching words first, then by filename number (15, 101, 126...)
        /// This ensures logical ordering instead of chaotic database ID ordering
        /// 
        /// Perfect for fuzzy søgning when you're not sure of exact spelling
        /// </summary>
        /// <param name="pattern">Wildcard pattern to match (* and ? supported, required)</param>
        /// <param name="caseSensitive">Enable case-sensitive pattern matching (default: false)</param>
        /// <param name="limit">Maximum results to return (default: 20, null for unlimited)</param>
        /// <returns>
        /// JSON response containing:
        /// - pattern: Original search pattern
        /// - totalDocuments: Total matching documents (before limit)
        /// - returnedDocuments: Number of documents returned (after limit)
        /// - isTruncated: True if results were limited
        /// - totalHits/returnedHits: Distinct word match counts
        /// - timeUsed: Search execution time in milliseconds
        /// - hits: Array of documents with matching words listed
        /// </returns>
        /// <response code="200">Pattern search completed successfully</response>
        /// <response code="400">Pattern parameter missing or invalid</response>
        /// <response code="500">Internal server error during pattern search</response>
        [HttpGet("pattern")]
        public IActionResult PatternSearch(
            [FromQuery] string pattern, 
            [FromQuery] bool caseSensitive = false, 
            [FromQuery] int? limit = 20)
        {
            // Validate required pattern parameter
            if (string.IsNullOrEmpty(pattern))
            {
                return BadRequest("Pattern parameter is required");
            }

            try
            {
                // Perform pattern search with wildcard support
                var result = _searchLogic.PatternSearch(pattern, limit, caseSensitive);
                
                // Return standardized JSON response format with instance ID for load balancing visibility
                return Ok(new
                {
                    instanceId = _instanceId,
                    pattern,
                    totalDocuments = result.TotalDocuments,
                    returnedDocuments = result.ReturnedDocuments,
                    isTruncated = result.IsTruncated,
                    totalHits = result.TotalHits,
                    returnedHits = result.ReturnedHits,
                    timeUsed = result.TimeUsed.TotalMilliseconds,
                    hits = result.Hits.Select(hit => new
                    {
                        document = new
                        {
                            id = hit.Document.mId,
                            url = hit.Document.mUrl,
                            indexTime = hit.Document.mIdxTime,
                            creationTime = hit.Document.mCreationTime
                        },
                        matchingWords = hit.MatchingWords
                    })
                });
            }
            catch (Exception ex)
            {
                // Return proper HTTP 500 error with message
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Health check endpoint for load balancer monitoring
        ///
        /// Returns the instance ID and status to verify which backend is responding.
        /// Useful for monitoring load balancer distribution and instance health.
        /// </summary>
        /// <returns>
        /// JSON response with instance ID and status
        /// </returns>
        /// <response code="200">Instance is healthy and ready to serve requests</response>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                instanceId = _instanceId,
                status = "healthy",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            });
        }
    }
}