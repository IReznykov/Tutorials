using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Models;
using Swashbuckle.Swagger.Annotations;

namespace DataApi.Controllers
{
	/// <summary>
	/// Controller for ads. Implements standard get-post operations.
	/// </summary>
	public class AdsController : ApiController
	{
		private readonly GoodEntities _db = new GoodEntities();

		/// GET: api/Ads
		/// <summary>
		/// Return all Ads from data source.
		/// </summary>
		/// <returns></returns>
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(IQueryable<Ad>), Description = "List of Ads was returned")]
		public IQueryable<Ad> GetAds()
		{
			//return _db.Ads;
			var ads = _db.Ads.Include(ad => ad.Category);
			return ads;
		}

		/// GET: api/Ads/categoryId
		/// <summary>
		/// Return all Ads from data source.
		/// </summary>
		/// <param name="categoryId">Id of category that filter ads.</param>
		/// <returns></returns>
		[Route("api/Ads/Category/{categoryId:long}")]
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(IQueryable<Ad>), Description = "List of Ads was returned")]
		public IQueryable<Ad> GetAdsByCategory(long categoryId)
		{
			var ads = _db.Ads.Include(ad => ad.Category).Where(ad => ad.CategoryId == categoryId);
			return ads;
		}

		/// GET: api/Ads/AdId
		/// <summary>
		/// Return Ad by id.
		/// </summary>
		/// <param name="id">id of Ad</param>
		/// <returns></returns>
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(Ad), Description = "Required Ad was found and successfully returned")]
		[SwaggerResponse(HttpStatusCode.NotFound, description: "Ad with provided id was not found")]
		public async Task<IHttpActionResult> GetAd(long id)
		{
			var ad = await _db.Ads.FindAsync(id);
			if (ad == null)
			{
				return NotFound();
			}

			return Ok(ad);
		}

		/// PUT: api/Ads/5
		/// <summary>
		/// Put updated Ad.
		/// </summary>
		/// <param name="id">id of the Ad</param>
		/// <param name="ad">Existent Ad</param>
		/// <returns></returns>
		[ResponseType(typeof(void))]
		[SwaggerResponse(HttpStatusCode.NoContent, description: "Ad was updated")]
		[SwaggerResponse(HttpStatusCode.NotFound, description: "Ad was already changed")]
		public async Task<IHttpActionResult> PutAd(long id, Ad ad)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (id != ad.Id)
			{
				return BadRequest();
			}

			_db.Entry(ad).State = EntityState.Modified;

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!AdExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return StatusCode(HttpStatusCode.NoContent);
		}

		/// POST: api/Ads
		/// <summary>
		/// Create new or updated existent Ad.
		/// </summary>
		/// <param name="ad">New or existent Ad</param>
		/// <returns></returns>
		[ResponseType(typeof(Ad))]
		[SwaggerResponse(HttpStatusCode.Created, Type = typeof(Ad), Description = "Ad was created")]
		public async Task<IHttpActionResult> PostAd(Ad ad)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			_db.Ads.Add(ad);
			await _db.SaveChangesAsync();

			return CreatedAtRoute("DefaultApi", new { id = ad.Id }, ad);
		}

		/// DELETE: api/Ads/5
		/// <summary>
		/// Deletes the Ad.
		/// </summary>
		/// <param name="id">id of the Ad</param>
		/// <returns></returns>
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(Ad), Description = "Ad was deleted")]
		[SwaggerResponse(HttpStatusCode.NotFound, description: "Ad was already removed")]
		public async Task<IHttpActionResult> DeleteAd(long id)
		{
			var ad = await _db.Ads.FindAsync(id);
			if (ad == null)
			{
				return NotFound();
			}

			_db.Ads.Remove(ad);
			await _db.SaveChangesAsync();

			return Ok(ad);
		}

		/// <summary>
		/// Implentation of IDispose interface.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_db.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Check that already exists Ad with id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private bool AdExists(long id)
		{
			return _db.Ads.Count(e => e.Id == id) > 0;
		}
	}
}