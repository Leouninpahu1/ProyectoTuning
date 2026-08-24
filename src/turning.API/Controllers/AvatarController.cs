using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turning.Application.Features.Emotions;
namespace Turning.API.Controllers;
[ApiController]
[Authorize]
[Route("api/sessions/{sessionId:guid}/avatar")]
public sealed class AvatarController : ControllerBase
{
    private readonly IAvatarService _svc;
    public AvatarController(IAvatarService svc) => _svc = svc;
    [HttpGet("current")]
    public async Task<IActionResult> Current(Guid sessionId, CancellationToken ct)
    {
        var cur = await _svc.GetCurrentAsync(sessionId, ct);
        return Ok(cur);
    }
}
