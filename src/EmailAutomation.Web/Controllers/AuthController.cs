using EmailAutomation.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailAutomation.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IGraphMailService _graphMail;

    public AuthController(IGraphMailService graphMail)
    {
        _graphMail = graphMail;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var isAuth = await _graphMail.IsAuthenticatedAsync(ct);
        return Ok(new { authenticated = isAuth });
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await _graphMail.DisconnectAsync(ct);
        return Ok(new { disconnected = true });
    }

    [HttpGet("login-url")]
    public IActionResult GetLoginUrl()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";
        var url = _graphMail.GetLoginUrl(redirectUri);
        return Ok(new { url });
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? error_description, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
        {
            var msg = string.IsNullOrEmpty(error_description) ? error : $"{error}: {error_description}";
            return Redirect($"/?error={Uri.EscapeDataString(msg)}");
        }

        if (string.IsNullOrEmpty(code))
            return Redirect("/?error=no_code");

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";
        try
        {
            var success = await _graphMail.AcquireTokenByCodeAsync(code, redirectUri, ct);
            return Redirect(success ? "/?connected=1" : "/?error=token_failed");
        }
        catch (Exception ex)
        {
            return Redirect($"/?error={Uri.EscapeDataString(ex.Message)}");
        }
    }
}
