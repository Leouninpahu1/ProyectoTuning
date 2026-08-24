using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turning.Application.Features.ExperimentSessions;
using AppEx = Turning.Application.Exceptions.ApplicationException;

namespace Turning.API.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly IExperimentSessionService _svc;
    public SessionsController(IExperimentSessionService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExperimentSessionRequest? req, CancellationToken ct)
    {
        var uid = GetUserId(); if (uid == Guid.Empty) return Unauthorized();
        try { var r = await _svc.CreateBootstrapSessionAsync(uid, req, ct); return CreatedAtAction(nameof(GetById), new { id = r.Id }, r); }
        catch (AppEx ex) { return StatusCode(Map(ex.Code ?? "SESSION_ERROR"), new { error = ex.Code, message = ex.Message }); }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        try { var r = await _svc.ActivateAsync(id, ct); return Ok(r); }
        catch (AppEx ex) { return StatusCode(Map(ex.Code ?? "SESSION_ERROR"), new { error = ex.Code, message = ex.Message }); }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        try { var r = await _svc.CompleteAsync(id, ct); return Ok(r); }
        catch (AppEx ex) { return StatusCode(Map(ex.Code ?? "SESSION_ERROR"), new { error = ex.Code, message = ex.Message }); }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest req, CancellationToken ct)
    {
        var uid = GetUserId();
        try { var r = await _svc.CancelAsync(id, req.Reason, uid, ct); return Ok(r); }
        catch (AppEx ex) { return StatusCode(Map(ex.Code ?? "SESSION_ERROR"), new { error = ex.Code, message = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try { var r = await _svc.GetByIdAsync(id, ct); return Ok(r); }
        catch (AppEx ex) { return NotFound(new { error = ex.Code }); }
    }

    [HttpGet("participant/{participantId:guid}")]
    public async Task<IActionResult> ListByParticipant(Guid participantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        try { var r = await _svc.ListByParticipantAsync(participantId, page, pageSize, ct); return Ok(new { participantId, sessions = r.Items, r.Total, r.Page, r.PageSize }); }
        catch (AppEx ex) { return BadRequest(new { error = ex.Code }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(claim, out var g) ? g : Guid.Empty;
    }
    private static int Map(string code) => code switch { "SESSION_NOT_FOUND" => 404, "SESSION_CONFLICT" => 409, "SESSION_INVALID_REASON" or "SESSION_INVALID_PAGE" => 400, _ => 400 };
}
public sealed class CancelRequest { public string Reason { get; set; } = string.Empty; }
