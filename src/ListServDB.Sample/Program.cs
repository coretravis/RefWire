//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Text.Json;
//using System.Threading.Tasks;
//using System.Linq;

//class Program
//{
//    private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("https://localhost:7194") };

//    // Admin credentials
//    private const string AdminUsername = "admin";
//    private const string AdminPassword = "adminpassword";

//    static async Task Main()
//    {
//        Console.WriteLine("ListServ API Client");
//        Console.WriteLine("Using Endpoints for Dataset & Item Management\n");

//        // Load or create dataset
//        string datasetId = "countries";
//        Console.WriteLine($"Checking dataset '{datasetId}'...");
//        if (!await DatasetExists(datasetId))
//        {
//            Console.WriteLine($"Dataset '{datasetId}' not found. Creating...");
//            await CreateDataset(datasetId);
//        }
//        else
//        {
//            Console.WriteLine($"Dataset '{datasetId}' found and loaded.");
//        }

//        // Define sample country data
//        var sampleCountries = new[]
//        {
//            new { Id = "US", Name = "United States", Continent = "North America" },
//            new { Id = "CA", Name = "Canada", Continent = "North America" },
//            new { Id = "GB", Name = "United Kingdom", Continent = "Europe" },
//            new { Id = "FR", Name = "France", Continent = "Europe" },
//            new { Id = "DE", Name = "Germany", Continent = "Europe" },
//            new { Id = "JP", Name = "Japan", Continent = "Asia" },
//            new { Id = "IN", Name = "India", Continent = "Asia" },
//            new { Id = "CN", Name = "China", Continent = "Asia" },
//            new { Id = "BR", Name = "Brazil", Continent = "South America" },
//            new { Id = "AU", Name = "Australia", Continent = "Oceania" },
//            new { Id = "ZA", Name = "South Africa", Continent = "Africa" },
//            new { Id = "RU", Name = "Russia", Continent = "Europe/Asia" }
//        };

//        // Add sample countries to dataset
//        Console.WriteLine("\nAdding sample countries to dataset...");
//        foreach (var country in sampleCountries)
//        {
//            if (!await ItemExists(datasetId, country.Id))
//            {
//                await AddItem(datasetId, country.Id, country);
//                Console.WriteLine($"Added: {country.Name} ({country.Id})");
//            }
//            else
//            {
//                Console.WriteLine($"Item already exists: {country.Name} ({country.Id})");
//            }
//        }

//        // List current items
//        Console.WriteLine("\nListing current items:");
//        await ListItems(datasetId);

//        // Search for countries containing "United"
//        Console.WriteLine("\nSearching for 'United'...");
//        await SearchItems(datasetId, "United");

//        // Update United Kingdom to UK
//        Console.WriteLine("\nUpdating 'GB' (United Kingdom) to 'UK'...");
//        await UpdateItem(datasetId, "GB", new { Name = "UK", Continent = "Europe" });

//        // Archive Canada
//        Console.WriteLine("\nArchiving item 'CA' (Canada)...");
//        await ArchiveItem(datasetId, "CA");

//        // List items after updates
//        Console.WriteLine("\nItems after updates:");
//        await ListItems(datasetId);

//        // List available backups
//        Console.WriteLine("\nListing available backups:");
//        await ListBackups();

//        // Simulate leader election
//        Console.WriteLine("\nChecking leader election...");
//        await CheckLeaderElection();

//        // Fetch health report
//        Console.WriteLine("\nFetching health report:");
//        await GetHealthReport();

//        Console.WriteLine("\nAll operations completed. Press any key to exit.");
//        Console.ReadKey();
//    }

//    private static async Task<bool> DatasetExists(string datasetId)
//    {
//        var response = await client.GetAsync($"/datasets/{datasetId}");
//        return response.IsSuccessStatusCode;
//    }

//    private static async Task CreateDataset(string datasetId)
//    {
//        var createDatasetRequest = new
//        {
//            Id = datasetId,
//            Name = "Countries",
//            IdField = "Id",
//            NameField = "Name",
//            Fields = new[] { "Id", "Name", "Continent" },
//            Username = AdminUsername,
//            Password = AdminPassword
//        };

//        var response = await client.PostAsJsonAsync("/datasets", createDatasetRequest);
//        if (response.IsSuccessStatusCode)
//        {
//            Console.WriteLine($"Dataset '{datasetId}' created successfully!");
//        }
//        else
//        {
//            Console.WriteLine($"Failed to create dataset: {await response.Content.ReadAsStringAsync()}");
//        }
//    }

//    // Checks if an item exists by listing all items and filtering by Id.
//    private static async Task<bool> ItemExists(string datasetId, string itemId)
//    {
//        var response = await client.GetAsync($"/datasets/{datasetId}/items?skip=0&take=1000");
//        if (!response.IsSuccessStatusCode)
//            return false;

//        var content = await response.Content.ReadAsStringAsync();
//        var items = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
//        foreach (var _ in from item in items
//                          where item.TryGetProperty("id", out var idProp) && idProp.GetString() == itemId
//                          select new { })
//        {
//            return true;
//        }

//        return false;
//    }

//    private static async Task AddItem(string datasetId, string itemId, object itemData)
//    {
//        var addItemRequest = new
//        {
//            Id = itemId,
//            Data = itemData,
//            Username = AdminUsername,
//            Password = AdminPassword
//        };

//        var response = await client.PostAsJsonAsync($"/datasets/{datasetId}/items", addItemRequest);
//        if (!response.IsSuccessStatusCode)
//        {
//            Console.WriteLine($"Failed to add item {itemId}: {await response.Content.ReadAsStringAsync()}");
//        }
//    }

//    private static async Task ListItems(string datasetId)
//    {
//        var response = await client.GetAsync($"/datasets/{datasetId}/items?skip=0&take=1000");
//        if (response.IsSuccessStatusCode)
//        {
//            Console.WriteLine(await response.Content.ReadAsStringAsync());
//        }
//        else
//        {
//            Console.WriteLine("Failed to list items.");
//        }
//    }

//    private static async Task SearchItems(string datasetId, string searchTerm)
//    {
//        var response = await client.GetAsync($"/datasets/{datasetId}/items/search?searchTerm={Uri.EscapeDataString(searchTerm)}&skip=0&take=1000");
//        if (response.IsSuccessStatusCode)
//        {
//            Console.WriteLine(await response.Content.ReadAsStringAsync());
//        }
//        else
//        {
//            Console.WriteLine("Failed to search items.");
//        }
//    }

//    private static async Task UpdateItem(string datasetId, string itemId, object updatedData)
//    {
//        // Construct update request. Note that the endpoint expects a Name property separately.
//        var updateItemRequest = new
//        {
//            Name = updatedData.GetType().GetProperty("Name")?.GetValue(updatedData, null),
//            Data = updatedData,
//            Username = AdminUsername,
//            Password = AdminPassword
//        };

//        var response = await client.PutAsJsonAsync($"/datasets/{datasetId}/items/{itemId}", updateItemRequest);
//        if (!response.IsSuccessStatusCode)
//        {
//            Console.WriteLine($"Failed to update item {itemId}: {await response.Content.ReadAsStringAsync()}");
//        }
//    }

//    private static async Task ArchiveItem(string datasetId, string itemId)
//    {
//        var archiveRequest = new
//        {
//            Username = AdminUsername,
//            Password = AdminPassword
//        };

//        // DELETE endpoints do not accept a body via DeleteAsync, so we create a custom request.
//        var request = new HttpRequestMessage
//        {
//            Method = HttpMethod.Delete,
//            RequestUri = new Uri(client.BaseAddress!, $"/datasets/{datasetId}/items/{itemId}"),
//            Content = JsonContent.Create(archiveRequest)
//        };

//        var response = await client.SendAsync(request);
//        if (!response.IsSuccessStatusCode)
//        {
//            Console.WriteLine($"Failed to archive item {itemId}: {await response.Content.ReadAsStringAsync()}");
//        }
//    }

//    private static async Task ListBackups()
//    {
//        var response = await client.GetAsync("/backups");
//        if (response.IsSuccessStatusCode)
//        {
//            Console.WriteLine(await response.Content.ReadAsStringAsync());
//        }
//        else
//        {
//            Console.WriteLine("Failed to list backups.");
//        }
//    }

//    private static async Task CheckLeaderElection()
//    {
//        var response = await client.GetAsync("/leader");
//        if (response.IsSuccessStatusCode)
//        {
//            Console.WriteLine(await response.Content.ReadAsStringAsync());
//        }
//        else
//        {
//            Console.WriteLine("Failed to check leader status.");
//        }
//    }

//    private static async Task GetHealthReport()
//    {
//        var response = await client.GetAsync("/health");
//        if (response.IsSuccessStatusCode)
//        {
//            Console.WriteLine(await response.Content.ReadAsStringAsync());
//        }
//        else
//        {
//            Console.WriteLine("Failed to fetch health report.");
//        }
//    }
//}
