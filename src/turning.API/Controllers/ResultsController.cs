using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turning.Application.Features.Events;
namespace Turning.API.Controllers;
[ApiController]
[Authorize]
[Route("api")]
public sealed class ResultsController : ControllerBase
{
    private readonly IResultsService _svc;
    public ResultsController(IResultsService svc) => _svc = svc;
    [HttpGet("sessions/{sessionId:guid}/results")]
    public async Task<IActionResult> GetResult(Guid sessionId, CancellationToken ct)
    {
        try { var r = await _svc.GetResultAsync(sessionId, ct); return Ok(r); }
        catch (InvalidOperationException) { return NotFound(); }
    }
    [HttpGet("results")]
    [Authorize(Roles="Researcher,Administrator")]
    public async Task<IActionResult> List([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? condition, [FromQuery] int page=1, [FromQuery] int pageSize=50, CancellationToken ct=default)
    {
        var r = await _svc.ListAsync(from,to,condition,page,pageSize,ct);
        return Ok(r);
    }
}
