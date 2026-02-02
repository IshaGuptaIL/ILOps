using ILOps_Inventory.Areas.Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class ModifyInventoryController : Controller
    {
        private readonly string _pgConn;

        public ModifyInventoryController(IConfiguration configuration)
        {
            _pgConn = configuration.GetConnectionString("spire_Connection");
        }

        public IActionResult Edit(long id, string search = "", int page = 1, int size = 10)
        {
            return RedirectToAction("Index", new { editId = id, search, page, size });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllWarehouses(string partNo, string skipWhse)
        {
            var warehouses = new List<object>();

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();

            var sql = @"
        SELECT i.whse, 
               COALESCE(i.current_cost,0) AS current_cost,
               COALESCE(i.average_cost,0) AS average_cost,
               COALESCE(sp.price,0) AS sell_price
        FROM inventory i
        LEFT JOIN inventory_uoms iu ON i.id = iu.inventory_id AND iu.uom = 'EA'
        LEFT JOIN inventory_sell_prices sp 
            ON iu.id = sp.uom_id AND sp.inventory_id = i.id AND sp.price_level_id = 1
        WHERE i.part_no = @partNo
          AND i.whse <> 'FR'"; // always skip FR

            if (!string.IsNullOrEmpty(skipWhse))
                sql += " AND i.whse <> @skipWhse";

            sql += " ORDER BY i.whse ASC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("partNo", partNo);
            if (!string.IsNullOrEmpty(skipWhse))
                cmd.Parameters.AddWithValue("skipWhse", skipWhse);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                warehouses.Add(new
                {
                    Whse = reader.GetString("whse"),
                    CurrentCost = reader.GetDecimal("current_cost"),
                    AverageCost = reader.GetDecimal("average_cost"),
                    SellPrice = reader.IsDBNull("sell_price") ? 0m : reader.GetDecimal("sell_price")
                });
            }

            return Json(warehouses);
        }
        public async Task<IActionResult> Index(string search = "", int page = 1, int size = 10, long? editId = null)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 5, 50);

            ViewBag.SearchTerm = search ?? "";
            ViewBag.PageSize = size;

            var model = new ModifyInventory
            {
                EditInventoryId = editId,
                SearchTerm = search ?? "",
                CurrentPage = page,
                PageSize = size
            };

            int offset = (page - 1) * size;

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();

            var countSql = @"
                SELECT COUNT(DISTINCT i.id)
                FROM inventory i
                WHERE i.whse NOT IN ('FR','ZZ')
                AND (@search = '' OR LOWER(i.part_no) LIKE LOWER(@search) OR LOWER(i.description) LIKE LOWER(@search))";

            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                countCmd.Parameters.AddWithValue("search", $"%{search}%");
                model.TotalItems = Convert.ToInt64(await countCmd.ExecuteScalarAsync());
            }

            model.TotalPages = model.TotalItems > 0 ? (int)Math.Ceiling((double)model.TotalItems / size) : 1;
            if (page > model.TotalPages) model.CurrentPage = model.TotalPages;

            var sql = @"
                SELECT DISTINCT
                    i.id AS inventory_id, i.whse, i.part_no, i.description, i.product_code,
                    COALESCE(i.current_cost, 0) AS current_cost,
                    COALESCE(i.average_cost, 0) AS average_cost,
                    COALESCE(sp.price, 0) AS sell_price,
                    iu.id AS uom_id
                FROM inventory i
                LEFT JOIN inventory_uoms iu ON i.id = iu.inventory_id AND iu.uom = 'EA'
                LEFT JOIN inventory_sell_prices sp ON iu.id = sp.uom_id 
                    AND sp.inventory_id = i.id AND sp.price_level_id = 1
                WHERE i.whse NOT IN ('FR','ZZ')
                AND (@search = '' OR LOWER(i.part_no) LIKE LOWER(@search) OR LOWER(i.description) LIKE LOWER(@search))
                ORDER BY i.part_no ASC
                LIMIT @size OFFSET @offset";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("search", $"%{search}%");
            cmd.Parameters.AddWithValue("size", size);
            cmd.Parameters.AddWithValue("offset", offset);

            model.InventoryItems.Clear();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.InventoryItems.Add(new InventoryItem
                {
                    InventoryId = reader.GetInt64("inventory_id"),
                    Whse = reader.GetString("whse"),
                    PartNo = reader.GetString("part_no"),
                    Description = reader.GetString("description"),
                    ProductCode = reader.IsDBNull("product_code") ? null : reader.GetString("product_code"),
                    CurrentCost = reader.GetDecimal("current_cost"),
                    AverageCost = reader.GetDecimal("average_cost"),
                    SellPrice = reader.IsDBNull("sell_price") ? null : (decimal?)reader.GetDecimal("sell_price"),
                    UomId = reader.IsDBNull("uom_id") ? null : (long?)reader.GetInt64("uom_id")
                });
            }

            return View("~/Areas/Inventory/Views/Inventory/ModifyInventory.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice(PriceUpdateModel model, bool applyToAll = false)
        {
            if (!ModelState.IsValid || model.CurrentCost < 0 || model.AverageCost < 0 || model.SellPrice < 0)
            {
                TempData["Error"] = "Prices cannot be negative";
                return RedirectToAction("Index");
            }

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // ✅ Update inventory
                string whseFilter = applyToAll ? "" : "AND whse = @whse";
                var invSql = $@"
                    UPDATE inventory
                    SET current_cost = @current,
                        average_cost = @avg,
                        _modified = NOW()
                    WHERE part_no = @partNo
                    {whseFilter}
                    RETURNING id, whse";

                using var invCmd = new NpgsqlCommand(invSql, conn, transaction);
                invCmd.Parameters.Add(new NpgsqlParameter("partNo", NpgsqlDbType.Text) { Value = model.PartNo });
                if (!applyToAll)
                    invCmd.Parameters.Add(new NpgsqlParameter("whse", NpgsqlDbType.Text) { Value = model.Whse });
                invCmd.Parameters.Add(new NpgsqlParameter("current", NpgsqlDbType.Numeric) { Value = model.CurrentCost });
                invCmd.Parameters.Add(new NpgsqlParameter("avg", NpgsqlDbType.Numeric) { Value = model.AverageCost });

                var updatedWhses = new List<string>();
                using var reader = await invCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    updatedWhses.Add(reader.GetString("whse"));
                reader.Close();

                // ✅ Update or insert sell prices
                foreach (var wh in updatedWhses)
                {
                    var spSqlUpdate = @"
                        UPDATE inventory_sell_prices
                        SET price = @price, _modified = NOW()
                        WHERE inventory_id = (SELECT id FROM inventory WHERE part_no = @partNo AND whse = @whse)
                          AND price_level_id = 1
                        RETURNING id";

                    using var spCmdUpdate = new NpgsqlCommand(spSqlUpdate, conn, transaction);
                    spCmdUpdate.Parameters.Add(new NpgsqlParameter("partNo", NpgsqlDbType.Text) { Value = model.PartNo });
                    spCmdUpdate.Parameters.Add(new NpgsqlParameter("whse", NpgsqlDbType.Text) { Value = wh });
                    spCmdUpdate.Parameters.Add(new NpgsqlParameter("price", NpgsqlDbType.Numeric) { Value = model.SellPrice });

                    var spUpdated = await spCmdUpdate.ExecuteScalarAsync();

                    if (spUpdated == null)
                    {
                        var spSqlInsert = @"
                            INSERT INTO inventory_sell_prices (inventory_id, uom_id, price_level_id, price, _created)
                            VALUES ((SELECT id FROM inventory WHERE part_no = @partNo AND whse = @whse), @uomId, 1, @price, NOW())
                            RETURNING id";

                        using var spCmdInsert = new NpgsqlCommand(spSqlInsert, conn, transaction);
                        spCmdInsert.Parameters.Add(new NpgsqlParameter("partNo", NpgsqlDbType.Text) { Value = model.PartNo });
                        spCmdInsert.Parameters.Add(new NpgsqlParameter("whse", NpgsqlDbType.Text) { Value = wh });
                        spCmdInsert.Parameters.Add(new NpgsqlParameter("uomId", NpgsqlDbType.Bigint) { Value = model.UomId ?? throw new Exception("UOM ID required") });
                        spCmdInsert.Parameters.Add(new NpgsqlParameter("price", NpgsqlDbType.Numeric) { Value = model.SellPrice });

                        await spCmdInsert.ExecuteScalarAsync();
                    }
                }

                await transaction.CommitAsync();
                TempData["Success"] = applyToAll
                    ? $"✅ Prices updated for all warehouses of {model.PartNo}"
                    : $"✅ Prices updated for {model.PartNo} in {model.Whse}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"❌ {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
