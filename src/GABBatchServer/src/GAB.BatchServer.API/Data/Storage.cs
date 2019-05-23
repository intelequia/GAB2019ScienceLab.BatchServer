using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GAB.BatchServer.API.Data
{
    /// <summary>
    /// Utility class for Azure Storage management
    /// </summary>
    public static class Storage
    {
        /// <summary>
        /// Inputs container
        /// </summary>
        public static CloudBlobContainer InputsContainer { get; set; }
        /// <summary>
        /// Ouputs container
        /// </summary>
        public static CloudBlobContainer OutputsContainer { get; set; }

        /// <summary>
        /// Initializes the storage account creating the containers
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="seedData"></param>
        public static async void Initialize(IConfiguration configuration, ILogger logger, bool seedData)
        {
            logger.LogInformation("Initializing Azure storage");
            var account = CloudStorageAccount.Parse(configuration.GetConnectionString("Storage"));
            var client = account.CreateCloudBlobClient();
            
            var inputsContainerName = configuration["BatchServer:InputsContainerName"];
            logger.LogInformation($"Creating inputs container '{inputsContainerName}'");
            InputsContainer = client.GetContainerReference(inputsContainerName);
            await InputsContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null)
                .ConfigureAwait(continueOnCapturedContext: false);

            var outputsContainerName = configuration["BatchServer:OutputsContainerName"];
            logger.LogInformation($"Creating inputs container '{outputsContainerName}'");
            OutputsContainer = client.GetContainerReference(outputsContainerName);
            await OutputsContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (seedData)
            {
                // Look for any input
                var blobs = await InputsContainer.ListBlobsSegmentedAsync(null)
                    .ConfigureAwait(continueOnCapturedContext: false);
                if (blobs.Results.Any())
                {
                    logger.LogInformation("Database already initialized. Skipping...");
                    return; // The database has been seeded
                }
                for (var i = 1; i < 10000; i++)
                {
                    var blob =  InputsContainer.GetBlockBlobReference($"input{i}.txt");
                    await blob.UploadTextAsync("Hello world")
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }

            logger.LogInformation($"Storage successfully initialized");
        }

        /// <summary>
        /// Uploads a output result to the storage account
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="blob"></param>
        /// <param name="filename"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static async Task UploadOutputAsync(IConfiguration configuration, CloudBlockBlob blob, string filename, string contentType = "application/json")
        {
            var options = new BlobRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 10)
            };
            ((CloudBlob) blob).Properties.ContentType = contentType;
            await blob.UploadFromFileAsync(filename, null, options, null);
        }
    }
}
