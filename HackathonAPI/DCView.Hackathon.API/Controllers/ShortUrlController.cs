using Microsoft.AspNetCore.Mvc;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.API.Controllers;

/// <summary>
/// Short URL redirect — no auth required. /s/{code} → 302 redirect to full survey URL.
/// </summary>
[ApiController]
public class ShortUrlController : ControllerBase
{
    private readonly ISurveyDistributionRepository _distributionRepo;
    private readonly IConfiguration _config;

    public ShortUrlController(ISurveyDistributionRepository distributionRepo, IConfiguration config)
    {
        _distributionRepo = distributionRepo;
        _config = config;
    }

    [HttpGet("s/{code}")]
    public async Task<IActionResult> Redirect(string code)
    {
        var distribution = await _distributionRepo.GetByShortCodeAsync(code);
        if (distribution == null)
            return NotFound("Link not found or expired.");

        var baseUrl = _config["SurveyEmail:FrontendBaseUrl"]
            ?? _config["FrontendBaseUrl"]
            ?? "http://localhost:5173/novaccodelab";

        var fullUrl = $"{baseUrl}/survey/{distribution.Token}";
        return base.Redirect(fullUrl);
    }
}
