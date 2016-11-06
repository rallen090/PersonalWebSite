using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
		[HttpGet, Route("home")]
        public ActionResult Home()
        {
            return View();
        }

		[HttpGet, Route("about")]
		public ActionResult About()
        {
            ViewBag.Thing = 1;
            return View();
        }

		[HttpGet, Route("blog")]
		public ActionResult Blog()
		{
			ViewBag.Thing = 1;
			return View();
		}

		[HttpGet, Route("projects")]
		public ActionResult Projects()
		{
			ViewBag.Thing = 1;
			return View();
		}

		[HttpGet, Route("contact")]
		public ActionResult Contact()
        {
            return View();
        }
    }
}