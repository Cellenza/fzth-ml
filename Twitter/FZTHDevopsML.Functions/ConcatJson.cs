using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FZTHDevopsML.ConcatJSON.Functions
{
    public static class ConcatJson
    {
        /// <summary>
        /// Concatenate all the part JSON Lines files into one JSON file
        /// </summary>
        /// <param name="req"></param>
        /// <param name="container"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ConcatJson")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Blob("%BLOBSTORAGE_OUTPUT_CONTAINERNAME%", Connection = "BLOBSTORAGE_CNXSTRING")] CloudBlobContainer container,
            ILogger log)
        {
            log.LogInformation("Start Concat JSON files");
            var filePath = Path.GetTempFileName();
            var fileName = Environment.GetEnvironmentVariable("TRAIN_TWEETS_FILENAME");

            List<IListBlobItem> lstBlobs = await ListBlobs(container, log);
            CloudAppendBlob appBlob = container.GetAppendBlobReference(fileName);
            await appBlob.CreateOrReplaceAsync();

            // Add [ at the begining and ] at the end of the file
            await appBlob.AppendTextAsync("[");

            foreach (var item in lstBlobs)
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    var filename = ((CloudBlockBlob)item).Name;

                    if (filename.Contains("part-"))
                    {
                        var blobContent = (CloudBlockBlob)item;

                        // Add coma at the end of each line
                        await blobContent.DownloadToFileAsync(filePath, FileMode.Create);
                        var lines = File.ReadAllLines(filePath);
                        var newContent = string.Join(",\r\n", lines);

                        var blobText = blobContent.DownloadTextAsync();
                        await appBlob.AppendTextAsync(newContent);
                    }
                }
            }
            await appBlob.AppendTextAsync("]");

            log.LogInformation("End Concat JSON files");

            return (ActionResult)new OkResult();
        }

        /// <summary>
        /// List all blobls inside a container
        /// </summary>
        /// <param name="blobClient"></param>
        /// <param name="containerName"></param>
        /// <returns>List of blobs</returns>
        public static async Task<List<IListBlobItem>> ListBlobs(CloudBlobContainer blobContainer, ILogger log)
        {
            // Get Container reference
            List<IListBlobItem> lstBlobs = new List<IListBlobItem>();
            BlobContinuationToken continuationToken = null;

            // Get the list of blobs in the container and store them into a list
            do
            {
                try
                {
                    // Get list of blobs
                    BlobResultSegment resultSegment = await blobContainer.ListBlobsSegmentedAsync(continuationToken);

                    // Update continuation token
                    continuationToken = resultSegment.ContinuationToken;
                    // Add Blobs to the list
                    lstBlobs.AddRange(resultSegment.Results);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);
                }
            } while (continuationToken != null);

            return lstBlobs;
        }
    }
}
