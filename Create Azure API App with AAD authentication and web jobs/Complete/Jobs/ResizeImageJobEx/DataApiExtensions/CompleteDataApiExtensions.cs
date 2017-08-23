using System;
using System.Configuration;
using System.Net.Http.Headers;
using Azure;

namespace ResizeImageJobEx.DataApi
{
	public partial class CompleteDataApi
	{
		partial void CustomInitialize()
		{
		}

		public static ICompleteDataApi NewDataApiClient()
		{
			var client = new CompleteDataApi(new Uri(ConfigurationManager.AppSettings["DataApiUrl"]));
			client.HttpClient.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", ServicePrincipal.GetS2SAccessTokenForProdMSA().AccessToken);

			return client;
		}

	}
}