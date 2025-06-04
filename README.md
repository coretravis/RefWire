# RefWire

[![GitHub stars](https://img.shields.io/github/stars/coretravis/RefWire.svg?style=social)](https://github.com/coretravis/RefWire/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/coretravis/RefWire.svg?style=social)](https://github.com/coretravis/RefWire/network)
[![GitHub issues](https://img.shields.io/github/issues/coretravis/RefWire.svg)](https://github.com/coretravis/RefWire/issues)

**RefWire** is a **lightweight, high-performance reference data service** designed to efficiently manage and serve **mostly static datasets**—such as countries, currencies, languages, or product categories. RefWire Instantly transforms a JSON array of objects into ready-to-use APIs. It excels at providing rapid lookups and searches by leveraging **intelligent in-memory caching and efficient indexing**. Complementing the core service, the RefWire ecosystem includes the [RefWireCLI](https://github.com/coretravis/RefWireCLI) for comprehensive command-line management, [ListStor](https://refwire.online/stor) for accessing ready-to-use standardized datasets (**RefPack**), and [RefWire Explor](https://refwire.online/explor) for easy API interaction and testing. It includes features such as pluggable persistence, optional API key security, rate limiting, and simple distributed orchestration capabilities.

---

## Table of Contents

- [Why RefWire?](#why-refwire)
  - [Ideal Use Cases](#ideal-use-cases)
- [Quickstart](#quickstart)
- [Features](#features)
- [Architecture](#architecture)
  - [Pluggable Persistence](#pluggable-persistence)
  - [Caching & Preloading](#caching--preloading)
  - [Security & Rate Limiting](#security--rate-limiting)
  - [Distributed Orchestration](#distributed-orchestration)
- [CLI Management – RefWireCLI](#cli-management--refwirecli)
- [ListStor – Standardized Datasets](#liststor--standardized-datasets)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
    - [Source Installation](#source-installation)
    - [Docker Installation](#docker-installation)
- [Configuration](#configuration)
  - [Sample Environment File](#sample-environment-file)
- [Usage](#usage)
  - [API Endpoints](#api-endpoints)
- [Tests](#tests)
- [Contributing](#contributing)
- [Communication](#communication)
- [License](#license)
- [Contact](#contact)

---

## Why RefWire?

Modern applications often rely on consistent and reliable **reference data**—datasets like country codes, currencies, time zones, or product taxonomies that change infrequently but are used widely across systems. Traditional databases or bloated data services are often overkill for managing such static, read-heavy data.

**RefWire** was built specifically for this gap:

* 🧠 **Smart caching & indexing** – For lightning-fast responses to common lookups and filtered queries.
* ⚙️ **Minimal configuration** – Just point it at a JSON array, and you instantly get a full-featured API.
* 🚀 **Designed for performance** – Built on .NET 9 Minimal APIs with efficient memory use.
* 🔐 **Secure out-of-the-box** – With optional API key enforcement and per-client rate limiting.
* 🔌 **Pluggable architecture** – Swap out storage backends (Azure, Local FS, etc.) or orchestration logic as needed.
* 🧩 **Easily embeddable** – Use as a microservice in your stack or run it standalone for internal tooling.

### Ideal Use Cases:

* **Global reference data** (e.g., countries, currencies, languages)
* **Product catalogs and taxonomies**
* **Standard code lists** (e.g., ISO, UN, IATA)
* **Static metadata for frontend apps** (e.g., dropdowns, filters)
* **Read-heavy services in distributed systems**
* **Mock API generation from JSON samples**

If you’ve ever found yourself serving static JSON files from an S3 bucket or maintaining a CRUD app just to handle lookup tables—**RefWire is for you.**

---

## Quickstart

Using DockerHub
   ```bash
   docker pull coretravis/refwire:latest
   docker run -d -p 7010:80 --name refwire coretravis/refwire:latest
   ```

   **Manage RefWire:**
   You can manage RefWire at `http://localhost:7010` via the RefWireCLI.

   ```bash
  npm install -g @coretravis/refwire
  
  refwire dataset list-ids
  ```
  This will prompt you for:
  - RefWire Url: Your RefWire Instance URL `http://localhost:7010`
  - ApiKey (Use 'ThisIsTheApiKey' for demo purposes but make sure to set a strong key for production via Configuration)
  - ListStor Server Url (optional): Set this to `https://refpack.refwire.online` to use the `refwire dataset pull {datasetID}` command which gives you access to standardized datasets         found at https://stor.refwire.online
  - **Note: You will only need to set the configuration only once**

  **Add your first dataset**
  ```bash
  refwire dataset pull currencies
  ```
  **Or Import your first dataset from a JSON array file**
  ```bash
  refwire dataset import
  ```
 

  **Explore RefWire APIs:**
  You can explore the RefWire APIs using the [RefWire Explor](https://refwire.online/explor) API Explorer, which provides an interactive interface to test endpoints and view responses.
  **For more details on managing RefWire, check out the [RefWireCLI documentation](https://github.com/coretravis/RefWireCLI)

---

## Features

- **Cost Effective & Lightweight**
  - Stores standardized JSON datasets via a unified persistence interface.
  - Supports persistence using **Azure Blob Storage** or **Local File System** by default.
  - Loads data on demand with preloading for frequently accessed datasets to minimize latency.

- **High Performance**
  - In-memory caching with intelligent eviction and efficient indexing (including simple text searches).
  - Built on **.NET 9 Minimal APIs** ensuring minimal overhead and rapid startup.

- **Built-in Security & Rate Limiting**
  - Optional API key security with keys stored encrypted and loaded into memory.
  - Configurable rate limiting per client IP and comprehensive CORS policies protect the service.

- **Distributed Orchestration**
  - Supports multi-instance deployments via a simple leader/follower orchestration pattern.
  - Orchestration data is managed through a pluggable provider model, allowing custom backends.

- **Easy Integration & Management**
  - Fully configurable through environment variables and configuration files.
  - Available as a container for easy deployment.
  - **[RefWire CLI](https://github.com/coretravis/RefWireCLI)** An open-source npm CLI tool for managing RefWire instances. It provides full commands to administer API keys, datasets (including an **interactive dataset import wizard**), items, and distributed instances directly from the command line.
  - **[ListStor](https://refwire.online/stor)** A dedicated repository of standardized (**RefPack**), categorized reference datasets, enabling quick access to pre-built, ready-to-use datasets.

---

## Architecture

### Pluggable Persistence

- **Unified Provider Interface:**  
  RefWire stores/retrieves datasets and API keys as JSON using a common interface. Built-in implementations include:
  - **Azure Blob Storage Provider:** For scalable, geo-replicated cloud storage.
  - **Local File System Provider:** Suitable for development, on-premise and depending on your requirements even production deployments.
  
- **Extensibility:**  
  Developers can integrate custom providers (e.g., Amazon S3, Google Cloud Storage and even OneDrive or Google Drive) by implementing the defined interface.

### Caching & Preloading

- **In-Memory Caching:**  
  Datasets are loaded on demand and cached with sliding expiration. Frequently accessed datasets can be preloaded at startup to reduce response times.
  
- **Efficient Indexing:**  
  Advanced indexing—such as suffix trees—supports rapid text searches and fast lookups.

### Security & Rate Limiting

- **API Key Middleware:**  
  Endpoints are secured using API key validation. Admin endpoints compare against a configured key, while regular endpoints validate keys via a dedicated service.
  
- **Rate Limiting & CORS:**  
  Configurable limits based on client IP and custom CORS policies help protect the service from abuse.

### Distributed Orchestration

- **Leader/Follower Model:**  
  Multiple RefWire instances can coordinate, share state.
  
- **Pluggable Orchestration Provider:**  
  Managed via a provider interface for flexibility with custom orchestration backends. The default is an Azure Blob Storage provider.
  
- **CLI Integration:**  
  **RefWireCLI** provides commands to manage distributed instances (e.g., register, list, remove, elect leaders) directly from the command line.

---

## CLI Management – RefWireCLI

**RefWireCLI** is an open-source command-line administration tool designed to manage RefWire instances/servers. It offers:

- **API Key Management:** Create, list, update, retrieve, and revoke API keys.
- **Dataset Administration:** Create, update, import (with an interactive wizard), pull, and delete datasets.
- **Item Operations:** Add, update, or archive both single and bulk items.
- **Health & Instance Monitoring:** Retrieve system health reports and manage distributed app instances.
- **Interactive Dataset Import:** Launches a guided wizard for interactive dataset import and configuration.

For installation and detailed usage, visit the [RefWireCLI GitHub repository](https://github.com/coretravis/RefWireCLI).

---

## ListStor – Standardized Datasets

**ListStor** is a repository of categorized, standardized (RefPack) reference/lookup datasets designed for immediate use with RefWire. ListStor provides ready-to-use datasets—such as countries, currencies, languages, and more—so you don’t have to spend time manually gathering data. Access and download these datasets from [ListStor](https://refwire.online/stor) or use them directly from the RefWireCLI with a simple command.

```bash
refwire dataset pull {datasetID}
```

---

## Getting Started

### Prerequisites

- **.NET 9 SDK** – For building and running RefWire.
- **Docker** (optional) – For containerized deployments.
- **Node.js (v14 or later) and NPM** – To install and use **RefWireCLI**.

### Installation Steps (RefWire)

RefWire can be installed and run in multiple ways.
Below are the steps for both building from Source and Docker-based installations.

#### Source Installation (Using .NET CLI)

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/coretravis/RefWire.git
   cd RefWire
   ```

2. **Restore Dependencies:**

   ```bash
   dotnet restore
   ```

3. **Build the Application:**

   ```bash
   dotnet build
   ```

4. **Run the Application:**

   ```bash
   dotnet run --urls "http://localhost:7010" 
   ```

---

#### Docker Installation

   RefWire can also be run as a Docker container, which simplifies deployment and scaling. You can either build the image locally or pull it from Docker Hub.

   1. **Build the Docker Image:**
   ```bash
   docker build -t refwire:latest .
   docker run -p 7010:80 refwire:latest
   ```
   - **Note:** The above command will build the image and run it on port 7010. You can change the port as needed.

   Using DockerHub
   ```bash
   docker pull coretravis/refwire:latest
   docker run -d -p 7010:80 --name refwire coretravis/refwire:latest
   ```

   **Access RefWire:**
   You can access RefWire at `http://localhost:7010` via your API client or test using the **[RefWire Explor](https://refwire.online/explor)** Api Explorer.
   
   ***Management via RefWireCLI:***
   Check out the [RefWireCLI](https://github.com/coretravis/RefWireCLI) repository for installation and usage instructions.
   
---

## Configuration

RefWire is highly configurable via environment variables and configuration files. The sample environment file below illustrates key configuration parameters:

### Sample Environment File

```dotenv
# RefWire API Configuration
# ---------------------------
# APIKEY: Your unique authentication key for accessing the API
# USEAPIKEYSECURITY: Enable API key validation for secured endpoints (recommended: true)
# AZUREBLOBSTORAGECONNECTIONSTRING: Connection string for Azure Blob Storage 
#   - For local development, use "UseDevelopmentStorage=true" with Azure Storage Emulator
#   - For production, use your actual Azure Storage connection string
REFWIRE__APIKEY="ThisIsTheApiKey"
REFWIRE__USEAPIKEYSECURITY=true
REFWIRE__AZUREBLOBSTORAGECONNECTIONSTRING="UseDevelopmentStorage=true"

# Rate Limiting Configuration
# --------------------------
# Controls request throttling to prevent abuse and ensure service stability
# ENABLED: Turn rate limiting on/off
# PERMITLIMIT: Maximum number of requests allowed within the time window
# WINDOW__SECONDS: Time window for rate limiting in seconds
# QUEUEPROCESSINGORDER: How queued requests are processed (0=unspecified, 1=FIFO, 2=LIFO)
# QUEUELIMIT: Maximum number of requests to queue when rate limit is reached (0=no queue)
# REJECTIONSTATUSCODE: HTTP status code returned when rate limit is exceeded
REFWIRE__RATELIMITING__ENABLED=true
REFWIRE__RATELIMITING__PERMITLIMIT=100
REFWIRE__RATELIMITING__WINDOW__SECONDS=60
REFWIRE__RATELIMITING__QUEUEPROCESSINGORDER=1
REFWIRE__RATELIMITING__QUEUELIMIT=100
REFWIRE__RATELIMITING__REJECTIONSTATUSCODE=429

# Persistence Configuration
# ------------------------
# Controls data storage options
# USEAZURE: When true, uses Azure storage; when false, uses local file system
# DATASETSDIRECTORY: Directory for storing datasets when using local storage
# BACKUPSDIRECTORY: Directory for storing backups when using local storage
# APIKEYSDIRECTORY: Directory for storing API keys when using local storage
REFWIRE__PERSISTENCE__USEAZURE=false
REFWIRE__PERSISTENCE__DATASETSDIRECTORY="datasets"
REFWIRE__PERSISTENCE__BACKUPSDIRECTORY="backups"
REFWIRE__PERSISTENCE__APIKEYSDIRECTORY="apikeys"

# Orchestration Configuration
# --------------------------
# Controls distributed application coordination and resilience
# CONTAINERNAME: Name of the blob container used for orchestration
# CONTROLBLOBNAME: Name of the blob used to store application instance information
# POLLINGINTERVALSECONDS: How often to check for orchestration updates
# USEORCHESTRATION: Enable/disable distributed orchestration features
# CIRCUITBREAKERFAILURETHRESHOLD: Number of failures before circuit breaker opens
# CIRCUITBREAKERRESETTIMEOUT: Seconds to wait before attempting to close circuit breaker
# MAXRETRYATTEMPTS: Maximum number of retry attempts for failed operations
# RETRYDELAYS: Comma-separated list of delays (in seconds) between retry attempts
# LEASEDURATION: Duration in seconds for blob leases in leader election
REFWIRE__ORCHESTRATION__CONTAINERNAME="orchestration"
REFWIRE__ORCHESTRATION__CONTROLBLOBNAME="appinstances.json"
REFWIRE__ORCHESTRATION__POLLINGINTERVALSECONDS=30
REFWIRE__ORCHESTRATION__USEORCHESTRATION=false
REFWIRE__ORCHESTRATION__CIRCUITBREAKERFAILURETHRESHOLD=5
REFWIRE__ORCHESTRATION__CIRCUITBREAKERRESETTIMEOUT=120
REFWIRE__ORCHESTRATION__MAXRETRYATTEMPTS=10
REFWIRE__ORCHESTRATION__RETRYDELAYS="1,2,5,10,30"
REFWIRE__ORCHESTRATION__LEASEDURATION=10

# CORS Configuration
# ----------------
# Cross-Origin Resource Sharing settings for browser security
# ALLOWEDORIGINS: Origins allowed to access the API
#   - Use "*" for development only
#   - For production, specify exact origins (e.g., "https://myapp.com,https://admin.myapp.com")
# ALLOWEDMETHODS: HTTP methods that can be used when accessing the API
# ALLOWEDHEADERS: HTTP headers that can be included in requests to the API
REFWIRE__CORS__ALLOWEDORIGINS="*"
REFWIRE__CORS__ALLOWEDMETHODS="POST,GET,PUT,DELETE,PATCH"
REFWIRE__CORS__ALLOWEDHEADERS="X-Api-Key,Content-Type,Accept,Authorization"

# Environment
# ----------
# ASPNETCORE__ENVIRONMENT: Application environment
#   - "Development": Enables developer-friendly features like detailed error pages
#   - "Production": Optimizes for security and performance (use for live deployments)
#   - "Staging": For pre-production testing
ASPNETCORE__ENVIRONMENT="Development"

# Logging Configuration
# -------------------
# LOGLEVEL: Minimum level of events to capture
#   - Options: Trace, Debug, Information, Warning, Error, Critical, None
#   - "Information" is recommended for most scenarios
#   - Use "Debug" or "Trace" for troubleshooting
LOGGING__LOGLEVEL="Information"
```

Adjust these variables as necessary for your deployment environment.

---

## Usage

<details>
<summary><strong>API Endpoints</strong></summary>

RefWire exposes RESTful API endpoints for interacting with datasets and items, along with administrative functions. Endpoints are generally divided into:

1.  **Public Item Endpoints:** Primarily for reading dataset items. These are the most commonly used endpoints for applications consuming RefWire data.
2.  **Administrative Endpoints:** For managing datasets, items, API keys, backups, and system health. These typically require authentication (e.g., using the master `REFWIRE__APIKEY` or a specific admin API key) and are often prefixed with `/admin/`.

<details>
<summary><strong>Item Endpoints (Public Access)</strong></summary>

These endpoints allow querying items within a specific dataset. They offer powerful features like selective field inclusion (`includeFields`) and linking related data from other datasets (`links`).

*   **`GET /datasets/{id}/items/{skip}/{take}`**
    *   **Description:** Retrieves a paginated list of non-archived items from the specified dataset (`{id}`). Use `{skip}` for the starting index (0-based) and `{take}` for the maximum number of items per page.
    *   **Query Parameters:** Supports `includeFields` and `links` (see explanation below).

*   **`GET /datasets/{id}/items/{itemId}`**
    *   **Description:** Retrieves a single, non-archived item by its unique identifier (`{itemId}`) from the specified dataset (`{id}`).
    *   **Query Parameters:**
        *   Supports `includeFields` and `links` (see explanation below).    

*   **`POST /datasets/{id}/items/search-by-ids`**
    *   **Description:** Retrieves multiple non-archived items by their specific IDs from the specified dataset (`{id}`). Send a JSON array of item IDs in the request body (e.g., `["id1", "id2", "id3"]`).
    *   **Query Parameters:** Supports `includeFields` and `links` (see explanation below).

*   **`GET /datasets/{id}/items/{skip}/{take}/search`**
    *   **Description:** Performs a text search against the `NameField` of non-archived items within the specified dataset (`{id}`). Provides paginated results.
    *   **Query Parameters:**
        *   `searchTerm` (Required): The text string to search for.
        *   Supports `includeFields` and `links` (see explanation below).

**Understanding `includeFields` and `links` Query Parameters:**

These optional parameters, available on the `List`, `SearchByIds`, `GetById` and `Search` item endpoints, allow you to customize the data returned for each item:

*   **`includeFields`**:
    *   **Purpose:** Controls which specific fields from an item's `Data` object are included in the response. This helps reduce payload size when only certain details are needed.
    *   **Format:** A comma-separated string of field names (e.g., `?includeFields=capital,currency_code,population`).
    *   **Behavior:**
        *   **If provided:** Each returned item will include its standard `Id` and `Name`, and its `Data` object will contain *only* the fields specified in the `includeFields` list.
        *   **If omitted:** Items are returned with only their `Id` and `Name`. The `Data` object will be empty or minimal (it might still contain data added via the `links` parameter, if used). This provides a lightweight response when detailed data isn't needed.

*   **`links`**:
    *   **Purpose:** Fetches and embeds related data from *other* datasets into the current item's response, based on a defined relationship. This enables retrieving connected information (e.g., a country and its associated airports) in a single API call.
    *   **Format:** A comma-separated string specifying the relationship in the format `linkedDatasetId-foreignKeyField`.
        *   `linkedDatasetId`: The ID of the dataset you want to pull related data *from*.
        *   `foreignKeyField`: The name of the field *within the linked dataset* that contains the value matching the `Id` of the items in the *current* dataset being queried.
    *   **Example:** Querying a `countries` dataset (items have an `Id` like `USA`) and wanting to include related airports from an `airports` dataset (where airport items have a field `countryid_iso3` containing `USA`). Use:
        `?links=airports-countryid_iso3`
    *   **Behavior:** For each item returned from the primary dataset query (e.g., a specific country):
        1.  RefWire takes the primary item's `Id`.
        2.  It searches the specified `linkedDatasetId` (e.g., `airports`).
        3.  It finds all items within that linked dataset where the value of the `foreignKeyField` (e.g., `countryid_iso3`) matches the primary item's `Id`.
        4.  These found related items (e.g., all airports for `USA`) are then embedded within the primary item's `Data` object. The structure typically involves adding a key to the `Data` object named after the `linkedDatasetId` (e.g., `"airports"`) containing an array of the matching linked items.

</details>

<details>
<summary><strong>Administrative Endpoints (Require Authentication)</strong></summary>

These endpoints are used for managing the RefWire instance and require appropriate authentication/authorization.

**Dataset Management:**

*   `GET /datasets/`: Retrieves a list of all datasets (basic info like Id, Name, Fields - *Public, but often used in admin contexts*).
*   `GET /datasets/{id}`: Retrieves detailed configuration for a specific dataset (excluding items - *Public, but often used in admin contexts*).
*   `GET /admin/datasets/list`: Retrieves a simple list of all dataset IDs.
*   `GET /admin/datasets/{id}/meta`: Retrieves detailed metadata and statistics for a specific dataset.
*   `POST /admin/datasets/`: Creates a new dataset, potentially including initial items.
*   `PUT /admin/datasets/{id}`: Updates the configuration (Name, Description, Fields) of an existing dataset.
*   `DELETE /admin/datasets/{id}`: Deletes a dataset permanently.
*   `GET /admin/datasets/{id}/api/spec`: Retrieves a dynamic API specification (fully qualified endpoint URLs) for the specified dataset.
*   `GET /admin/datasets/state`: Retrieves a full snapshot of the current state of all datasets (potentially very large response).

**Item Management (Admin):**

*   `POST /admin/datasets/{id}/items`: Adds a single new item to a dataset.
*   `POST /admin/datasets/{id}/items/bulk`: Adds multiple new items to a dataset in a single request.
*   `PUT /admin/datasets/{id}/items/{itemId}`: Updates an existing item in a dataset.
*   `DELETE /admin/datasets/{id}/items/{itemId}`: Archives (soft-deletes) an item in a dataset.

**API Key Management** *(Only available if API Key Security is enabled)*:

*   `POST /admin/api-keys/`: Creates a new API key, returning the secret key value once.
*   `GET /admin/api-keys/`: Lists all API keys (excluding the secret key value).
*   `GET /admin/api-keys/{id}`: Retrieves details for a specific API key (excluding the secret key value).
*   `PUT /admin/api-keys/{id}`: Updates an existing API key (e.g., Name, Description, Scopes).
*   `DELETE /admin/api-keys/{id}`: Revokes (deletes) an API key.

**Backup Management:**

*   `GET /admin/backups/{datasetId}`: Lists available backup files for a specific dataset.
*   `POST /admin/backups/restore/{datasetId}`: Initiates restoring a dataset from a specified backup file.

**Health & Orchestration:**

*   `GET /admin/health`: Retrieves a system health report including dataset status.
*   `GET /admin/instances/`: Retrieves a list of registered distributed app instances *(Only available if Orchestration is enabled)*.
*   `DELETE /admin/instances/{id:guid}`: Removes a registered distributed app instance *(Only available if Orchestration is enabled)*.

</details>
</details>


## Tests

RefWire includes a comprehensive suite of tests to ensure quality and reliability. To run the tests:

1. **Using the .NET CLI:**

   ```bash
   dotnet test
   ```

2. **Via Visual Studio/Test Explorer:**

   Open the solution in Visual Studio and run the tests using the integrated Test Explorer.

These tests cover core functionality, including API endpoints, caching, and persistence logic, ensuring that changes don’t introduce regressions.

---

## Contributing

Contributions to RefWire are highly welcomed! Whether you're fixing a bug, improving documentation, or introducing a new feature, we appreciate your help in making RefWire better for everyone.

For detailed guidelines on how to contribute, please refer to our [CONTRIBUTING.md](CONTRIBUTING.md) file. It covers everything from how to fork and branch the repository, coding standards, testing procedures, and how to submit your pull requests.

If you have any questions or need assistance, feel free to reach out via [GitHub Issues](https://github.com/coretravis/RefWire/issues) or join the discussion on [GitHub Discussions](https://github.com/coretravis/RefWire/discussions).

---

## Communication

- **GitHub Issues & Discussions:**  
  Use [GitHub Discussions](https://github.com/coretravis/RefWire/discussions) to ask questions, propose ideas, or get help.
- **Direct Contact:**  
  If you need further assistance, feel free to reach out via email at [info@coretravis.work](mailto:info@coretravis.work).

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.

---

## Contact

Please contact [info@coretravis.work](mailto:info@coretravis.work).

---
