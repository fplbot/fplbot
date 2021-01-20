using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Search.Searching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchClient _searchClient;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchClient searchClient, ILogger<SearchController> logger)
        {
            _searchClient = searchClient;
            _logger = logger;
        }
        
        [HttpGet("entries/{query}")]
        public async Task<IActionResult> GetEntries(string query)
        {
            try
            {
                var searchResult = await _searchClient.SearchForEntry(query, 10);
                return Ok(new
                {
                    Hits = searchResult,
                });
            }
            catch (HttpRequestException e)
            {
                _logger.LogWarning(e.ToString());
            }
            return NotFound();
        }
    }
}