FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

# Load environment variables for RefWire application
# See .env file for configuration
ENV \
    REFWIRE__APIKEY="ThisIsTheApiKey" \
    REFWIRE__USEAPIKEYSECURITY=false \
    REFWIRE__AZUREBLOBSTORAGECONNECTIONSTRING="UseDevelopmentStorage" \
    \
    REFWIRE__RATELIMITING__ENABLED=true \
    REFWIRE__RATELIMITING__PERMITLIMIT=100 \
    REFWIRE__RATELIMITING__WINDOW__SECONDS=60 \
    REFWIRE__RATELIMITING__QUEUEPROCESSINGORDER=1 \
    REFWIRE__RATELIMITING__QUEUELIMIT=100 \
    REFWIRE__RATELIMITING__REJECTIONSTATUSCODE=429 \
    \
    REFWIRE__PERSISTENCE__USEAZURE=false \
    REFWIRE__PERSISTENCE__DATASETSDIRECTORY="datasets" \
    REFWIRE__PERSISTENCE__BACKUPSDIRECTORY="backups" \
    REFWIRE__PERSISTENCE__APIKEYSDIRECTORY="apikeys" \
    \
    REFWIRE__ORCHESTRATION__CONTAINERNAME="orchestration" \
    REFWIRE__ORCHESTRATION__CONTROLBLOBNAME="appinstances.json" \
    REFWIRE__ORCHESTRATION__POLLINGINTERVALSECONDS=30 \
    REFWIRE__ORCHESTRATION__USEORCHESTRATION=false \
    REFWIRE__ORCHESTRATION__CIRCUITBREAKERFAILURETHRESHOLD=5 \
    REFWIRE__ORCHESTRATION__CIRCUITBREAKERRESETTIMEOUT=120 \
    REFWIRE__ORCHESTRATION__MAXRETRYATTEMPTS=10 \
    REFWIRE__ORCHESTRATION__RETRYDELAYS="1,2,5,10,30" \
    REFWIRE__ORCHESTRATION__LEASEDURATION=10 \
    \
    REFWIRE__CORS__ALLOWEDORIGINS="*" \
    REFWIRE__CORS__ALLOWEDMETHODS="POST,GET,PUT,DELETE,PATCH" \
    REFWIRE__CORS__ALLOWEDHEADERS="X-Api-Key,Content-Type,Accept,Authorization"
    

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ListServDB.WebApi/ListServDB.WebApi.csproj", "src/ListServDB.WebApi/"]
COPY ["src/ListServDB.Core/ListServDB.Core.csproj", "src/ListServDB.Core/"]
COPY ["src/ListServDb.Orchestration.Azure.Blob/ListServDb.Orchestration.Azure.Blob.csproj", "src/ListServDb.Orchestration.Azure.Blob/"]
COPY ["src/ListServDB.Orchestration/ListServDB.Orchestration.csproj", "src/ListServDB.Orchestration/"]
COPY ["src/ListServDB.Persistence.Azure.Blob/ListServDB.Persistence.Azure.Blob.csproj", "src/ListServDB.Persistence.Azure.Blob/"]
COPY ["src/ListServDB.Persistence/ListServDB.Persistence.csproj", "src/ListServDB.Persistence/"]
COPY ["src/ListServDB.Persistence.FileSystem/ListServDB.Persistence.FileSystem.csproj", "src/ListServDB.Persistence.FileSystem/"]
COPY ["src/ListServDB.Security.Azure.Blob/ListServDB.Security.Azure.Blob.csproj", "src/ListServDB.Security.Azure.Blob/"]
COPY ["src/ListServDB.Security/ListServDB.Security.csproj", "src/ListServDB.Security/"]
COPY ["src/ListServDB.Security.File/ListServDB.Security.File.csproj", "src/ListServDB.Security.File/"]
RUN dotnet restore "./src/ListServDB.WebApi/ListServDB.WebApi.csproj"
COPY . .
WORKDIR "/src/src/ListServDB.WebApi"
RUN dotnet build "./ListServDB.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ListServDB.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ListServDB.WebApi.dll"]
