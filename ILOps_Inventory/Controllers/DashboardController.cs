using Microsoft.AspNetCore.Mvc;

namespace ILOps_Inventory.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
                return RedirectToAction("Login", "Account");
            return View();
        }
    }
}
