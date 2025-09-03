using Microsoft.AspNetCore.Mvc;

namespace SearchAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly SearchLogic _searchLogic;

        public SearchController()
        {
            _searchLogic = new SearchLogic(new DatabaseSqlite());
        }

        [HttpGet]
        public IActionResult Search(
            [FromQuery] string query, 
            [FromQuery] bool caseSensitive = false, 
            [FromQuery] int? limit = 20, 
            [FromQuery] bool includeTimestamps = true)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Query parameter is required");
            }

            try
            {
                var queryArray = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var result = _searchLogic.Search(queryArray, limit, caseSensitive);
                
                return Ok(new
                {
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("pattern")]
        public IActionResult PatternSearch(
            [FromQuery] string pattern, 
            [FromQuery] bool caseSensitive = false, 
            [FromQuery] int? limit = 20)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return BadRequest("Pattern parameter is required");
            }

            try
            {
                Console.WriteLine($"API DEBUG PATTERN: pattern='{pattern}', caseSensitive={caseSensitive}, limit={limit}");
                var result = _searchLogic.PatternSearch(pattern, limit, caseSensitive);
                
                Console.WriteLine($"API DEBUG RESPONSE: Returning {result.Hits.Count} hits for pattern search");
                
                return Ok(new
                {
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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}