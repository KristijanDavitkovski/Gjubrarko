using Gjubrarko.Api.Auth;
using Gjubrarko.Application.Reports;
using Gjubrarko.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gjubrarko.Api.Controllers;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Roles = $"{Roles.Moderator},{Roles.Admin}")]
public class ModerationController(ModerationService moderation) : ControllerBase
{
    /// <summary>Anonymous reports waiting for approval, oldest first.</summary>
    [HttpGet("pending")]
    public Task<IReadOnlyList<PendingReportItem>> Pending(CancellationToken ct) => moderation.PendingAsync(ct);

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await moderation.ApproveAsync(id, User.UserId()!.Value, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/hide")]
    public async Task<IActionResult> Hide(Guid id, [FromQuery] bool hidden = true, CancellationToken ct = default)
    {
        await moderation.SetHiddenAsync(id, hidden, ct);
        return NoContent();
    }
}
