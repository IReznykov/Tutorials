using System.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Azure
{
	/// <summary>
	/// Code is taken from
	/// https://docs.microsoft.com/en-us/azure/app-service-api/app-service-api-dotnet-service-principal-auth
	/// </summary>
	public static class ServicePrincipal
	{
		// The issuer URL of the tenant. For example: login.microsoftonline.com/contoso.onmicrosoft.com
		private static readonly string _authority = ConfigurationManager.AppSettings["ida:Authority"];

		// The Client ID of the calling AAD app (i.e. the one associated with this project). For example: 960adec2-b74a-484a-960adec2-b74a-484a
		private static readonly string _clientId = ConfigurationManager.AppSettings["ida:ClientId"];

		// The key that was created for the calling AAD app. For example: oCgdj3EYLfnR0p6iR3UvHFAfkn+zQB+0VqZT/6=
		private static readonly string _clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];

		// The Client ID of the called AAD app. For example: e65e8fc9-5f6b-48e8-e65e8fc9-5f6b-48e8
		// The called AAD app may be the same as the calling AAD app, in which case this value will be the same as the ClientId value.
		private static readonly string _resource = ConfigurationManager.AppSettings["ida:Resource"];

		public static AuthenticationResult GetS2SAccessTokenForProdMSA()
		{
			return GetS2SAccessToken(_authority, _resource, _clientId, _clientSecret).Result;
		}

		///<summary>
		/// Gets an application token used for service-to-service (S2S) API calls.
		///</summary>
		private static async Task<AuthenticationResult> GetS2SAccessToken(string authority, string resource, string clientId, string clientSecret)
		{
			// Client credential consists of the "client" AAD web application's Client ID
			// and the key that was generated for the application in the AAD Azure portal extension.
			var clientCredential = new ClientCredential(clientId, clientSecret);

			// The authentication context represents the AAD directory.
			var context = new AuthenticationContext(authority, false);

			// Fetch an access token from AAD.
			var authenticationResult = await context.AcquireTokenAsync(
				resource,
				clientCredential);
			return authenticationResult;
		}
	}
}
