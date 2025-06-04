//using ListServDB.Core.API;
//using ListServDB.Core.Concurrency;
//using ListServDB.Core.HA;
//using ListServDB.Core.Interfaces;
//using ListServDB.Core.Models;
//using ListServDB.Logging;
//using ListServDB.Persistence.FileSystem;
//using ListServDB.Security;

//namespace ListServDB.Sample
//{
//    class Program
//    {
//        // Sample admin credentials (as defined in our RoleBasedAuthenticator)
//        private const string AdminUsername = "admin";
//        private const string AdminPassword = "adminpassword";
//        // Sample non-admin credentials to simulate unauthorized access.
//        private const string NonAdminUsername = "user";
//        private const string NonAdminPassword = "userpassword";

//        static void Main(string[] args)
//        {
//            Console.WriteLine("ListServ Sample Application (Extended Version)");
//            Console.WriteLine("Enhanced with Additional Operations Demonstrating More API Features\n");

//            // Initialize persistence manager (this will be used for auto-loading state)
//            string stateFilePath = "listserv_state.json";
//            var persistenceManager = new FilePersistenceManager(stateFilePath);

//            // Initialize Role-Based Authenticator and Audit Logger.
//            IRoleBasedAuthenticator authenticator = new RoleBasedAuthenticator();
//            var auditLogger = new AuditLogger(new ConsoleLogger());
//            var concurrencyManager = new ConcurrencyManager();
//            // Instantiate ListServApi with persistence manager, so it auto-loads state if available.
//            using var listServ = new ListServApi(persistenceManager, concurrencyManager, authenticator, auditLogger);

//            // --- Existing Dataset: Countries ---
//            string countriesDatasetId = "countries";
//            Dataset countriesDataset = listServ.GetDatasetById(countriesDatasetId);
//            if (countriesDataset == null)
//            {
//                // Create a new dataset if it does not exist.
//                var fields = new List<string> { "ISO", "Name", "Continent" };
//                countriesDataset = listServ.CreateDataset(countriesDatasetId, "Countries", "ISO", "Name", fields, AdminUsername, AdminPassword);
//                Console.WriteLine($"New dataset created: {countriesDataset.Name}");
//            }
//            else
//            {
//                Console.WriteLine($"Loaded existing dataset: {countriesDataset.Name}");
//            }

//            // Define sample countries.
//            var sampleCountries = new List<(string Iso, string Name, string Continent)>
//            {
//                ("US", "United States", "North America"),
//                ("CA", "Canada", "North America"),
//                ("GB", "United Kingdom", "Europe"),
//                ("FR", "France", "Europe"),
//                ("DE", "Germany", "Europe"),
//                ("JP", "Japan", "Asia"),
//                ("IN", "India", "Asia"),
//                ("CN", "China", "Asia"),
//                ("BR", "Brazil", "South America"),
//                ("AU", "Australia", "Oceania"),
//                ("ZA", "South Africa", "Africa"),
//                ("RU", "Russia", "Europe/Asia")
//            };

//            // Add sample countries if not already present.
//            foreach (var (Iso, Name, Continent) in sampleCountries)
//            {
//                if (!countriesDataset.Items.ContainsKey(Iso))
//                {
//                    var countryItem = new DatasetItem
//                    {
//                        Id = Iso,
//                        Name = Name,
//                        Data = new Dictionary<string, object>
//                        {
//                            { "ISO", Iso },
//                            { "Name", Name },
//                            { "Continent", Continent }
//                        }
//                    };
//                    listServ.AddItem(countriesDatasetId, countryItem, AdminUsername, AdminPassword);
//                    Console.WriteLine($"Added country: {Name} ({Iso})");
//                }
//            }

//            // List current countries.
//            var countryItems = listServ.ListItems(countriesDatasetId, 0, 20);
//            Console.WriteLine("\nCurrent Countries in Dataset:");
//            foreach (var item in countryItems)
//            {
//                Console.WriteLine($"Id: {item.Id}, Name: {item.Name}");
//            }

//            // Search for countries containing "United".
//            var searchResults = listServ.SearchItems(countriesDatasetId, "United", 0, 20);
//            Console.WriteLine("\nSearch Results for 'United':");
//            foreach (var item in searchResults)
//            {
//                Console.WriteLine($"Found Country - Id: {item.Id}, Name: {item.Name}");
//            }

//            // Update an item: change "United Kingdom" to "UK".
//            if (countriesDataset.Items.TryGetValue("GB", out var gbItem))
//            {
//                gbItem.Name = "UK";
//                gbItem.Data["Name"] = "UK";
//                listServ.UpdateItem(countriesDatasetId, gbItem, AdminUsername, AdminPassword);
//                Console.WriteLine("\nUpdated item 'GB' to 'UK'.");
//            }

//            // Archive an item: Archive Canada.
//            listServ.ArchiveItem(countriesDatasetId, "CA", AdminUsername, AdminPassword);
//            Console.WriteLine("\nArchived item 'CA'.");

//            // --- New Dataset: Cities ---
//            string citiesDatasetId = "cities";
//            Dataset citiesDataset = listServ.GetDatasetById(citiesDatasetId);
//            if (citiesDataset == null)
//            {
//                // Create a new dataset for cities.
//                var fields = new List<string> { "CityId", "CityName", "Country" };
//                citiesDataset = listServ.CreateDataset(citiesDatasetId, "Cities", "CityId", "CityName", fields, AdminUsername, AdminPassword);
//                Console.WriteLine($"\nNew dataset created: {citiesDataset.Name}");
//            }

//            // Add sample cities.
//            var sampleCities = new List<(string CityId, string CityName, string Country)>
//            {
//                ("NYC", "New York", "United States"),
//                ("LDN", "London", "United Kingdom"),
//                ("PAR", "Paris", "France"),
//                ("TKY", "Tokyo", "Japan"),
//                ("DEL", "Delhi", "India")
//            };

//            foreach (var (CityId, CityName, Country) in sampleCities)
//            {
//                if (!citiesDataset.Items.ContainsKey(CityId))
//                {
//                    var cityItem = new DatasetItem
//                    {
//                        Id = CityId,
//                        Name = CityName,
//                        Data = new Dictionary<string, object>
//                        {
//                            { "CityId", CityId },
//                            { "CityName", CityName },
//                            { "Country", Country }
//                        }
//                    };

//                    try
//                    {
//                        listServ.AddItem(citiesDatasetId, cityItem, AdminUsername, AdminPassword);
//                        Console.WriteLine($"Added city: {CityName} ({CityId})");
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"Error adding city '{CityName}': {ex.Message}");
//                    }
//                }
//            }

//            // Attempt to add a duplicate city item to trigger an exception.
//            try
//            {
//                var duplicateCity = new DatasetItem
//                {
//                    Id = "NYC",
//                    Name = "New York Duplicate",
//                    Data = new Dictionary<string, object>
//                    {
//                        { "CityId", "NYC" },
//                        { "CityName", "New York Duplicate" },
//                        { "Country", "United States" }
//                    }
//                };
//                listServ.AddItem(citiesDatasetId, duplicateCity, AdminUsername, AdminPassword);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"\nExpected error for duplicate item: {ex.Message}");
//            }

//            // Update an item in cities: change "London" to "Greater London".
//            if (citiesDataset.Items.TryGetValue("LDN", out var ldnItem))
//            {
//                ldnItem.Name = "Greater London";
//                ldnItem.Data["CityName"] = "Greater London";
//                listServ.UpdateItem(citiesDatasetId, ldnItem, AdminUsername, AdminPassword);
//                Console.WriteLine("\nUpdated city 'LDN' to 'Greater London'.");
//            }

//            // List current cities.
//            var cityItems = listServ.ListItems(citiesDatasetId, 0, 20);
//            Console.WriteLine("\nCurrent Cities in Dataset:");
//            foreach (var item in cityItems)
//            {
//                Console.WriteLine($"Id: {item.Id}, City Name: {item.Name}");
//            }

//            // Update dataset metadata: change dataset name for cities.
//            var updatedCitiesDataset = new Dataset
//            {
//                Id = citiesDataset.Id,
//                Name = "Urban Centers", // updated name
//                IdField = citiesDataset.IdField,
//                NameField = citiesDataset.NameField,
//                Fields = new List<string>(citiesDataset.Fields)
//            };
//            listServ.UpdateDataset(updatedCitiesDataset, AdminUsername, AdminPassword);
//            Console.WriteLine("\nUpdated dataset metadata for 'cities' to 'Urban Centers'.");

//            // Demonstrate unauthorized operation by attempting to update an item with non-admin credentials.
//            try
//            {
//                if (countriesDataset.Items.TryGetValue("US", out var usItem))
//                {
//                    usItem.Name = "USA";
//                    usItem.Data["Name"] = "USA";
//                    listServ.UpdateItem(countriesDatasetId, usItem, NonAdminUsername, NonAdminPassword);
//                }
//            }
//            catch (UnauthorizedAccessException ex)
//            {
//                Console.WriteLine($"\nUnauthorized operation caught: {ex.Message}");
//            }

//            // List all datasets managed by the API.
//            var allDatasets = listServ.GetAllDatasets();
//            Console.WriteLine("\nAll Available Datasets:");
//            foreach (var ds in allDatasets)
//            {
//                Console.WriteLine($"Dataset Id: {ds.Id}, Name: {ds.Name}");
//            }

//            // Save the current state.
//            var state = listServ.GetState();
//            persistenceManager.SaveState(state);
//            Console.WriteLine($"\nState saved to disk at '{stateFilePath}' (backup created if applicable).");

//            // List available backups.
//            var backups = persistenceManager.ListBackups();
//            Console.WriteLine("\nAvailable Backups:");
//            foreach (var backup in backups)
//            {
//                Console.WriteLine(backup);
//            }

//            // Demonstrate Leader Election.
//            string leaderLockFile = "leader.lock";
//            using (var leaderElection = new LeaderElectionManager(leaderLockFile))
//            {
//                bool isLeader = leaderElection.TryBecomeLeader();
//                Console.WriteLine(isLeader
//                    ? "\nThis instance has acquired leadership."
//                    : "\nThis instance is not the leader.");
//            }

//            // Generate and display a health report.
//            var healthEndpoint = new HealthEndpoint(state.Datasets);
//            string healthReport = healthEndpoint.GetHealthReport();
//            Console.WriteLine("\nHealth Report:");
//            Console.WriteLine(healthReport);

//            Console.WriteLine("\nExtended sample app execution complete. Press any key to exit.");
//            Console.ReadKey();
//        }
//    }
//}
