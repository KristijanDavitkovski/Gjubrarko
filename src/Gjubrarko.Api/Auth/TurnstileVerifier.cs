namespace Gjubrarko.Api.Auth;

/// <summary>Checks a Cloudflare Turnstile token. With no secret configured (local development) every token passes.</summary>
public class TurnstileVerifier(HttpClient http, IConfiguration config)
{
    private sealed record VerifyResponse(bool Success);

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct)
    {
        var secret = config["Turnstile:SecretKey"];
        if (string.IsNullOrEmpty(secret)) return true;
        if (string.IsNullOrEmpty(token)) return false;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = secret, ["response"] = token, ["remoteip"] = remoteIp ?? string.Empty,
        });
        using var response = await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", form, ct);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponse>(ct);
        return body?.Success == true;
    }
}
