//using ListServDB.Core.API;
//using ListServDB.Core.Concurrency;
//using ListServDB.Core.HA;
//using ListServDB.Core.Interfaces;
//using ListServDB.Core.Models;
//using ListServDB.Persistence.Azure.Blob;  // Uncomment to use Azure Blob persistence
//using ListServDB.Persistence.FileSystem;    // Uncomment to use file-based persistence for state
//using ListServDB.Security;
//// Assuming HealthEndpoint is defined in your project:

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

//        static async Task Main(string[] _)
//        {
//            Console.WriteLine("ListServ Sample Application (Async Thorough Test Version)");
//            Console.WriteLine("Demonstrating asynchronous multi-file persistence, lazy loading, and full API operations\n");

//            // Define base directory and directories for datasets and backups.
//            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
//            string datasetsDir = Path.Combine(baseDir, "TestDatasets");
//            string backupsDir = Path.Combine(baseDir, "TestBackups");

//            // Clean up existing directories.
//            if (Directory.Exists(datasetsDir))
//            {
//                Directory.Delete(datasetsDir, recursive: true);
//            }

//            if (Directory.Exists(backupsDir))
//            {
//                Directory.Delete(backupsDir, recursive: true);
//            }

//            Directory.CreateDirectory(datasetsDir);
//            Directory.CreateDirectory(backupsDir);

//            // Choose persistence implementation:
//            // For Azure Blob Storage persistence (make sure to supply a valid connection string):
//            string connectionString = @"UseDevelopmentStorage=true;";
//            IDatasetPersistenceManager multiFilePersistenceManager = new AzureBlobPersistenceManager(connectionString);
//            // Alternatively, to use local file-based persistence, comment the above line and uncomment below:
//            // IDatasetPersistenceManager multiFilePersistenceManager = new MultiFilePersistenceManager(datasetsDir, backupsDir);

//            // Create concurrency manager, authenticator, and audit logger.
//            IConcurrencyManager concurrencyManager = new ConcurrencyManager();
//            IRoleBasedAuthenticator authenticator = new RoleBasedAuthenticator();
//        //   var auditLogger = new AuditLogger(new ConsoleLogger());

//            // Instantiate the API.
//            using var listServ = new ListServApi(multiFilePersistenceManager, concurrencyManager, authenticator, null);

//            // ******************************
//            // Test Dataset: Countries
//            // ******************************
//            string countriesDatasetId = "countries";
//            Dataset countriesDataset = await GetOrCreateDatasetAsync(
//                listServ, countriesDatasetId, "Countries",
//                new List<string> { "ISO", "Name", "Continent" },
//                AdminUsername, AdminPassword);

//            // Sample countries.
//            var sampleCountries = new List<(string Iso, string Name, string Continent)>
//            {
//                ("US", "United States", "North America"),
//                ("CA", "Canada", "North America"),
//                ("GB", "United Kingdom", "Europe"),
//                ("FR", "France", "Europe"),
//                ("DE", "Germany", "Europe"),
//                ("JP", "Japan", "Asia")
//            };

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
//                    await listServ.AddItemAsync(countriesDatasetId, countryItem, AdminUsername, AdminPassword);
//                    Console.WriteLine($"[Countries] Added: {Name} ({Iso})");
//                }
//            }

//            // List and search in Countries.
//            var countryItems = await listServ.ListItemsAsync(countriesDatasetId, 0, 20);
//            Console.WriteLine("\n[Countries] Current Items:");
//            foreach (var item in countryItems)
//            {
//                Console.WriteLine($"  {item.Id} - {item.Name}");
//            }

//            var searchCountries = await listServ.SearchItemsAsync(countriesDatasetId, "United", 0, 20);
//            Console.WriteLine("\n[Countries] Search for 'United':");
//            foreach (var item in searchCountries)
//            {
//                Console.WriteLine($"  Found: {item.Id} - {item.Name}");
//            }

//            // Update and archive in Countries.
//            if (countriesDataset.Items.TryGetValue("GB", out var gbItem))
//            {
//                gbItem.Name = "UK";
//                gbItem.Data["Name"] = "UK";
//                await listServ.UpdateItemAsync(countriesDatasetId, gbItem, AdminUsername, AdminPassword);
//                Console.WriteLine("\n[Countries] Updated 'GB' to 'UK'.");
//            }
//            await listServ.ArchiveItemAsync(countriesDatasetId, "CA", AdminUsername, AdminPassword);
//            Console.WriteLine("[Countries] Archived 'CA'.");

//            // ******************************
//            // Test Dataset: Cities
//            // ******************************
//            string citiesDatasetId = "cities";
//            Dataset citiesDataset = await GetOrCreateDatasetAsync(
//                listServ, citiesDatasetId, "Cities",
//                new List<string> { "CityId", "CityName", "Country" },
//                AdminUsername, AdminPassword);

//            var sampleCities = new List<(string CityId, string CityName, string Country)>
//            {
//                ("NYC", "New York", "United States"),
//                ("LDN", "London", "United Kingdom"),
//                ("PAR", "Paris", "France"),
//                ("TKY", "Tokyo", "Japan")
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
//                    await listServ.AddItemAsync(citiesDatasetId, cityItem, AdminUsername, AdminPassword);
//                    Console.WriteLine($"[Cities] Added: {CityName} ({CityId})");
//                }
//            }

//            // Try adding a duplicate city to test error handling.
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
//                await listServ.AddItemAsync(citiesDatasetId, duplicateCity, AdminUsername, AdminPassword);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"\n[Cities] Expected duplicate error: {ex.Message}");
//            }

//            // Update city in Cities.
//            if (citiesDataset.Items.TryGetValue("LDN", out var ldnItem))
//            {
//                ldnItem.Name = "Greater London";
//                ldnItem.Data["CityName"] = "Greater London";
//                await listServ.UpdateItemAsync(citiesDatasetId, ldnItem, AdminUsername, AdminPassword);
//                Console.WriteLine("\n[Cities] Updated 'LDN' to 'Greater London'.");
//            }

//            var cityItems = await listServ.ListItemsAsync(citiesDatasetId, 0, 20);
//            Console.WriteLine("\n[Cities] Current Items:");
//            foreach (var item in cityItems)
//            {
//                Console.WriteLine($"  {item.Id} - {item.Name}");
//            }

//            // ******************************
//            // Test Dataset: Languages
//            // ******************************
//            string languagesDatasetId = "languages";
//            Dataset languagesDataset = await GetOrCreateDatasetAsync(
//                listServ, languagesDatasetId, "Languages",
//                new List<string> { "Code", "Language", "Family" },
//                AdminUsername, AdminPassword);

//            var sampleLanguages = new List<(string Code, string Language, string Family)>
//            {
//                ("EN", "English", "Germanic"),
//                ("FR", "French", "Romance"),
//                ("ES", "Spanish", "Romance"),
//                ("ZH", "Chinese", "Sino-Tibetan")
//            };

//            foreach (var (Code, Language, Family) in sampleLanguages)
//            {
//                if (!languagesDataset.Items.ContainsKey(Code))
//                {
//                    var langItem = new DatasetItem
//                    {
//                        Id = Code,
//                        Name = Language,
//                        Data = new Dictionary<string, object>
//                        {
//                            { "Code", Code },
//                            { "Language", Language },
//                            { "Family", Family }
//                        }
//                    };
//                    await listServ.AddItemAsync(languagesDatasetId, langItem, AdminUsername, AdminPassword);
//                    Console.WriteLine($"[Languages] Added: {Language} ({Code})");
//                }
//            }

//            // List languages and search.
//            var languageItems = await listServ.ListItemsAsync(languagesDatasetId, 0, 20);
//            Console.WriteLine("\n[Languages] Current Items:");
//            foreach (var item in languageItems)
//            {
//                Console.WriteLine($"  {item.Id} - {item.Name}");
//            }

//            var searchLanguages = await listServ.SearchItemsAsync(languagesDatasetId, "French", 0, 20);
//            Console.WriteLine("\n[Languages] Search for 'French':");
//            foreach (var item in searchLanguages)
//            {
//                Console.WriteLine($"  Found: {item.Id} - {item.Name}");
//            }

//            // ******************************
//            // Unauthorized Operation Test
//            // ******************************
//            try
//            {
//                if (countriesDataset.Items.TryGetValue("US", out var usItem))
//                {
//                    usItem.Name = "USA";
//                    usItem.Data["Name"] = "USA";
//                    await listServ.UpdateItemAsync(countriesDatasetId, usItem, NonAdminUsername, NonAdminPassword);
//                }
//            }
//            catch (UnauthorizedAccessException ex)
//            {
//                Console.WriteLine($"\n[Unauthorized Test] Caught expected unauthorized error: {ex.Message}");
//            }

//            // ******************************
//            // List all cached datasets
//            // ******************************
//            var allDatasets = await listServ.GetAllDatasetsAsync();
//            Console.WriteLine("\nAll Available Datasets:");
//            foreach (var ds in allDatasets)
//            {
//                Console.WriteLine($"  {ds.Id} - {ds.Name}");
//            }

//            // ******************************
//            // Save Overall State (Using File-based persistence for state backup)
//            // ******************************
//            string stateFilePath = Path.Combine(baseDir, "listserv_state.json");
//            var filePersistenceManager = new FilePersistenceManager(stateFilePath);
//            var state = await listServ.GetStateAsync();
//           // filePersistenceManager.SaveState(state);
//            Console.WriteLine($"\nState saved to disk at '{stateFilePath}' (backup created if applicable).");

//            //var stateBackups = await filePersistenceManager.ListBackupsAsync(da);
//            //Console.WriteLine("\nAvailable Backups for State:");
//            //foreach (var backup in stateBackups)
//            //{
//            //    Console.WriteLine($"  {backup}");
//            //}

//            // ******************************
//            // Leader Election Test
//            // ******************************
//            string leaderLockFile = Path.Combine(baseDir, "leader.lock");
//            using (var leaderElection = new LeaderElectionManager(leaderLockFile))
//            {
//                bool isLeader = leaderElection.TryBecomeLeader();
//                Console.WriteLine(isLeader
//                    ? "\nThis instance has acquired leadership."
//                    : "\nThis instance is not the leader.");
//            }

//            // ******************************
//            // Health Report Test
//            // ******************************
//            var healthEndpoint = new HealthEndpoint(state.Datasets ?? new Dictionary<string, Dataset>());
//            string healthReport = healthEndpoint.GetHealthReport();
//            Console.WriteLine("\nHealth Report:");
//            Console.WriteLine(healthReport);

//            Console.WriteLine("\nExtended async sample app execution complete. Press any key to exit.");
//            Console.ReadKey();
//        }

//        private static async Task<Dataset> GetOrCreateDatasetAsync(ListServApi api, string id, string name, List<string> fields, string adminUser, string adminPass)
//        {
//            try
//            {
//                return await api.GetDatasetByIdAsync(id);
//            }
//            catch (KeyNotFoundException)
//            {
//                return await api.CreateDatasetAsync(id, name, fields[0], "Name", fields, adminUser, adminPass);
//            }
//        }
//    }
//}
