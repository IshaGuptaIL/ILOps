using Microsoft.AspNetCore.Mvc;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class IMEIController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Areas/Inventory/Views/Inventory/IMEI.cshtml");
        }

        public IActionResult ReceiveImei()
        {
            return View("~/Areas/Inventory/Views/Inventory/ReceiveIMEI.cshtml");
        }

        public IActionResult InvoiceCredit()
        {
            return View("~/Areas/Inventory/Views/Inventory/InvoiceCredit.cshtml");
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult FindByImei()
        {
            return View();
        }

        public IActionResult ReverseReceipt()
        {
            return View();
        }

        public IActionResult ImeiExceptions()
        {
            return View();
        }

        public IActionResult ExitReceive()
        {
            return View();
        }
    }
}
