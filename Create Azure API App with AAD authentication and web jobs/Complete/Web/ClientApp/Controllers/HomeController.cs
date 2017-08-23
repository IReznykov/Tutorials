using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClientApp.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return View();
		}

		public ActionResult About()
		{
			//ViewBag.Message = "This site is Client Application for Data Api that implemented for tutorial \"Create Api App with authentication and web jobs\"";

			return View();
		}

		public ActionResult Contact()
		{
			return View();
		}
	}
}