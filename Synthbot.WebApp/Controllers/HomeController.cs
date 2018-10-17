using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synthbot.WebApp.Models;

namespace Synthbot.WebApp.Controllers
{
	public class HomeController : Controller
	{
		public HomeController()
		{
		}

		public IActionResult Index()
		{
			return View();
		}

		[Authorize]
		public IActionResult About()
		{
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		[Authorize]
		public IActionResult Contact()
		{
			ViewData["Message"] = "Your contact page.";

			return View();
		}

		[Authorize]
		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
