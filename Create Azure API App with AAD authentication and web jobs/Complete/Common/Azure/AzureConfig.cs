using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ikc5.TypeLibrary.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;

namespace Azure
{
	public static class AzureConfig
	{
		private static readonly ILogger Logger = new ConsoleLogger();

		public const string ImageBlobName = "images";
		public const string ThumbnailQueueName = "thumbnailrequest";

		public const string ThumbnailPoisonQueueName = "thumbnailrequest-poison";

		private static string StorageConnectionString =>
			ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString();


		#region Get azure objects

		public static void InitializeStorage()
		{
			// Open storage account using credentials from .cscfg file.
			var storageAccount = CloudStorageAccount.Parse(StorageConnectionString);

			Logger.Log($"Creating {ImageBlobName} blob container...");
			// Get context object for working with blobs, and 
			// set a default retry policy appropriate for a web user interface.
			var blobClient = storageAccount.CreateCloudBlobClient();
			blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

			var imagesBlobContainer = blobClient.GetContainerReference(ImageBlobName);
			if (imagesBlobContainer.CreateIfNotExists())
			{
				// Enable public access on the newly created "articles" container.
				imagesBlobContainer.SetPermissions(
					new BlobContainerPermissions
					{
						PublicAccess = BlobContainerPublicAccessType.Blob
					});
			}
			Logger.Log($"Create {ImageBlobName} blob container");

			Logger.Log("Creating queues...");
			// Get context object for working with queues, and 
			// set a default retry policy appropriate for a web user interface.
			var queueClient = storageAccount.CreateCloudQueueClient();
			queueClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

			Logger.Log($"Creating {ThumbnailQueueName} queue");
			var blobnameQueue = queueClient.GetQueueReference(ThumbnailQueueName);
			blobnameQueue.CreateIfNotExists();

			Logger.Log("Storage initialized");
		}

		public static CloudBlobContainer GetImageBlobContainer()
		{
			// Open storage account using credentials from .cscfg file.
			var storageAccount = CloudStorageAccount.Parse(StorageConnectionString);

			// Get context object for working with blobs, and 
			// set a default retry policy appropriate for a web user interface.
			var blobClient = storageAccount.CreateCloudBlobClient();
			blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

			// Get a reference to the blob container.
			return blobClient.GetContainerReference(ImageBlobName);
		}

		public static CloudQueue GetThumbnailQueue()
		{
			return GetQueue(ThumbnailQueueName);
		}

		private static CloudQueue GetQueue(string queueName)
		{
			// Open storage account using credentials from .cscfg file.
			var storageAccount = CloudStorageAccount.Parse(StorageConnectionString);

			// Get context object for working with queues, and 
			// set a default retry policy appropriate for a web user interface.
			var queueClient = storageAccount.CreateCloudQueueClient();
			queueClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 5);

			// Get a reference to the queue.
			return queueClient.GetQueueReference(queueName);
		}

		#endregion

		#region Work with Azure objects

		public static async Task AddObjectToThumbnailQueue(object obj, CancellationToken cancellationToken)
		{
			await AddObjectToQueue(ThumbnailQueueName, obj, cancellationToken);
		}

		public static async Task AddObjectToQueue(CloudQueue queue, object obj, CancellationToken cancellationToken)
		{
			if (obj == null)
				return;

			//// add object to Azure queue
			var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(obj));
			await queue.AddMessageAsync(queueMessage, cancellationToken);
			Logger.Log($"Created message for {queue.Name} queue, includes {obj}");
		}

		private static async Task AddObjectToQueue(string queueName, object obj, CancellationToken cancellationToken)
		{
			if (obj == null)
				return;

			//// add object to Azure queue
			var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(obj));
			var queue = GetQueue(queueName);
			await queue.AddMessageAsync(queueMessage, cancellationToken);
			Logger.Log($"Created message for {queueName} queue, includes {obj}");
		}

		#endregion

		#region Work with files

		public static async Task<CloudBlockBlob> UploadAndSaveBlobAsync(HttpPostedFileBase imageFile)
		{
			Logger.Log($"Uploading image file {imageFile.FileName}");

			var blobName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
			// Retrieve reference to a blob. 
			var imagesBlobContainer = GetImageBlobContainer();
			var imageBlob = imagesBlobContainer.GetBlockBlobReference(blobName);
			// Create the blob by uploading a local file.
			using (var fileStream = imageFile.InputStream)
			{
				await imageBlob.UploadFromStreamAsync(fileStream);
			}

			Logger.Log($"Uploaded image file to {imageBlob.Uri}");

			return imageBlob;
		}

		public static async Task DeleteAdBlobAsync(Uri blobUri)
		{
			var blobName = blobUri.Segments[blobUri.Segments.Length - 1];
			Logger.Log($"Deleting image blob {blobName}");
			// Retrieve reference to a blob. 
			var imagesBlobContainer = GetImageBlobContainer();
			var blobToDelete = imagesBlobContainer.GetBlockBlobReference(blobName);
			await blobToDelete.DeleteAsync();
		}

		public static async Task AddAdBlobToQueueAsync(long? id, CloudBlockBlob imageBlob)
		{
			if (imageBlob == null || id == null)
				return;

			var blobInfo = new BlobInformation()
			{
				AdId = id.Value,
				BlobUri = new Uri(imageBlob.Uri.ToString())
			};
			await AddObjectToThumbnailQueue(blobInfo, CancellationToken.None);
		}


		#endregion
	}
}