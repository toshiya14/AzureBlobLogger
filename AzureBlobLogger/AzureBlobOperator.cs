using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobLogger
{
    internal class AzureBlobOperator
    {
        private const int RetryCounts = 3;

        CloudStorageAccount account;
        CloudBlobClient client;
        /// <summary>
        /// Initialize a new instance of the <see cref="BlobOperator"/> class.
        /// </summary>
        /// <param name="constr">connection string.</param>
        public AzureBlobOperator(string constr)
        {
            account = CloudStorageAccount.Parse(constr);
            client = account.CreateCloudBlobClient();
        }

        /// <summary>
        /// Upload a blob file to the container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="data">The file data.</param>
        /// <returns></returns>
        internal async Task Upload(string containerName, string blobName, byte[] data)
        {
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);
            await blockBlob.UploadFromByteArrayAsync(data, 0, data.Count());
        }

        /// <summary>
        /// Download Blob Block from the container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <exception cref="Exception">Data do not exists.</exception>
        /// <returns>The file bytes.</returns>
        internal async Task<byte[]> Download(string containerName, string blobName)
        {
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);
            if (!await blockBlob.ExistsAsync())
            {
                throw new Exception("data do not exists.");
            }
            var memStream = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(memStream);
            return memStream.ToArray();
        }

        /// <summary>
        /// Rename the blob.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        internal async Task Rename(string containerName, string blobName, string newName)
        {
            var container = client.GetContainerReference(containerName);
            var blobNew = container.GetBlockBlobReference(newName);
            if (!await blobNew.ExistsAsync())
            {
                var blobOld = container.GetBlockBlobReference(blobName);
                if (await blobOld.ExistsAsync())
                {
                    await blobNew.StartCopyAsync(blobOld);
                    await blobOld.DeleteIfExistsAsync();
                }
            }
        }

        /// <summary>
        /// Append content to the blob.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <param name="content">The contents in bytes.</param>
        /// <returns></returns>
        internal async Task Append(string containerName, string blobName, byte[] content)
        {
            var container = client.GetContainerReference(containerName);
            var blob = container.GetAppendBlobReference(blobName);
            if (!blob.Exists())
            {
                await blob.CreateOrReplaceAsync();
            }

            var retryTime = 0;
            var flag = false;
            while (!flag)
            {
                var leaseId = blob.AcquireLease(null, null);
                var accCondition = AccessCondition.GenerateLeaseCondition(leaseId);
                try
                {
                    await blob.AppendFromByteArrayAsync(content, 0, content.Length, accCondition, null, null);
                    flag = true;
                    blob.ReleaseLease(accCondition);
                }
                catch (StorageException e)
                {
                    var response = e.RequestInformation.HttpStatusCode;
                    if (response == (int)HttpStatusCode.Conflict)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    else
                    {
                        if (retryTime >= RetryCounts)
                        {
                            blob.ReleaseLease(accCondition);
                            flag = true;
                            throw e;
                        }
                        retryTime++;
                    }
                }
                catch (Exception ex)
                {
                    if (retryTime >= RetryCounts)
                    {
                        blob.ReleaseLease(accCondition);
                        flag = true;
                        throw ex;
                    }
                    retryTime++;
                }
            }
        }
    }
}
