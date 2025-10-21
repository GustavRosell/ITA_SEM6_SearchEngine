using Microsoft.AspNetCore.Mvc;
using Coordinator.Services;

namespace Coordinator.Controllers
{
    /// <summary>
    /// CoordinatorController - Z-Scale data partitioning aggregation layer
    /// Part of IT-Arkitektur 6. semester Module 7 implementation
    ///
    /// This controller implements the Coordinator pattern for distributed search across
    /// multiple data partitions. It sends parallel requests to all SearchAPI instances,
    /// merges the results, and returns unified search results to clients.
    ///
    /// Architecture:
    ///   Client → Coordinator → SearchAPI-1 (DB partition 1)
    ///                       → SearchAPI-2 (DB partition 2)
    ///                       → SearchAPI-3 (DB partition 3)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CoordinatorController : ControllerBase
    {
        private readonly CoordinatorService _coordinatorService;

        public CoordinatorController(CoordinatorService coordinatorService)
        {
            _coordinatorService = coordinatorService;
        }

        /// <summary>
        /// Standard search endpoint - queries all data partitions and merges results
        /// </summary>
        /// <param name="query">Search terms separated by spaces (required)</param>
        /// <param name="caseSensitive">Enable case-sensitive matching (default: false)</param>
        /// <param name="limit">Maximum results to return (default: 20, null for unlimited)</param>
        /// <param name="includeTimestamps">Include indexing and creation timestamps (default: true)</param>
        /// <returns>Merged search results from all partitions, sorted by relevance</returns>
        [HttpGet]
        public async Task<IActionResult> Search(
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
                var result = await _coordinatorService.SearchAsync(query, caseSensitive, limit, includeTimestamps);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error coordinating search: {ex.Message}");
            }
        }

        /// <summary>
        /// Pattern search endpoint - queries all data partitions with wildcard support
        /// Supports ? (single char) and * (multiple chars) wildcards
        /// </summary>
        /// <param name="pattern">Search pattern with optional wildcards</param>
        /// <param name="caseSensitive">Enable case-sensitive matching (default: false)</param>
        /// <param name="limit">Maximum results to return (default: 20, null for unlimited)</param>
        /// <returns>Merged pattern search results from all partitions</returns>
        [HttpGet("pattern")]
        public async Task<IActionResult> SearchPattern(
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
                var result = await _coordinatorService.SearchPatternAsync(pattern, caseSensitive, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error coordinating pattern search: {ex.Message}");
            }
        }

        /// <summary>
        /// Health check endpoint - verifies Coordinator is running
        /// </summary>
        /// <returns>Simple text response identifying the service</returns>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Coordinator");
        }
    }
}
