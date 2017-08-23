using System;
using System.IO;

namespace Azure
{
	/// <summary>
	/// Code is taken from
	/// https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk-get-started
	/// </summary>
	public class BlobInformation
	{
		public long AdId { get; set; }

		public Uri BlobUri { get; set; }

		public string BlobName => BlobUri.Segments[BlobUri.Segments.Length - 1];

		public string BlobNameWithoutExtension => Path.GetFileNameWithoutExtension(BlobName);

		public override string ToString()
		{
			return $"AdId={AdId}, BlobUri={BlobName}";
		}
	}

}
