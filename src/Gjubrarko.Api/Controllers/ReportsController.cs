using Gjubrarko.Api.Auth;
using Gjubrarko.Application.Reports;
using Gjubrarko.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gjubrarko.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController(ReportService reports, TurnstileVerifier captcha) : ControllerBase
{
    private const string TokenHeader = "X-Report-Token";

    [HttpGet]
    public Task<PagedResult<ReportListItem>> List([FromQuery] ReportQuery query, CancellationToken ct) =>
        reports.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<ReportDetail> Get(Guid id, [FromHeader(Name = TokenHeader)] string? token, CancellationToken ct) =>
        reports.GetAsync(id, User.UserId(), token, User.IsInRole(Roles.Moderator) || User.IsInRole(Roles.Admin), ct);

    /// <summary>Multipart form: a "report" field with the JSON body and up to 5 "photos" files.</summary>
    [HttpPost]
    [EnableRateLimiting("create-report")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<ActionResult<CreateReportResult>> Create(
        [FromForm(Name = "report")] string reportJson,
        [FromForm(Name = "photos")] List<IFormFile>? photos,
        [FromForm(Name = "captchaToken")] string? captchaToken,
        CancellationToken ct)
    {
        var userId = User.UserId();
        if (userId is null && !await captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct))
            return Problem(statusCode: 400, detail: "Captcha check failed.");

        var request = System.Text.Json.JsonSerializer.Deserialize<CreateReportRequest>(reportJson, JsonOptions);
        if (request is null) return Problem(statusCode: 400, detail: "Missing report data.");

        var uploads = (photos ?? []).Select(f => new PhotoUpload(f.OpenReadStream(), f.FileName, f.Length)).ToList();
        try
        {
            var result = await reports.CreateAsync(request, uploads, userId, ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        finally
        {
            foreach (var u in uploads) await u.Content.DisposeAsync();
        }
    }

    [HttpPost("{id:guid}/photos")]
    [EnableRateLimiting("create-report")]
    public async Task<IActionResult> AddPhotos(Guid id, [FromForm(Name = "photos")] List<IFormFile> photos,
        [FromHeader(Name = TokenHeader)] string? token, CancellationToken ct)
    {
        var uploads = photos.Select(f => new PhotoUpload(f.OpenReadStream(), f.FileName, f.Length)).ToList();
        try
        {
            await reports.AddPhotosAsync(id, uploads, User.UserId(), token, ct);
            return NoContent();
        }
        finally
        {
            foreach (var u in uploads) await u.Content.DisposeAsync();
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };
}
