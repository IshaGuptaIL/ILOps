using System.Diagnostics;
using ILOps_Inventory.Models;
using Microsoft.AspNetCore.Mvc;

namespace ILOps_Inventory.Controllers
{
        [Area("Common")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(
         actionName: "Login",
         controllerName: "Account",
         new { area = "" }   // 🔥 IMPORTANT
     );
        }
    }
}
