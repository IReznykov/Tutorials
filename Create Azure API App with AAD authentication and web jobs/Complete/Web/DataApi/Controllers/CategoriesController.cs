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
	/// Controller for categories. Implements standard get-post operations.
	/// </summary>
	public class CategoriesController : ApiController
	{
		private readonly GoodEntities _db = new GoodEntities();

		/// GET: api/Categories
		/// <summary>
		/// Return all Categories from data source.
		/// </summary>
		/// <returns></returns>
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(IQueryable<Category>), Description = "List of Categories was returned")]
		public IQueryable<Category> GetCategories()
		{
			return _db.Categories;
		}

		/// GET: api/Categories/5
		/// <summary>
		/// Return Category by id.
		/// </summary>
		/// <param name="id">id of Category</param>
		/// <returns></returns>
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(Category), Description = "Required Category was found and successfully returned")]
		[SwaggerResponse(HttpStatusCode.NotFound, description: "Category with provided id was not found")]
		public async Task<IHttpActionResult> GetCategory(long id)
		{
			var category = await _db.Categories.FindAsync(id);
			if (category == null)
			{
				return NotFound();
			}

			return Ok(category);
		}

		/// PUT: api/Categories/5
		/// <summary>
		/// Put updated Category.
		/// </summary>
		/// <param name="id">id of the Category</param>
		/// <param name="category">Existent Category</param>
		/// <returns></returns>
		[ResponseType(typeof(void))]
		[SwaggerResponse(HttpStatusCode.NoContent, description: "Category was updated")]
		[SwaggerResponse(HttpStatusCode.NotFound, description: "Category was already changed")]
		public async Task<IHttpActionResult> PutCategory(long id, Category category)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (id != category.Id)
			{
				return BadRequest();
			}

			_db.Entry(category).State = EntityState.Modified;

			try
			{
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!CategoryExists(id))
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

		/// POST: api/Categories
		/// <summary>
		/// Create new or updated existent Category.
		/// </summary>
		/// <param name="category">New or existent Category</param>
		/// <returns></returns>
		[ResponseType(typeof(Category))]
		[SwaggerResponse(HttpStatusCode.Created, Type = typeof(Category), Description = "Category was created")]
		public async Task<IHttpActionResult> PostCategory(Category category)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			_db.Categories.Add(category);
			await _db.SaveChangesAsync();

			return CreatedAtRoute("DefaultApi", new { id = category.Id }, category);
		}

		/// DELETE: api/Categories/5
		/// <summary>
		/// Deletes the Category.
		/// </summary>
		/// <param name="id">id of the Category</param>
		/// <returns></returns>
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(Category), Description = "Category was deleted")]
		[SwaggerResponse(HttpStatusCode.NotFound, description: "Category was already removed")]
		public async Task<IHttpActionResult> DeleteCategory(long id)
		{
			Category category = await _db.Categories.FindAsync(id);
			if (category == null)
			{
				return NotFound();
			}

			_db.Categories.Remove(category);
			await _db.SaveChangesAsync();

			return Ok(category);
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
		/// Check that already exists Category with id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private bool CategoryExists(long id)
		{
			return _db.Categories.Count(e => e.Id == id) > 0;
		}
	}
}