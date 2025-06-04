using ListServDB.Security;
using ListServDB.WebApi.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ListServDB.WebApi.Middleware;

/// <summary>
/// Middleware for API key authentication with enhanced security features
/// </summary>
internal class ApiKeyMiddleware(
    RequestDelegate next,
    IOptions<ListServOptions> configuration,
    IListServApiKeyService apiKeyService,
    ILogger<ApiKeyMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IOptions<ListServOptions> _configuration = configuration;
    private readonly IListServApiKeyService _apiKeyService = apiKeyService;
    private readonly ILogger<ApiKeyMiddleware> _logger = logger;
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const int UnauthorizedStatusCode = 401;
    private const int InternalServerErrorStatusCode = 500;
    private const int ForbiddenStatusCode = 403;

    /// <summary>
    /// Invokes the middleware asynchronously.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for swagger endpoints
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var requestPath = context.Request.Path.ToString();
        bool isAdminEndpoint = requestPath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase);

        // If API security is disabled and it's not an admin endpoint, skip authentication
        if (!_configuration.Value.UseApiKeySecurity && !isAdminEndpoint)
        {
            await _next(context);
            return;
        }

        // Check if API key is provided
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey))
        {
            _logger.LogWarning("Request missing API key: {Path}", requestPath);
            await WriteJsonErrorResponse(context, UnauthorizedStatusCode, "API key is required.");
            return;
        }

        string apiKey = providedKey.ToString();

        try
        {
            // Handle admin endpoints
            if (isAdminEndpoint)
            {
                await HandleAdminAuthentication(context, apiKey);
                return;
            }

            // Handle regular API endpoints
            await HandleRegularAuthentication(context, apiKey, requestPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API key authentication");
            await WriteJsonErrorResponse(context, InternalServerErrorStatusCode, "An error occurred during authentication.");
        }
    }

    private async Task HandleAdminAuthentication(HttpContext context, string providedKey)
    {
        string expectedKey = _configuration.Value.ApiKey
            ?? throw new InvalidOperationException("Admin API Key not found in configuration.");

        // Check if the provided key matches the expected key
        if (!string.Equals(providedKey, expectedKey))
        {
            _logger.LogWarning("Invalid admin API key attempt for {Path}", context.Request.Path);
            await WriteJsonErrorResponse(context, ForbiddenStatusCode, "Invalid admin API key.");
            return;
        }

        // Add admin claim for further authorization
        var identity = new System.Security.Claims.ClaimsIdentity("ApiKey");
        identity.AddClaim(new System.Security.Claims.Claim("role", "admin"));
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        await _next(context);
    }

    private async Task HandleRegularAuthentication(HttpContext context, string apiKey, string requestPath)
    {
        // Verify key exists and is valid
        var keyInfo = await _apiKeyService.GetByApiKey(apiKey);
        if (keyInfo is null)
        {
            _logger.LogWarning("Invalid API key used for {Path}", requestPath);
            await WriteJsonErrorResponse(context, ForbiddenStatusCode, "Invalid API key.");
            return;
        }

        // Check if key is expired
        if (keyInfo.ExpiresAt.HasValue && keyInfo.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired API key used for {Path}", requestPath);
            await WriteJsonErrorResponse(context, ForbiddenStatusCode, "API key has expired.");
            return;
        }

        // Record this API usage for analytics, still not sure i want to do this.
        // TODO: implement recording of API usage

        // Add claims for downstream use
        var identity = new System.Security.Claims.ClaimsIdentity("ApiKey");
        identity.AddClaim(new System.Security.Claims.Claim("api-key-id", keyInfo.Id.ToString()));
        identity.AddClaim(new System.Security.Claims.Claim("api-key-owner", keyInfo.Id.ToString()));

        // Add roles as claims if available
        if (keyInfo.Scopes != null)
        {
            foreach (var role in keyInfo.Scopes)
            {
                identity.AddClaim(new System.Security.Claims.Claim("role", role));
            }
        }

        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        await _next(context);
    }

    private static async Task WriteJsonErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var error = new
        {
            status = statusCode,
            message,
            timestamp = DateTime.UtcNow
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, error);
    }
}