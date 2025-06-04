using ListServDb.Orchestration.Azure.Blob;
using ListServDB.Core.API;
using ListServDB.Core.Caching;
using ListServDB.Core.Concurrency;
using ListServDB.Core.Interfaces;
using ListServDB.Core.Links;
using ListServDB.Orchestration;
using ListServDB.Persistence.Azure.Blob;
using ListServDB.Persistence.FileSystem;
using ListServDB.Security;
using ListServDB.Security.Azure.Blob;
using ListServDB.Security.File;
using ListServDB.WebApi.BackgroundTasks;
using ListServDB.WebApi.Configuration;
using ListServDB.WebApi.Endpoints;
using ListServDB.WebApi.Extensions;
using ListServDB.WebApi.Middleware;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Setup logging first to capture the entire startup process
var logger = ConfigureLogging(builder);
logger.LogInformation("=== RefWireDB Application Starting ===");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

// Configure configuration sources
logger.LogInformation("Configuring application settings...");
ConfigureAppSettings(builder);

// Load configuration
logger.LogInformation("Loading RefWire configuration...");
var listServConfig = builder.Configuration
                         .GetSection("REFWIRE")
                         .Get<ListServOptions>()
                    ?? throw new InvalidOperationException("Missing REFWIRE configuration section!");

// Configure options with the section
builder.Services.Configure<ListServOptions>(builder.Configuration.GetSection("REFWIRE"));
builder.Services.AddSingleton(listServConfig);

// Register all services
logger.LogInformation("Registering application services...");
RegisterServices(builder, logger);

// Configure Kestrel for production
ConfigureKestrel(builder, logger);

// Build the application
logger.LogInformation("Building application...");
var app = builder.Build();

// Configure middleware pipeline
logger.LogInformation("Configuring middleware pipeline...");
ConfigureMiddleware(app, logger);

// Map API endpoints
logger.LogInformation("Mapping API endpoints...");
MapApiEndpoints(app, logger);

// Start the application
logger.LogInformation("=== Application startup complete - Running application now ===");
app.Run();

// Configure logging
ILogger<Program> ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
        loggingBuilder.AddDebug();
    });
    return LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<Program>();
}

// Configure application settings sources
void ConfigureAppSettings(WebApplicationBuilder builder)
{
    // Clear default sources so we control exactly what comes from where
    logger.LogDebug("Clearing default configuration sources");
    builder.Configuration.Sources.Clear();

    // Add environment variables as primary source
    logger.LogDebug("Adding environment variables configuration source with prefix 'REFWIRE__'");

    // Configure environment-specific settings
    if (builder.Environment.IsDevelopment())
    {
        ConfigureDevelopmentEnvironment(builder);
    }
    else
    {
        ConfigureProductionEnvironment(builder);
    }
}

// Configure development environment settings
void ConfigureDevelopmentEnvironment(WebApplicationBuilder builder)
{
    logger.LogInformation("Development environment detected - Adding development configuration sources");

    logger.LogDebug("Adding user secrets");
    builder.Configuration.AddUserSecrets<Program>();

    logger.LogDebug("Adding JSON configuration files");
    builder.Configuration
           .SetBasePath(builder.Environment.ContentRootPath)
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
}

void ConfigureProductionEnvironment(WebApplicationBuilder builder)
{
    logger.LogInformation("Production environment detected - Adding production configuration sources");
    logger.LogDebug("Adding environment variables");
    builder.Configuration.AddEnvironmentVariables();
}

// Configure Kestrel for production
void ConfigureKestrel(WebApplicationBuilder builder, ILogger<Program> logger)
{
    if (!builder.Environment.IsDevelopment())
    {
        logger.LogInformation("Configuring Kestrel for production environment");
        builder.WebHost.ConfigureKestrel(options =>
        {
            logger.LogDebug("Setting Kestrel to listen on all IP addresses on port 80");
            options.ListenAnyIP(80); // Listen on all interfaces on port 80 only
        });

        builder.WebHost.UseUrls("http://*:80");
        logger.LogInformation("Kestrel configured to listen on http://*:80");
    }
}

// Register application services
void RegisterServices(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("=== Beginning Service Registration ===");

    // Register core services
    RegisterCoreServices(builder, logger);

    // Configure persistence layer
    ConfigurePersistence(builder, logger);

    // Configure security services
    ConfigureSecurity(builder, logger);

    // Configure orchestration services
    ConfigureOrchestration(builder, logger);

    // Configure rate limiting
    ConfigureRateLimiting(builder, logger);

    // Configure CORS
    ConfigureCors(builder, logger);

    logger.LogInformation("=== Service Registration Complete ===");
}

// Register core services
void RegisterCoreServices(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("Registering core services");

    logger.LogDebug("Adding memory cache");
    builder.Services.AddMemoryCache();

    logger.LogDebug("Adding concurrency manager");
    builder.Services.AddSingleton<IConcurrencyManager, ConcurrencyManager>();

    logger.LogDebug("Adding dataset cache");
    builder.Services.AddSingleton<IDatasetCache, DatasetCache>();

    logger.LogDebug("Adding link enricher service");
    builder.Services.AddSingleton<ILinkEnricher, LinkEnricher>();

    logger.LogDebug("Adding RefWire API");
    builder.Services.AddSingleton<IListServApi, ListServApi>();

    logger.LogDebug("Adding health metrics provider");
    builder.Services.AddListServMetrics();

    logger.LogInformation("Core services registered successfully");
}

// Configure persistence services
void ConfigurePersistence(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("Configuring persistence layer");

    if (listServConfig.Persistence.UseAzure)
    {
        logger.LogInformation("Using Azure Blob Storage for persistence");

        logger.LogDebug("Connection string present: {HasConnectionString}",
            !string.IsNullOrEmpty(listServConfig.AzureBlobConnectionString));
        logger.LogDebug("Datasets directory: {DatasetsDirectory}",
            listServConfig.Persistence.DatasetsDirectory);
        logger.LogDebug("Backups directory: {BackupsDirectory}",
            listServConfig.Persistence.BackupsDirectory);

        builder.Services.AddSingleton<IDatasetPersistenceManager>(sp =>
            new AzureBlobPersistenceManager(
                listServConfig.AzureBlobConnectionString,
                listServConfig.Persistence.DatasetsDirectory,
                listServConfig.Persistence.BackupsDirectory));

        logger.LogInformation("Azure Blob Storage persistence configured successfully");
    }
    else
    {
        logger.LogInformation("Using File System for persistence");

        string datasetsBaseDir = listServConfig.Persistence.DatasetsDirectory;
        if (string.IsNullOrEmpty(datasetsBaseDir))
        {
            logger.LogCritical("Datasets directory is not configured");
            throw new InvalidOperationException("Datasets directory is not configured");
        }

        logger.LogDebug("Datasets directory: {DatasetsDirectory}", datasetsBaseDir);

        builder.Services.AddSingleton<IDatasetPersistenceManager>(sp =>
            new FilePersistenceManager(datasetsBaseDir));

        logger.LogInformation("File System persistence configured successfully");
    }
}

// Configure security services
void ConfigureSecurity(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("Configuring security services");
    logger.LogDebug("API Key Security Enabled: {UseApiKeySecurity}", listServConfig.UseApiKeySecurity);

    if (!listServConfig.UseApiKeySecurity)
    {
        logger.LogWarning("API Key security is disabled. Using null implementation");
        builder.Services.AddSingleton<IListServApiKeyService, NullApiKeyService>();
        return;
    }

    // Configure API key storage repository based on persistence mechanism
    if (listServConfig.Persistence.UseAzure)
    {
        logger.LogInformation("Using Azure Blob Storage for API key management");
        logger.LogDebug("API Keys container: {ApiKeysDirectory}", listServConfig.Persistence.ApiKeysDirectory);

        builder.Services.AddBlobStorageApiKeyRepository(options =>
        {
            options.ConnectionString = listServConfig.AzureBlobConnectionString;
            options.ContainerName = listServConfig.Persistence.ApiKeysDirectory;
            options.CacheExpirationTime = TimeSpan.FromMinutes(10);

            logger.LogDebug("Cache expiration time: {CacheExpirationTime} minutes", options.CacheExpirationTime.TotalMinutes);
        });
    }
    else
    {
        logger.LogInformation("Using encrypted file storage for API key management");

        builder.Services.AddEncryptedFileApiKeyRepository(options =>
        {
            options.FilePath = "data/api-keys.json.enc";
            options.CacheExpirationTime = TimeSpan.FromMinutes(15);

            // For testing only: using static sample Base64-encoded strings.
            options.EncryptionKey = Convert.FromBase64String("MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI=");
            options.EncryptionIV = Convert.FromBase64String("MTIzNDU2Nzg5MDEyMzQ1Ng==");

            logger.LogDebug("API keys file path: {FilePath}", options.FilePath);
            logger.LogDebug("Cache expiration time: {CacheExpirationTime} minutes", options.CacheExpirationTime.TotalMinutes);
            logger.LogWarning("Using static encryption keys for testing - Not secure for production");
        });
    }

    logger.LogInformation("Security services configured successfully");
}

// Configure orchestration services
void ConfigureOrchestration(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("Configuring orchestration services");
    logger.LogDebug("Orchestration Enabled: {UseOrchestration}", listServConfig.Orchestration.UseOrchestration);

    if (listServConfig.Orchestration.UseOrchestration)
    {
        logger.LogInformation("Setting up distributed instance management");
        logger.LogDebug("Container name: {ContainerName}", listServConfig.Orchestration.ContainerName);
        logger.LogDebug("Circuit breaker threshold: {Threshold}", listServConfig.Orchestration.CircuitBreakerFailureThreshold);
        logger.LogDebug("Circuit breaker reset timeout: {ResetTimeout} minutes", listServConfig.Orchestration.CircuitBreakerResetTimeout);
        logger.LogDebug("Max retry attempts: {MaxRetries}", listServConfig.Orchestration.MaxRetryAttempts);

        builder.Services.AddSingleton<IDistributedInstanceManager>(sp =>
        {
            var instanceLogger = sp.GetRequiredService<ILogger<AzureBlobDistributedInstanceManager>>();
            var config = new DistributedInstanceManagerConfig
            {
                ContainerName = listServConfig.Orchestration.ContainerName,
                CircuitBreakerFailureThreshold = listServConfig.Orchestration.CircuitBreakerFailureThreshold,
                CircuitBreakerResetTimeout = TimeSpan.FromMinutes(listServConfig.Orchestration.CircuitBreakerResetTimeout),
                MaxRetryAttempts = listServConfig.Orchestration.MaxRetryAttempts,
                RetryDelays = listServConfig.Orchestration.RetryDelays.Select(delay => TimeSpan.FromSeconds(delay)).ToArray()
            };
            return new AzureBlobDistributedInstanceManager(instanceLogger, listServConfig.AzureBlobConnectionString, config);
        });

        // Add the hosted service to initialize
        logger.LogDebug("Adding orchestration background service");
        builder.Services.AddHostedService<OrchestrationBackgroundService>();

        logger.LogInformation("Orchestration services configured successfully");
    }
    else
    {
        logger.LogInformation("Orchestration services disabled - skipping configuration");
    }
}

// Configure rate limiting
void ConfigureRateLimiting(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("Configuring rate limiting");
    logger.LogDebug("Rate limiting enabled: {Enabled}", listServConfig.RateLimiting.Enabled);

    if (listServConfig.RateLimiting.Enabled)
    {
        logger.LogDebug("Permit limit: {PermitLimit} requests", listServConfig.RateLimiting.PermitLimit);
        logger.LogDebug("Window: {Window} seconds", listServConfig.RateLimiting.Window);
        logger.LogDebug("Queue limit: {QueueLimit}", listServConfig.RateLimiting.QueueLimit);
        logger.LogDebug("Rejection status code: {StatusCode}", listServConfig.RateLimiting.RejectionStatusCode);

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = listServConfig.RateLimiting.PermitLimit,
                        Window = listServConfig.RateLimiting.Window,
                        QueueProcessingOrder = (QueueProcessingOrder)listServConfig.RateLimiting.QueueProcessingOrder,
                        QueueLimit = listServConfig.RateLimiting.QueueLimit
                    });
            });
            options.RejectionStatusCode = listServConfig.RateLimiting.RejectionStatusCode;
        });

        logger.LogInformation("Rate limiting configured successfully");
    }
    else
    {
        logger.LogInformation("Rate limiting disabled - skipping configuration");
    }
}

// Configure CORS
void ConfigureCors(WebApplicationBuilder builder, ILogger<Program> logger)
{
    logger.LogInformation("Configuring CORS policy");

    logger.LogDebug("Allowed origins: {Origins}", string.Join(", ", listServConfig.Cors.AllowedOrigins));
    logger.LogDebug("Allowed methods: {Methods}", string.Join(", ", listServConfig.Cors.AllowedMethods));
    logger.LogDebug("Allowed headers: {Headers}", string.Join(", ", listServConfig.Cors.AllowedHeaders));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ListServCorsPolicy", policy =>
        {
            policy.WithOrigins(listServConfig.Cors.AllowedOrigins)
                  .WithMethods(listServConfig.Cors.AllowedMethods)
                  .WithHeaders(listServConfig.Cors.AllowedHeaders);
        });
    });

    logger.LogInformation("CORS policy configured successfully");
}

// Configure middleware pipeline
void ConfigureMiddleware(WebApplication app, ILogger<Program> logger)
{
    logger.LogInformation("=== Configuring Middleware Pipeline ===");

    // Rate limiter middleware
    if (listServConfig.RateLimiting.Enabled)
    {
        logger.LogDebug("Adding rate limiter middleware");
        app.UseRateLimiter();
    }

    // CORS middleware
    logger.LogDebug("Adding CORS middleware");
    app.UseCors("ListServCorsPolicy");

    // API key security check and middleware
    if (listServConfig.UseApiKeySecurity)
    {
        if (string.IsNullOrEmpty(listServConfig.ApiKey))
        {
            logger.LogCritical("API key security is enabled but no API key is configured");
            throw new InvalidOperationException("API key is not configured");
        }

        logger.LogDebug("Adding API key security middleware");
        app.UseMiddleware<ApiKeyMiddleware>();
    }

    // Request logging middleware
    logger.LogDebug("Adding request logging middleware");
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Routing middleware
    logger.LogDebug("Adding routing middleware");
    app.UseRouting();

    logger.LogInformation("=== Middleware Pipeline Configured ===");
}

// Map API endpoints
void MapApiEndpoints(WebApplication app, ILogger<Program> logger)
{
    logger.LogInformation("=== Mapping API Endpoints ===");

    logger.LogDebug("Mapping datasets endpoints");
    app.MapDatasetsEndpoints();

    logger.LogDebug("Mapping API keys endpoints");
    app.MapApiKeysEndpoints(app.Services.GetRequiredService<IOptions<ListServOptions>>());

    logger.LogDebug("Mapping items endpoints");
    app.MapItemsEndpoints();

    logger.LogDebug("Mapping backup endpoints");
    app.MapBackupEndpoints();

    logger.LogDebug("Mapping health endpoints");
    app.MapHealthEndpoints();

    logger.LogDebug("Mapping distributed app instance manager endpoints");
    app.MapDistributedAppInstanceManagerEndpoints(app.Services.GetRequiredService<IOptions<ListServOptions>>());

    logger.LogInformation("=== API Endpoints Mapped Successfully ===");
}