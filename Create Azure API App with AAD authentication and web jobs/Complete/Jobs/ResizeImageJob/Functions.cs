using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using Models;

namespace ResizeImageJob
{
    public class Functions
    {
        public static void GenerateThumbnail(
        [QueueTrigger(AzureConfig.ThumbnailQueueName)] BlobInformation blobInfo,
        [Blob("images/{BlobName}", FileAccess.Read)] Stream input,
        [Blob("images/{BlobNameWithoutExtension}_thumbnail.jpg")] CloudBlockBlob outputBlob)
        {
            using (Stream output = outputBlob.OpenWrite())
            {
                ConvertImageToThumbnailJpeg(input, output);
                outputBlob.Properties.ContentType = "image/jpeg";
            }

			// Entity Framework context class is not thread-safe, so it must
			// be instantiated and disposed within the function.
			using (var db = new GoodEntities())
			{
				var id = blobInfo.AdId;
				var ad = db.Ads.Find(id);
				if (ad == null)
				{
					throw new ArgumentException($"Ad (Id={id}) not found, can't create thumbnail");
				}
				ad.Thumbnail_Url = outputBlob.Uri.ToString();
				db.SaveChanges();
			}
		}

        public static void ConvertImageToThumbnailJpeg(Stream input, Stream output)
        {
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

                using (var graphics = Graphics.FromImage(thumbnailImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnailImage.Save(output, ImageFormat.Jpeg);
            }
            finally
            {
	            thumbnailImage?.Dispose();
            }
        }
    }
}
