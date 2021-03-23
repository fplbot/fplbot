using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        [HttpGet("entries/{id:int}")]
        public async Task<IActionResult> GetEntry(int id)
        {
            var entry = await _searchService.GetEntry(id);

            if (entry == null)
            {
                return NotFound();
            }

            return Ok(entry);
        }

        [HttpGet("entries")]
        public async Task<IActionResult> GetEntries(string query, int page)
        {
            var metaData = new SearchMetaData
            {
                Client = QueryClient.Web, Actor = Request?.HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var searchResult = await _searchService.SearchForEntry(query, page, 10, metaData);

            if (searchResult.TotalPages < page && !searchResult.Any())
            {
                ModelState.AddModelError(nameof(page), $"{nameof(page)} exceeds the total page count");
                return BadRequest(ModelState);
            }

            return Ok(new
            {
                Hits = searchResult,
            });
        }

        [HttpGet("leagues")]
        public async Task<IActionResult> GetLeagues(string query, int page)
        {
            var metaData = new SearchMetaData
            {
                Client = QueryClient.Web, Actor = Request?.HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var searchResult = await _searchService.SearchForLeague(query, page, 10, metaData);

            if (searchResult.TotalPages < page && !searchResult.Any())
            {
                ModelState.AddModelError(nameof(page), $"{nameof(page)} exceeds the total page count");
                return BadRequest(ModelState);
            }

            return Ok(new
            {
                Hits = searchResult,
            });
        }
    }
}
