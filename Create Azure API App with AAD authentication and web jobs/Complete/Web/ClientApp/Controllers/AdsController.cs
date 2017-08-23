using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Azure;
using ClientApp.DataApi;
using Ikc5.TypeLibrary.Logging;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Ad = ClientApp.DataApi.Models.Ad;
using Category = ClientApp.DataApi.Models.Category;
using Newtonsoft.Json;

namespace ClientApp.Controllers
{
	/// <summary>
	/// Controller for view and action on Ads subset. Most call redirect to Data Api
	/// in order to get data.
	/// </summary>
	public class AdsController : Controller
	{
		private readonly ILogger _logger = new ConsoleLogger();

		public AdsController()
		{
		}

		// GET: Ads
		public async Task<ActionResult> Index(Category category = null)
		{
			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					HttpOperationResponse<IList<Ad>> result;

					// call to data api
					if (category?.Id == null)
					{
						result = await dataApiClient.Ads.GetAdsWithHttpMessagesAsync();
					}
					else
					{
						result = await dataApiClient.Ads.GetAdsByCategoryWithHttpMessagesAsync(category.Id.Value);
					}

					if (result.Response.StatusCode != HttpStatusCode.OK)
					{
						//ViewBag.errorMessage =
						//	$"Action Index, Data Api returns the following code {result.Response.StatusCode}, reason \'{result.Response.ReasonPhrase}\'";
						//return View("Error");
						return new HttpStatusCodeResult(result.Response.StatusCode, result.Response.ReasonPhrase);
					}

					var adList = result.Body;

					return View(adList);
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// GET: Ads/Details/5
		public async Task<ActionResult> Details(long? id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					// call to data api
					var ad = await dataApiClient.Ads.GetAdAsync(id.Value);
					if (ad == null)
					{
						return HttpNotFound();
					}
					return View(ad);
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// GET: Ads/Create
		public async Task<ActionResult> Create()
		{
			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					await InitCategoriesAsync(dataApiClient);
					return View();
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// POST: Ads/Create
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Create(
			[Bind(Include = "Id,Title,Description,CategoryId,Price,Phone,Posted")] Ad ad,
			HttpPostedFileBase imageFile)
		{
			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					if (ModelState.IsValid)
					{
						CloudBlockBlob imageBlob = null;
						if (imageFile != null && imageFile.ContentLength != 0)
						{
							imageBlob = await AzureConfig.UploadAndSaveBlobAsync(imageFile);
							ad.ImageUrl = imageBlob.Uri.ToString();
						}
						ad.Posted = DateTime.Now;
						ad = await dataApiClient.Ads
							.PostAdAsync(ad)
							.ConfigureAwait(false);
						_logger.Log($"Created Ad (Id={ad.Id}) in database");

						await AzureConfig.AddAdBlobToQueueAsync(ad.Id, imageBlob);
						return RedirectToAction("Index");
					}

					await InitCategoriesAsync(dataApiClient, ad.CategoryId);
					return View(ad);
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// GET: Ads/Edit/5
		public async Task<ActionResult> Edit(long? id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					var ad = await dataApiClient.Ads.GetAdAsync(id.Value);
					if (ad == null)
					{
						return HttpNotFound();
					}
					await InitCategoriesAsync(dataApiClient, ad.CategoryId);
					return View(ad);
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// POST: Ads/Edit/5
		// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Edit(
			[Bind(Include = "Id,Title,Description,CategoryId,ImageUrl,ThumbnailUrl,Price,Phone,Posted")] Ad ad,   //ImageUrl,ThumbnailUrl,
			HttpPostedFileBase imageFile)
		{
			if (ad == null)
				return View(ad);

			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					if (ModelState.IsValid && ad.Id != null)
					{
						CloudBlockBlob imageBlob = null;
						if (imageFile != null && imageFile.ContentLength != 0)
						{
							// User is changing the image -- delete the existing
							// image blobs and then upload and save a new one.
							await DeleteAdBlobsAsync(ad);
							imageBlob = await AzureConfig.UploadAndSaveBlobAsync(imageFile);
							ad.ImageUrl = imageBlob.Uri.ToString();
						}
						await dataApiClient.Ads.PutAdAsync(ad.Id.Value, ad);

						await AzureConfig.AddAdBlobToQueueAsync(ad.Id, imageBlob);
						return RedirectToAction("Index");
					}

					await InitCategoriesAsync(dataApiClient, ad.CategoryId);
					return View(ad);
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// GET: Ads/Delete/5
		public async Task<ActionResult> Delete(long? id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					var ad = await dataApiClient.Ads.GetAdAsync(id.Value);
					if (ad == null)
					{
						return HttpNotFound();
					}
					return View(ad);
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		// POST: Ads/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> DeleteConfirmed(long id)
		{
			try
			{
				using (var dataApiClient = CompleteDataApi.NewDataApiClient())
				{
					var ad = await dataApiClient.Ads.DeleteAdAsync(id);
					await DeleteAdBlobsAsync(ad);

					return RedirectToAction("Index");
				}
			}
			catch (OperationCanceledException ex)
			{
				return LogAndShowError(ex);
			}
			catch (AggregateException ex)
			{
				return LogAndShowError(ex);
			}
			catch (HttpOperationException ex)
			{
				return LogAndShowError(ex);
			}
			//catch (Exception ex)
			//{
			//	return LogAndShowError(ex);
			//}
		}

		#region Helpers

		private IList<Category> _categoriesList = null;
		private DateTime _lastCategoryCall = DateTime.MinValue;

		private async Task InitCategoriesAsync(ICompleteDataApi dataApiClient, long? selectedValue = null)
		{
			if (DateTime.UtcNow.Subtract(_lastCategoryCall).Minutes < 5 && _categoriesList != null)
			{
				ViewBag.CategoryId = new SelectList(_categoriesList, "Id", "Name", selectedValue);
				return;
			}

			var categories = await dataApiClient.Categories.GetCategoriesAsync();
			if (categories == null)
				throw new ArgumentException("List of categories was not correctly returned");

			_categoriesList = categories;
			_lastCategoryCall = DateTime.UtcNow;
			ViewBag.CategoryId = new SelectList(categories, "Id", "Name", selectedValue);
		}

		#endregion

		#region Storage handling

		// Code is taken from
		// see https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk-get-started

		private async Task DeleteAdBlobsAsync(Ad ad)
		{
			if (!string.IsNullOrWhiteSpace(ad.ImageUrl))
			{
				var blobUri = new Uri(ad.ImageUrl);
				await AzureConfig.DeleteAdBlobAsync(blobUri);
			}
			if (!string.IsNullOrWhiteSpace(ad.ThumbnailUrl))
			{
				var blobUri = new Uri(ad.ThumbnailUrl);
				await AzureConfig.DeleteAdBlobAsync(blobUri);
			}
			_logger.Log($"Deleted ad {ad.Id}");
		}

		#endregion

		#region Error handling

		private ActionResult LogAndShowError(IHttpOperationResponse response)
		{
			return LogAndShowError($"Action Index, Data Api returns the following code {response.Response.StatusCode}, reason \'{response.Response.ReasonPhrase}\'");
		}

		private ActionResult LogAndShowError(AggregateException ex)
		{
			var commonMessage = new StringBuilder();
			foreach (var innerException in ex.InnerExceptions)
			{
				_logger.Exception(innerException);
				Trace.WriteLine(innerException.Message);
				commonMessage.AppendLine(innerException.Message);
			}
			return LogAndShowError(commonMessage.ToString());
		}

		private ActionResult LogAndShowError(Exception ex)
		{
			_logger.Exception(ex);
			Trace.WriteLine(ex.Message);

			return LogAndShowError(ex.Message);
		}

		private ActionResult LogAndShowError(string message)
		{
			ViewBag.errorMessage = message;
			return View("Error");
		}

		#endregion

		#region Override base controller methods

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// dispose managed resources
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}
