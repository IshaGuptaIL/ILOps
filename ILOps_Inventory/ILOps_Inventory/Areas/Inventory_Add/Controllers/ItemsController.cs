using ILOps_Inventory.Areas.Inventory_Add.Models;
using Microsoft.AspNetCore.Mvc;

namespace ILOps_Inventory.Areas.Inventory_Add.Controllers
{

    [Area("Inventory_Add")]
    public class ItemsController : Controller
    {
        //private readonly SpireApiHelper _spire;

        //public ItemsController(SpireApiHelper spire)
        //{
        //    _spire = spire;
        //}
        public IActionResult AddInventoryItems()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> AddInventoryItems(InventoryItems model)
        {
            // 1) Model attribute validation
            if (!ModelState.IsValid)
                return View(model);



            // 2) Extra business rules (type + codes)
            if (model.Type == "Accessory" && string.IsNullOrEmpty(model.AccessoryGroup))
                ModelState.AddModelError(nameof(model.AccessoryGroup),
                    "You must select a group when creating accessories.");

            if (model.Type == "Hardware" && string.IsNullOrEmpty(model.AccessoryGroup))
                ModelState.AddModelError(nameof(model.AccessoryGroup),
                    "You must select a manufacturer when creating hardware.");

            if (model.Type == "Hardware" &&
                model.ProductCode != "HCC" && model.SalesDept != "4")
                ModelState.AddModelError(string.Empty,
                    "Product Code and Sales Department not correct for Hardware.");

            if (model.Type == "Hardware" && model.ProductCode != "HCC")
                ModelState.AddModelError(nameof(model.ProductCode),
                    "Product Code not correct for Hardware.");

            if (model.Type == "Hardware" && model.SalesDept != "4")
                ModelState.AddModelError(nameof(model.SalesDept),
                    "Sales Dept not correct for Hardware.");

            if (model.Type == "Accessory" &&
                model.ProductCode != "ACC" && model.SalesDept != "5")
                ModelState.AddModelError(string.Empty,
                    "Product Code and Sales Department not correct for Accessory.");

            if (model.Type == "Accessory" && model.ProductCode != "ACC")
                ModelState.AddModelError(nameof(model.ProductCode),
                    "Product Code not correct for Accessory.");

            if (model.Type == "Accessory" && model.SalesDept != "5")
                ModelState.AddModelError(nameof(model.SalesDept),
                    "Sales Dept not correct for Accessory.");

            if (model.Type == "License" &&
                model.ProductCode != "ACC" && model.SalesDept != "5")
                ModelState.AddModelError(string.Empty,
                    "Product Code and Sales Department not correct for License.");

            if (model.Type == "License" && model.ProductCode != "ACC")
                ModelState.AddModelError(nameof(model.ProductCode),
                    "Product Code not correct for License.");

            if (model.Type == "License" && model.SalesDept != "5")
                ModelState.AddModelError(nameof(model.SalesDept),
                    "Sales Dept not correct for License.");

            if (!ModelState.IsValid)
                return View(model);



            return RedirectToAction("AddInventoryItems");
        }

    }
}
