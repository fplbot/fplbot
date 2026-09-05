using System.ComponentModel.DataAnnotations;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using FplBot.WebApi.Handlers.Sagas;

namespace FplBot.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SuggestionsController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public SuggestionsController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("verified")]
    public async Task<IActionResult> PostSuggestion(Suggestion suggestion)
    {
        if (suggestion.EntryId == 0)
            return BadRequest();

        if (suggestion.PlayerId.HasValue && suggestion.PlayerId.Value > 0)
        {
            await _publishEndpoint.Publish(new VerifiedPLEntrySuggestionReceived(suggestion.EntryId, suggestion.ShortDesc(), suggestion.PlayerId.Value));
        }
        else
        {
            await _publishEndpoint.Publish(new VerifiedEntrySuggestionReceived(suggestion.EntryId, suggestion.ShortDesc()));
        }

        return Ok();
    }
}

public record Suggestion([Required, Range(1, int.MaxValue)]int EntryId, [MaxLength(1000)]string Description, int? PlayerId)
{
    public string ShortDesc()
    {
        return Description.Length > 1000 ? Description.Substring(0, 1000) : Description;
    }
}
