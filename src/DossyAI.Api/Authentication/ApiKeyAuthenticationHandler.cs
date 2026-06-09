using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DossyAI.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = _configuration["DossyAi:Mcp:ApiKey"];

        if (string.IsNullOrEmpty(configuredKey))
        {
            var devClaims = new[] { new Claim(ClaimTypes.Name, "anonymous") };
            var devIdentity = new ClaimsIdentity(devClaims, Scheme.Name);
            var devPrincipal = new ClaimsPrincipal(devIdentity);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(devPrincipal, Scheme.Name)));
        }

        string? providedKey = null;
        if (Request.Headers.TryGetValue("x-dossyai-key", out var headerValue))
            providedKey = headerValue.FirstOrDefault();
        else if (Request.Query.TryGetValue("key", out var queryValue))
            providedKey = queryValue.FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey))
            return Task.FromResult(AuthenticateResult.Fail("Missing API key"));

        if (!string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var claims = new[] { new Claim(ClaimTypes.Name, "api-client") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
