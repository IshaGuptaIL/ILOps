using ILOps_Inventory.Areas.Inventory.Models;
using ILOps_Inventory.Common.Spire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class InventoryController : Controller
    {
        private readonly string _connectionString;
        private readonly SpireApiHelper _spire;
        private readonly string _pgConn;

        public InventoryController(SpireApiHelper spire, IConfiguration configuration)
        {
            _spire = spire;
            _connectionString = configuration.GetConnectionString("bvactivation_Connection");
            _pgConn = configuration.GetConnectionString("spire_Connection");
        }

        [HttpGet]
        public async Task<IActionResult> InventoryAdd()
        {
            var roleId = HttpContext.Session.GetInt32("UserRoleId") ?? 0;

            var allWarehouses = await GetWarehousesAsync();

            List<SelectListItem> filtered =
                roleId == 7
                    ? allWarehouses
                    : allWarehouses.Where(w => w.Value == "CO").ToList();

            var model = new InventoryItems
            {
                Warehouses = filtered,
                Manufacturers = await GetManufacturersAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InventoryAdd([FromForm] InventoryItems model)
        {
            if (!Request.Form.ContainsKey("__RequestVerificationToken"))
            {
                return Json(new { success = false, errors = new Dictionary<string, string> { [""] = "Invalid request" } });
            }

            var roleId = HttpContext.Session.GetInt32("UserRoleId") ?? 0;

            // Force CO warehouse for non-admin
            if (roleId != 1) model.Whse = "CO";

            // Uppercase PartNo
            if (!string.IsNullOrEmpty(model.PartNo))
                model.PartNo = model.PartNo.ToUpperInvariant();

            // Validation
            var errors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(model.Whse)) errors["Whse"] = "Warehouse required";
            if (string.IsNullOrWhiteSpace(model.PartNo)) errors["PartNo"] = "Part Number required";
            if (string.IsNullOrWhiteSpace(model.Description)) errors["Description"] = "Description required";
            if (string.IsNullOrWhiteSpace(model.FrDescription)) errors["FrDescription"] = "French description required";
            if (model.Description?.Length > 80) errors["Description"] = $"Max 80 chars ({model.Description.Length})";
            if (model.FrDescription?.Length > 80) errors["FrDescription"] = $"Max 80 chars ({model.FrDescription.Length})";
            if (model.CostPrice <= 0) errors["CostPrice"] = "Cost Price must be > 0";

            ValidateTypeSpecific(model, errors);

            if (errors.Any()) return Json(new { success = false, errors });

            try
            {
                // Build EN Spire item
                var enItem = BuildSpireItemRequest(model.Whse!, model.Description!, model);
                var enJson = JsonSerializer.Serialize(enItem);
                var (okEn, enText, enResp) = await _spire.CallSpireAsync(HttpMethod.Post, "inventory/items/", 0, enJson);

                if (!okEn)
                {
                    _spire.DebugSpireResponse(enResp, enText, enJson);
                    return Json(new { success = false, errors = new Dictionary<string, string> { [""] = $"Spire EN failed: {enResp.HttpStatusText}" } });
                }

                // Build FR Spire item
                var frItem = BuildSpireItemRequest("FR", model.FrDescription!, model);
                var frJson = JsonSerializer.Serialize(frItem);
                var (okFr, frText, frResp) = await _spire.CallSpireAsync(HttpMethod.Post, "inventory/items/", 0, frJson);

                if (!okFr)
                {
                    _spire.DebugSpireResponse(frResp, frText, frJson);
                    return Json(new { success = false, errors = new Dictionary<string, string> { [""] = $"Spire FR failed: {frResp.HttpStatusText}" } });
                }

                // Postgres save
                var pgArray = new[]
                {
                    new {
                        whse = model.Whse,
                        partNo = model.PartNo,
                        description = model.Description,
                        currentCost = model.CostPrice,
                        averageCost = model.CostPrice,
                        salesDept = int.Parse(model.SalesDept ?? "0"),
                        serialized = (model.Type == "Hardware"),
                        userDef1 = model.AccessoryGroup,
                        allowBackorders = false
                    },
                    new {
                        whse = "FR",
                        partNo = model.PartNo,
                        description = model.FrDescription,
                        currentCost = model.CostPrice,
                        averageCost = model.CostPrice,
                        salesDept = int.Parse(model.SalesDept ?? "0"),
                        serialized = (model.Type == "Hardware"),
                        userDef1 = model.AccessoryGroup,
                        allowBackorders = false
                    }
                };

                var pgJson = JsonSerializer.Serialize(pgArray);
                var savedPg = await _spire.SaveInventoryToPostgresAsync(pgJson);

                return Json(new
                {
                    success = true,
                    message = $"✅ Added! EN={enResp.HeaderKey}, FR={frResp.HeaderKey}, PG={savedPg}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errors = new Dictionary<string, string> { [""] = $"Error: {ex.Message}" } });
            }
        }

        private void ValidateTypeSpecific(InventoryItems model, Dictionary<string, string> errors)
        {
            if (model.Type == "Hardware")
            {
                if (model.ProductCode != "HCC") errors["ProductCode"] = "Must be HCC for Hardware";
                if (model.SalesDept != "4") errors["SalesDept"] = "Must be 4 for Hardware";
            }
            else if (model.Type == "Accessory")
            {
                if (model.ProductCode != "ACC") errors["ProductCode"] = "Must be ACC for Accessory";
                if (model.SalesDept != "5") errors["SalesDept"] = "Must be 5 for Accessory";
                if (string.IsNullOrEmpty(model.AccessoryGroup)) errors["AccessoryGroup"] = "Group required for Accessory";
            }
            else if (model.Type == "License")
            {
                if (model.ProductCode != "ACC") errors["ProductCode"] = "Must be ACC for License";
                if (model.SalesDept != "5") errors["SalesDept"] = "Must be 5 for License";
            }
        }

        private async Task<List<SelectListItem>> GetWarehousesAsync()
        {
            const string sql = "SELECT whse, description FROM public.inventory_warehouses WHERE whse IS NOT NULL ORDER BY id";
            var list = new List<SelectListItem>();

            await using var con = new NpgsqlConnection(_pgConn);
            await using var cmd = new NpgsqlCommand(sql, con);
            await con.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new SelectListItem
                {
                    Value = rdr["whse"]?.ToString() ?? "",
                    Text = $"{rdr["whse"]} - {rdr["description"]}"
                });
            }

            return list;
        }

        private async Task<List<ManufacturerBO>> GetManufacturersAsync()
        {
            const string sql = "SELECT Id, Name, InventoryType FROM tblMan WHERE Name IS NOT NULL ORDER BY Name";
            var list = new List<ManufacturerBO>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new ManufacturerBO
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    InventoryType = reader.GetString("InventoryType")
                });
            }

            return list;
        }



        [HttpGet]
        public async Task<IActionResult> CheckPartNo(string partNo, string whse)
        {
            if (string.IsNullOrWhiteSpace(partNo))
                return Json(new { exists = false });

            const string sql = @"
        SELECT 1 
        FROM inventory 
        WHERE part_no = @partNo 
        AND whse = @whse
        LIMIT 1";

            await using var con = new NpgsqlConnection(_pgConn);
            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@partNo", partNo.ToUpper());
            cmd.Parameters.AddWithValue("@whse", whse);

            await con.OpenAsync();
            var exists = await cmd.ExecuteScalarAsync() != null;

            return Json(new { exists });
        }

        private SpireInventoryItemRequest BuildSpireItemRequest(string whse, string description, InventoryItems model)
        {
            var item = new SpireInventoryItemRequest
            {
                whse = whse,
                partNo = model.PartNo!,
                description = (description ?? string.Empty).Length > 80 ? description[..80] : description ?? string.Empty,
                currentCost = (decimal)model.CostPrice,
                averageCost = (decimal)model.CostPrice,
                userDef1 = model.AccessoryGroup,
                allowBackorders = false
            };

            // Type-specific
            switch (model.Type)
            {
                case "Hardware":
                    item.groupNo = "HCC";
                    item.salesDept = 4;
                    item.serialized = true;
                    break;
                case "Accessory":
                    item.groupNo = "ACC";
                    item.salesDept = 5;
                    break;
                case "License":
                    item.groupNo = "ACC";
                    item.salesDept = 5;
                    item.serialized = true;
                    break;
            }

            // **VBA-style pricing logic**
            item.pricing = new Dictionary<string, SpireInventoryPricingDetail>();

            if (model.SellingPrice.HasValue)
            {
                var priceDetail = new SpireInventoryPricingDetail();
                priceDetail.sellPrices.Add(model.SellingPrice.Value);

                // Assume "EA" as default UOM (same as VBA)
                item.pricing["EA"] = priceDetail;
            }

            return item;
        }
    }
}
