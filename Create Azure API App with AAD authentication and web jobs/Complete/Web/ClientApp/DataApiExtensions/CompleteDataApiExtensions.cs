using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;
using Azure;

namespace ClientApp.DataApi
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