using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DataApi.Attributes
{
	/// <summary>
	/// Authorization attribute that require to use only https schema.
	/// </summary>
	public class RequireHttpsAttribute : AuthorizationFilterAttribute
	{
		/// <summary>
		/// Override base method, check for authorization.
		/// </summary>
		/// <param name="actionContext"></param>
		public override void OnAuthorization(HttpActionContext actionContext)
		{
			if (actionContext.Request.RequestUri.Scheme != Uri.UriSchemeHttps)
			{
				actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
				{
					ReasonPhrase = "HTTPS Required"
				};
			}
			else
			{
				base.OnAuthorization(actionContext);
			}
		}
	}
}