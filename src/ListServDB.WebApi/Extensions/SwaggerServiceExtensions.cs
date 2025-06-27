using ListServDB.WebApi.Extensions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ListServDB.WebApi.Extensions;

/// <summary>
/// Provides extension methods for configuring Swagger/OpenAPI services and middleware.
/// </summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Adds Swagger/OpenAPI services with default configuration
    /// </summary>
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        return services.AddSwaggerServices(options => { });
    }

    /// <summary>
    /// Adds Swagger/OpenAPI services with custom configuration
    /// </summary>
    public static IServiceCollection AddSwaggerServices(
        this IServiceCollection services,
        Action<SimpleSwaggerOptions> configureOptions)
    {
        var options = new SimpleSwaggerOptions();
        configureOptions(options);

        // Add OpenAPI/Swagger generation services
        services.AddOpenApi(openApiOptions =>
        {
            openApiOptions.AddDocumentTransformer<SwaggerDocumentTransformer>();
        });

        // Store options for use in document transformer
        services.AddSingleton(options);

        return services;
    }

    /// <summary>
    /// Configures Swagger middleware in the request pipeline
    /// </summary>
    public static WebApplication UseSwaggerServices(this WebApplication app)
    {
        return app.UseSwaggerServices(options => { });
    }

    /// <summary>
    /// Configures Swagger middleware with custom options
    /// </summary>
    public static WebApplication UseSwaggerServices(
        this WebApplication app,
        Action<SwaggerUIOptions> configureUI = null)
    {
        // Configure Swagger UI
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
        {
            // Map the OpenAPI endpoint first
            app.MapOpenApi();

            // Add a debug endpoint to verify OpenAPI is working
            app.MapGet("/debug/openapi-check", () =>
            {
                return Results.Ok(new
                {
                    Message = "OpenAPI endpoint should be available at /openapi/v1.json",
                    SwaggerUI = "/swagger",
                    Environment = app.Environment.EnvironmentName
                });
            });

            app.UseSwaggerUI(uiOptions =>
            {
                // Try different endpoint paths
                uiOptions.SwaggerEndpoint("/openapi/v1.json", "API v1");
                uiOptions.RoutePrefix = "swagger";
                uiOptions.DocumentTitle = "RefWire API";

                // Enhanced UI settings
                uiOptions.DefaultModelsExpandDepth(-1);
                uiOptions.DefaultModelRendering(ModelRendering.Model);
                uiOptions.DocExpansion(DocExpansion.None);
                uiOptions.EnableDeepLinking();
                uiOptions.DisplayOperationId();
                uiOptions.DisplayRequestDuration();

                // Apply custom configuration
                configureUI?.Invoke(uiOptions);
            });
        }

        return app;
    }
}

/// <summary>
/// Simplified swagger options for configuring OpenAPI documentation
/// </summary>
public class SimpleSwaggerOptions
{
    /// <summary>
    /// The title of the API documentation
    /// </summary>
    public string Title { get; set; } = "RefWire API";
    /// <summary>
    /// The version of the API
    /// </summary>
    public string Version { get; set; } = "v1";
    /// <summary>
    /// Gets or sets the description text
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the contact.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contact email address
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the URL for contacting support or obtaining additional information.
    /// </summary>
    public string ContactUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the software license.
    /// </summary>
    public string LicenseName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the URL of the license associated with the content.
    /// </summary>
    public string LicenseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Document transformer to apply custom OpenAPI document configuration
/// </summary>
public class SwaggerDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly SimpleSwaggerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerDocumentTransformer"/> class with the specified options.
    /// </summary>
    /// <param name="options">The configuration options used to customize the behavior of the Swagger document transformation. Cannot be null.</param>
    public SwaggerDocumentTransformer(SimpleSwaggerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Transforms the provided OpenAPI document by updating its metadata based on the specified options.
    /// </summary>
    /// <remarks>This method updates the <see cref="OpenApiDocument.Info"/> property of the provided document
    /// with metadata such as title, version, description, contact information, and license details, based on the
    /// configuration options.</remarks>
    /// <param name="document">The OpenAPI document to be transformed. Cannot be null.</param>
    /// <param name="context">The context containing additional information for the transformation process. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the transformation operation. If cancellation is requested, the operation
    /// will terminate early.</param>
    /// <returns>A task that represents the asynchronous transformation operation. The task completes when the document's
    /// metadata has been updated.</returns>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var info = new OpenApiInfo
        {
            Title = _options.Title,
            Version = _options.Version,
            Description = _options.Description
        };

        // Add contact info if provided
        if (!string.IsNullOrEmpty(_options.ContactName) || !string.IsNullOrEmpty(_options.ContactEmail))
        {
            info.Contact = new OpenApiContact
            {
                Name = _options.ContactName,
                Email = _options.ContactEmail
            };

            if (!string.IsNullOrEmpty(_options.ContactUrl) && Uri.TryCreate(_options.ContactUrl, UriKind.Absolute, out var contactUri))
            {
                info.Contact.Url = contactUri;
            }
        }

        // Add license info if provided
        if (!string.IsNullOrEmpty(_options.LicenseName))
        {
            info.License = new OpenApiLicense
            {
                Name = _options.LicenseName
            };

            if (!string.IsNullOrEmpty(_options.LicenseUrl) && Uri.TryCreate(_options.LicenseUrl, UriKind.Absolute, out var licenseUri))
            {
                info.License.Url = licenseUri;
            }
        }

        document.Info = info;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for easier Swagger UI configuration
/// </summary>
public static class SwaggerUIOptionsExtensions
{
    /// <summary>
    /// Enables or disables the "Try It Out" feature in the Swagger UI.
    /// </summary>
    /// <remarks>When enabled, the "Try It Out" feature allows users to interact with API endpoints directly
    /// from the Swagger UI.</remarks>
    /// <param name="options">The <see cref="SwaggerUIOptions"/> instance to configure.</param>
    /// <param name="enable">A value indicating whether to enable the "Try It Out" feature.  <see langword="true"/> to enable; otherwise,
    /// <see langword="false"/>. Defaults to <see langword="true"/>.</param>
    public static void EnableTryItOut(this SwaggerUIOptions options, bool enable = true)
    {
        if (enable)
        {
            options.ConfigObject.AdditionalItems["tryItOutEnabled"] = true;
        }
    }

    /// <summary>
    /// Sets the theme for the Swagger UI.
    /// </summary>
    /// <remarks>The specified theme is applied by adding an entry to the <c>AdditionalItems</c> dictionary 
    /// in the Swagger UI configuration object. Common theme values include <c>dark</c> and <c>light</c>.</remarks>
    /// <param name="options">The <see cref="SwaggerUIOptions"/> instance to configure.</param>
    /// <param name="theme">The name of the theme to apply. Defaults to <see langword="dark"/>.</param>
    public static void SetTheme(this SwaggerUIOptions options, string theme = "dark")
    {
        options.ConfigObject.AdditionalItems["theme"] = theme;
    }

    /// <summary>
    /// Enables or disables the filter functionality in the Swagger UI.
    /// </summary>
    /// <remarks>When enabled, the filter functionality allows users to search and filter API endpoints  in
    /// the Swagger UI. Disabling the filter removes this capability.</remarks>
    /// <param name="options">The <see cref="SwaggerUIOptions"/> instance to configure.</param>
    /// <param name="enable">A value indicating whether to enable the filter functionality.  <see langword="true"/> enables the filter; <see
    /// langword="false"/> disables it.</param>
    public static void EnableFilter(this SwaggerUIOptions options, bool enable = true)
    {
        options.EnableFilter(enable ? string.Empty : null);
    }
}