using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage.Analytics;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;


namespace AzureCostManagement
{
    class Program
    {
        private static readonly string ARMResourceUrl = ConfigurationManager.AppSettings["ARMResourceUrl"];
        private static readonly string ADALServiceURL = ConfigurationManager.AppSettings["ADALServiceURL"];
        private static readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];// well-known client ID for Azure PowerShell
        private static readonly string RedirectUri = ConfigurationManager.AppSettings["RedirectUri"];// redirect URI for Azure PowerShell
        private static readonly string InactivityDaysForStorageAccount = ConfigurationManager.AppSettings["InactivityDaysForStorageAccount"];
        private static string TenantId = "";
        private static string SubscriptionId = "";
        
        public delegate Task delegates(Resource resource);
        static delegates[] lastUsageChecks = new delegates[]
        {
            new delegates(LastUsageCheckClass.Storage)
        };

        public static class LastUsageCheckClass
        {
            public static async Task Storage(Resource resource)
            {
                try
                {
                    // Intelligent to figure using timestamps if there was any activity
                    string resourceGroupName = resource.ResourceGroupName;
                    string accountName = resource.Name;

                    Console.WriteLine("Checking...");
                    Console.WriteLine("Resource Name:" + resource.Name);
                    Console.WriteLine("Resource Type:" + resource.Type);

                    string connectionString = await GetStorageAccountConnectionString(resourceGroupName, accountName);
                    CloudBlobClient blobClient = GetBlobClient(connectionString);
                    CloudTableClient tableClient = GetTableClient(connectionString);
                    GetAnalyticsLogs(blobClient, tableClient);
                    
                    List<CloudBlobContainer> containers = await ListContainersAsync(blobClient);
                    
                    if (containers.Count == 0)
                    {
                        Console.WriteLine("Information: The Storage Account does not have any Containers!");
                    }
                    foreach (CloudBlobContainer container in containers)
                    {
                        foreach (IListBlobItem blob in container.ListBlobs())
                        {
                            if (blob.GetType() == typeof(CloudPageBlob))
                            {
                                CloudPageBlob pageBlob = (CloudPageBlob)blob;
                                if (pageBlob.Name.Contains(".vhd"))
                                {
                                    if (pageBlob.Properties.LeaseStatus.ToString() == "Unlocked" && pageBlob.Properties.LeaseState.ToString() == "Available")
                                    {
                                        Console.WriteLine("Warning: Unused VHD is present in the storage account.");
                                    }
                                    
                                }
                            }

                        }
                    }
                    Console.WriteLine();
                }
                catch (Exception)
                {
                    Console.WriteLine();
                }
            }
        }

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("--- Demo started ---");
                Program p = new Program();
                List<Subscription> subscriptions = await p.GetSubscriptions();
                foreach (var subscription in subscriptions)
                {
                    TenantId = subscription.TenantId;
                    SubscriptionId = subscription.SubscriptionId;
                    Console.WriteLine("Checking Resources for Subscription: " + SubscriptionId);
                    foreach (var resourceGroup in subscription.ResourceGroups)
                    {
                        foreach (var resource in resourceGroup.Resources)
                        {
                            switch (resource.Type)
                            {
                                case "Microsoft.Storage/storageAccounts":
                                    await LastUsageCheckClass.Storage(resource);
                                    break;
                            }
                            // We will keep building checks for new service types 

                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        public async Task<List<Subscription>> GetSubscriptions()
        {
            try
            {
                List <Subscription> subscriptions = new List<Subscription>();
                var ctx = new AuthenticationContext(new Uri(ADALServiceURL) + "common");
                
                // This will show the login window
                var authParam = new PlatformParameters(PromptBehavior.Always);
                var mainAuthRes = await ctx.AcquireTokenAsync(ARMResourceUrl, ClientId, new Uri(RedirectUri), authParam);
                var subscriptionCredentials = new TokenCredentials(mainAuthRes.AccessToken);
                var cancelToken = new CancellationToken();
                using (var subscriptionClient = new SubscriptionClient(subscriptionCredentials))
                {
                    var tenants = subscriptionClient.Tenants.ListAsync(cancelToken).Result;
                    Console.WriteLine("Fetching Subscriptions...");
                    foreach (var tenantDescription in tenants)
                    {
                        var authParam1 = new PlatformParameters(PromptBehavior.Never);
                        var tenantCtx = new AuthenticationContext(new Uri(ADALServiceURL) + tenantDescription.TenantId);
                        // This will NOT show the login window
                        var tenantAuthRes = await tenantCtx.AcquireTokenAsync(
                            ARMResourceUrl,
                            ClientId,
                            new Uri(RedirectUri),
                            authParam1,
                            new UserIdentifier(mainAuthRes.UserInfo.DisplayableId, UserIdentifierType.RequiredDisplayableId));
                        
                        var tenantTokenCreds = new TokenCredentials(tenantAuthRes.AccessToken);
                        
                        using (var tenantSubscriptionClient = new SubscriptionClient(tenantTokenCreds))
                        {
                            var tenantSubscriptioins = tenantSubscriptionClient.Subscriptions.ListAsync(cancelToken).Result;
                            foreach (var sub in tenantSubscriptioins)
                            {
                                Subscription subs = new Subscription()
                                {
                                    DisplayName = sub.DisplayName,
                                    SubscriptionId = sub.SubscriptionId,
                                    TenantId = tenantDescription.TenantId,
                                    State = sub.DisplayName
                                };

                                var resourceClient = new ResourceManagementClient(tenantTokenCreds);
                                resourceClient.SubscriptionId = subs.SubscriptionId;

                                Console.WriteLine("Fetching Resource Groups...");
                                // Getting the resource groups
                                var groups = resourceClient.ResourceGroups.List().ToList();
                                List <ResourceGroup> rsGroups = new List<ResourceGroup>();
                                foreach (var rg in groups)
                                {
                                    ResourceGroup rg1 = new ResourceGroup()
                                    {
                                        Id = rg.Id,
                                        Name = rg.Name,
                                        Location = rg.Location
                                    };
                                    List<Resource> resources = new List<Resource>();
                                    foreach (var resource in await resourceClient.Resources.ListByResourceGroupAsync(rg.Name))
                                    {
                                        
                                        Resource rs1 = new Resource()
                                        {
                                            Id = resource.Id,
                                            Name = resource.Name,
                                            Location = resource.Location,
                                            Type = resource.Type,
                                            ResourceGroupName = rg.Name
                                        };
                                        resources.Add(rs1);
                                    }
                                    rg1.Resources = resources;
                                    rsGroups.Add(rg1);

                                }
                                subs.ResourceGroups = rsGroups;
                                subscriptions.Add(subs);
                            }
                        }
                    }
                }
                return subscriptions;

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        public static async Task<string> GetOAuthTokenFromAADAsync()
        {
            var authenticationContext = new AuthenticationContext(String.Format("{0}/{1}",
                                                                    ConfigurationManager.AppSettings["ADALServiceURL"],
                                                                    TenantId));

            //Ask the logged in user to authenticate, so that this client app can get a token on his behalf
            var authParam1 = new PlatformParameters(PromptBehavior.Never);
            var result = await authenticationContext.AcquireTokenAsync(String.Format("{0}/", ConfigurationManager.AppSettings["ARMBillingServiceURL"]),
                                                            ConfigurationManager.AppSettings["ClientID"],
                                                            new Uri(ConfigurationManager.AppSettings["ADALRedirectURL"]),
                                                            authParam1);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }

        public static async Task<String> GetStorageAccountConnectionString(string resourceGroupName, string accountName)
        {
            string token = await GetOAuthTokenFromAADAsync();
            var tenantTokenCreds = new TokenCredentials(token);
            StorageManagementClient storageMgmtClient = new StorageManagementClient(tenantTokenCreds) { SubscriptionId = SubscriptionId };
            IList<StorageAccountKey> accountKeys = storageMgmtClient.StorageAccounts.ListKeys(resourceGroupName, accountName).Keys;
            string connectionString = String.Format("DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2}", "https", accountName, accountKeys[0].Value);
            return connectionString;
        }

        public static CloudBlobClient GetBlobClient(string connectionString)
        {
            // Create a blob client that can authenticate with a connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient;
        }

        public static CloudTableClient GetTableClient(string connectionString)
        {
            // Create a client that can authenticate with a connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            return tableClient;
        }

        public static void GetAnalyticsLogs(CloudBlobClient blobClient, CloudTableClient tableClient)
        {
            try
            {
                DateTime time = DateTime.UtcNow;
                int inactivityDays = Int16.Parse(InactivityDaysForStorageAccount) * -1;
                CloudAnalyticsClient analyticsClient = new CloudAnalyticsClient(blobClient.StorageUri, tableClient.StorageUri, tableClient.Credentials);
                IEnumerable<ICloudBlob> results = analyticsClient.ListLogs(StorageService.Blob, time.AddDays(inactivityDays), null, LoggingOperations.All, BlobListingDetails.Metadata, null, null);
                List<ICloudBlob> logs = results.ToList();

                //Download the log files
                //foreach (var item in logs)
                //{
                //    string name = ((CloudBlockBlob)item).Name;
                //    CloudBlobContainer container = blobClient.GetContainerReference("$logs");
                //    CloudBlockBlob blockBlob = container.GetBlockBlobReference(name);

                //    //specify the directory without file name
                //    string sub_folder = name.Remove(name.LastIndexOf("/") + 1);
                //    string path = (@"C:/logs/" + sub_folder);

                //    //create the directory if it does not exist.
                //    Directory.CreateDirectory(path);

                //    //specify the file full path
                //    string file_path = (@"C:/logs/" + name);

                //    using (var fileStream = File.Create(file_path))
                //    {

                //        blockBlob.DownloadToStream(fileStream);
                //    }
                //}
                ServiceProperties serviceProperties = blobClient.GetServiceProperties();
                bool isLoggingDisabled = serviceProperties.Logging.LoggingOperations.ToString() == "None";
                if (isLoggingDisabled) {
                  Console.WriteLine("Information: Diagnostics Settings are enabled for this account but logging is still disabled. Please change the Diagnostics Settings ");
                } else
                {
                    if (logs.Count == 0)
                    {
                        Console.WriteLine("Information: Diagnostics Settings are enabled for this account. Either this storage account have not been used from last " + InactivityDaysForStorageAccount + " days or Logs are not available for this storage account.");
                    } else
                    {
                        //If we want to check the logs, we will do that here
                        Console.WriteLine("Lets check the logs now!");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Information: Please enable Diagnostics Settings to check the logs/activity of the storage account.");
            }

        }

        public static async Task<List<CloudBlobContainer>> ListContainersAsync(CloudBlobClient blobClient)
        {
            BlobContinuationToken continuationToken = null;
            List<CloudBlobContainer> containers = new List<CloudBlobContainer>();
            do
            {
                ContainerResultSegment response = await blobClient.ListContainersSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                containers.AddRange(response.Results);

            } while (continuationToken != null);
            return containers;
        }
    }
}
