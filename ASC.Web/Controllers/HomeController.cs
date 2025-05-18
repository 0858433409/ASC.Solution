using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ASC.Solution.Configuration;
using ASC.Web.Models;
using System.Diagnostics;
using ASC.Utilities;

namespace ASC.Web.Controllers
{
    public class HomeController : AnonymousController
    {
        private readonly IOptions<ApplicationSettings> _settings;

        public HomeController(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }
        public IActionResult Index()
        {
            // Set Session
            HttpContext.Session.SetSession("Test", _settings.Value);

            // Get Session
            var settings = HttpContext.Session.GetSession<ApplicationSettings>("Test");

            // Usage of IOoptions
            ViewBag.Title = _settings.Value.ApplicationTitle;

            // Test fail test case
            // ViewData.Model = "Test";
            // throw new Exception("Login Fail!!!");

            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
