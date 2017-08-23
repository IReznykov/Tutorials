using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Ikc5.TypeLibrary.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage.Blob;
using ResizeImageJobEx.DataApi;

namespace ResizeImageJobEx
{
	public class Functions
	{
		/// <summary>
		/// Console logger. Is used by all functions, shows logs in common Dashboard.
		/// </summary>
		private static readonly ILogger Logger = new ConsoleLogger();

		public static async Task GenerateThumbnailAsync(
			[QueueTrigger(AzureConfig.ThumbnailQueueName)] BlobInformation blobInfo,
			[Blob("images/{BlobName}", FileAccess.Read)] Stream input,
			[Blob("images/{BlobNameWithoutExtension}_thumbnail.jpg")] CloudBlockBlob outputBlob,
			TextWriter textWriter,
			CancellationToken cancellationToken)
		{
			// log initial data
			Logger.Log($"Logger - Call with blob=\'{blobInfo}\'");
			await textWriter.WriteLineAsync($"TextWriter - Call with blob=\'{blobInfo}\'");

			// process image
			using (Stream output = outputBlob.OpenWrite())
			{
				ConvertImageToThumbnailJpeg(input, output, textWriter);
				outputBlob.Properties.ContentType = "image/jpeg";
			}
			await textWriter.WriteLineAsync($"Thumbnail is created, Url=\'{outputBlob.Uri}\'");

			try
			{
				// update ad object
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					var id = blobInfo.AdId;
					var ad = await dataApiClient.Ads.GetAdAsync(id, cancellationToken);
					if (ad == null)
					{
						throw new ArgumentException($"Ad (Id={0}) not found, can't create thumbnail");
					}
					ad.ThumbnailUrl = outputBlob.Uri.ToString();
					await dataApiClient.Ads.PutAdAsync(id, ad, cancellationToken);
					await textWriter.WriteLineAsync($"Ad is updated, ThumbnailUrl=\'{ad.ThumbnailUrl}\'");
				}
			}
			// parse errors
			catch (OperationCanceledException ex)
			{
				Logger.Exception(ex);
				await textWriter.WriteLineAsync(
					$"Exception is catched: {ex.GetType().FullName}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}
			catch (AggregateException ex)
			{
				foreach (var innerException in ex.InnerExceptions)
				{
					Logger.Exception(innerException);
					await textWriter.WriteLineAsync(
						$"Exception is catched: {ex.GetType().FullName}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
				}
			}
			catch (HttpOperationException ex)
			{
				await textWriter.WriteLineAsync($"Error is occured. Request: {Environment.NewLine}{ex.Request.RequestUri}" +
												$"{Environment.NewLine}Response: {ex.Response.Content}{Environment.NewLine}Code: {ex.Response.StatusCode}{Environment.NewLine}Message: {ex.Message}");
				Logger?.Exception(ex);
			}
			catch (Exception ex)
			{
				Logger?.Exception(ex);
				await textWriter.WriteLineAsync(
					$"Exception is catched: {ex.GetType().FullName}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}
			Logger.LogEnd("some calls is executed asynchronously.");
		}

		/// <summary>
		/// Process message in poison queue.
		/// </summary>
		/// <param name="blobInfo"></param>
		/// <param name="textWriter"></param>
		public static void ProcessPoisonAuthorRequestQueue(
			[QueueTrigger(AzureConfig.ThumbnailPoisonQueueName)] BlobInformation blobInfo,
			TextWriter textWriter)
		{
			// process the poison message and log it or send a notification
			Logger.Log($"Logger - {AzureConfig.ThumbnailPoisonQueueName} queue has a failed message with blob=\'{blobInfo}\'");
			textWriter.WriteLine($"TextWriter - {AzureConfig.ThumbnailPoisonQueueName} queue has a failed message with blob=\'{blobInfo}\'");
		}

		/// <summary>
		/// Job triggered by an crontab schedule each 5 minutes and run immediately on
		/// startup to add request for reading article statistics.
		/// </summary>
		public static async Task UpdateLostThumbnailAsync(
#if DEBUG
			// in debug mode one launch is enough, so use long period
			[TimerTrigger("0 0/15 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
#else
			[TimerTrigger("0 0/5 * * * *", RunOnStartup = false)] TimerInfo timerInfo,
#endif
			[Queue(AzureConfig.ThumbnailQueueName)] IAsyncCollector<BlobInformation> outputBlobInfoQueue,
			TextWriter textWriter,
			CancellationToken cancellationToken)
		{
			var message =
				$"UpdateLostThumbnailAsync: Last = '{timerInfo.ScheduleStatus?.Last ?? DateTime.MinValue}', "
				+ $"Next = '{timerInfo.ScheduleStatus?.Next ?? DateTime.MinValue}', IsPastDue ={timerInfo.IsPastDue}";
			Logger.Log($"Logger - {message}");
			await textWriter.WriteLineAsync($"TextWriter - {message}");

			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					var utcNow = DateTime.UtcNow;

					// get all ad without thumbnails and compose blobInformation objects
					var blobInfos = (await dataApiClient.Ads.GetAdsAsync(cancellationToken).ConfigureAwait(false))
						.Where(ad => string.IsNullOrEmpty(ad.ThumbnailUrl) && !string.IsNullOrEmpty(ad.ImageUrl))
						.Where(ad => utcNow.Subtract(ad.LastModified.GetValueOrDefault(utcNow)).TotalMinutes > 15)
						.Select(ad => new BlobInformation()
						{
							AdId = ad.Id.GetValueOrDefault(-1),
							BlobUri = new Uri(ad.ImageUrl)
						});

					foreach (var blobInfo in blobInfos)
					{
						await outputBlobInfoQueue.AddAsync(blobInfo, cancellationToken);
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				Logger.Exception(ex);
				await textWriter.WriteLineAsync(
					$"Exception is catched: {ex.GetType().FullName}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}
			catch (AggregateException ex)
			{
				foreach (var innerException in ex.InnerExceptions)
				{
					Logger.Exception(innerException);
					await textWriter.WriteLineAsync(
						$"Exception is catched: {ex.GetType().FullName}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
				}
			}
			catch (HttpOperationException ex)
			{
				Logger?.Exception(ex);
				await textWriter.WriteLineAsync($"Error is occured. Request: {Environment.NewLine}{ex.Request.RequestUri}" +
					$"{Environment.NewLine}Response: {ex.Response.Content}{Environment.NewLine}Code: {ex.Response.StatusCode}{Environment.NewLine}Message: {ex.Message}");
			}
			catch (Exception ex)
			{
				Logger?.Exception(ex);
				await textWriter?.WriteLineAsync($"Exception is catched: {ex.GetType().FullName}{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
			}

			Logger.LogEnd("some calls is executed asynchronously.");
		}

		public static void ConvertImageToThumbnailJpeg(
			Stream input, Stream output,
			TextWriter textWriter)
		{
			textWriter.WriteLine("ConvertImageToThumbnailJpegAsync - calculate size");

			const int thumbnailsize = 80;
			int width;
			int height;
			var originalImage = new Bitmap(input);

			if (originalImage.Width > originalImage.Height)
			{
				width = thumbnailsize;
				height = thumbnailsize * originalImage.Height / originalImage.Width;
			}
			else
			{
				height = thumbnailsize;
				width = thumbnailsize * originalImage.Width / originalImage.Height;
			}

			Bitmap thumbnailImage = null;
			try
			{
				thumbnailImage = new Bitmap(width, height);
				textWriter.WriteLine("ConvertImageToThumbnailJpegAsync - ready to draw image");

				using (var graphics = Graphics.FromImage(thumbnailImage))
				{
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
					graphics.DrawImage(originalImage, 0, 0, width, height);
				}
				textWriter.WriteLine("ConvertImageToThumbnailJpegAsync - ready to save image");

				thumbnailImage.Save(output, ImageFormat.Jpeg);
				textWriter.WriteLine("ConvertImageToThumbnailJpegAsync - image processing complete");
			}
			finally
			{
				thumbnailImage?.Dispose();
			}
		}
	}
}
