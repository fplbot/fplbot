using System.Threading.Tasks;
using Fpl.Data;
using Fpl.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VerifiedController : ControllerBase
    {
        private readonly IVerifiedPLEntriesRepository _plRepo;
        private readonly IVerifiedEntriesRepository _repo;

        public VerifiedController(IVerifiedPLEntriesRepository plRepo, IVerifiedEntriesRepository repo)
        {
            _plRepo = plRepo;
            _repo = repo;
        }
        
        [HttpGet("pl/{entryId:int}")]
        public async Task<IActionResult> GetPL(int entryId)
        {
            var plVerifiedEntry = await _plRepo.GetVerifiedPLEntry(entryId);
            if (plVerifiedEntry == null)
                return NotFound();

            var verifiedEntry = await _repo.GetVerifiedEntry(entryId);
            if (verifiedEntry == null)
                return NotFound();
            
            return Ok(ApiModelBuilder.BuildPLEntry(verifiedEntry,plVerifiedEntry));
        }
        
        [HttpGet("{entryId:int}")]
        public async Task<IActionResult> GetRegular(int entryId)
        {
            var verifiedEntry = await _repo.GetVerifiedEntry(entryId);
            if (verifiedEntry == null)
                return NotFound();
            
            return Ok(ApiModelBuilder.BuildRegularEntry(verifiedEntry));
        }
    }
}