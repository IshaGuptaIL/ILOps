using DAL.Common.Login;
using DAL.Inventory.InventoryType;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Channels;
using static Azure.Core.HttpHeader;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DAL.Inventory.ModifyInventory
{
    public class ModifyInventoryDA : IModifyInventory
    {

        private readonly string _conn;

        public ModifyInventoryDA(IConfiguration config)
        {
            _conn = config.GetConnectionString("spire_Connection");
        }

        // ================= INVENTORY LIST =================
        public async Task<ModifyInventoryBO> GetInventoryAsync(string search, int page, int size)
        {
            var model = new ModifyInventoryBO
            {
                SearchTerm = search,
                CurrentPage = page,
                PageSize = size
            };

            int offset = (page - 1) * size;

            using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var countSql = @"
                SELECT COUNT(*)
                FROM inventory
                WHERE whse NOT IN ('FR','ZZ')
                AND (@search = '' OR part_no ILIKE @search OR description ILIKE @search)";

            using var countCmd = new NpgsqlCommand(countSql, conn);
            countCmd.Parameters.AddWithValue("search", $"%{search}%");
            model.TotalItems = (long)await countCmd.ExecuteScalarAsync();

            model.TotalPages = (int)Math.Ceiling((double)model.TotalItems / size);

            var sql = @"
                SELECT i.id, i.whse, i.part_no, i.description, i.product_code,
                       COALESCE(i.current_cost,0),
                       COALESCE(i.average_cost,0),
                       COALESCE(sp.price,0),
                       iu.id AS uom_id,
 i.sales_acct 
                FROM inventory i
                LEFT JOIN inventory_uoms iu ON i.id = iu.inventory_id AND iu.uom='EA'
                LEFT JOIN inventory_sell_prices sp 
                       ON sp.inventory_id=i.id AND sp.uom_id=iu.id AND sp.price_level_id=1
                WHERE i.whse NOT IN ('FR','ZZ','00','DUMMY','GR','SLTEST','ROGOR')
                AND (@search = '' OR i.part_no ILIKE @search OR i.description ILIKE @search)
                ORDER BY i.part_no
                LIMIT @size OFFSET @offset";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("search", $"%{search}%");
            cmd.Parameters.AddWithValue("size", size);
            cmd.Parameters.AddWithValue("offset", offset);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.InventoryItems.Add(new InventoryItemBO
                {
                    InventoryId = reader.GetInt64(0),
                    Whse = reader.GetString(1),
                    PartNo = reader.GetString(2),
                    Description = reader.GetString(3),
                    ProductCode = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CurrentCost = reader.GetDecimal(5),
                    AverageCost = reader.GetDecimal(6),
                    SellPrice = reader.GetDecimal(7),
                    UomId = reader.IsDBNull(8) ? null : reader.GetInt64(8),
                    SalesAcct = reader.IsDBNull(9) ? 0 : reader.GetInt16(9)
                });
            }

            return model;
        }

        // ================= ALL WAREHOUSES =================
        public async Task<List<WarehousePriceBO>> GetAllWarehousesAsync(string partNo, string skipWhse)
        {
            var list = new List<WarehousePriceBO>();

            using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = @"
                SELECT i.whse, COALESCE(i.current_cost,0),
                       COALESCE(i.average_cost,0),
                       COALESCE(sp.price,0)
                FROM inventory i
                LEFT JOIN inventory_uoms iu ON i.id = iu.inventory_id AND iu.uom='EA'
                LEFT JOIN inventory_sell_prices sp 
                     ON sp.inventory_id=i.id AND sp.uom_id=iu.id AND sp.price_level_id=1
                WHERE i.part_no=@partNo
                AND i.whse <> 'FR'
                AND (@skipWhse='' OR i.whse<>@skipWhse)
                ORDER BY i.whse";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("partNo", partNo);
            cmd.Parameters.AddWithValue("skipWhse", skipWhse ?? "");

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new WarehousePriceBO
                {
                    Whse = reader.GetString(0),
                    CurrentCost = reader.GetDecimal(1),
                    AverageCost = reader.GetDecimal(2),
                    SellPrice = reader.GetDecimal(3)
                });
            }

            return list;
        }

        // ================= UPDATE PRICE =================

        public async Task<ApiResposne> UpdatePriceAsync(PriceUpdateModel model, bool applyToAll)
        {
            var response = new ApiResposne();

            if (model.CurrentCost < 0 || model.AverageCost < 0 || model.SellPrice < 0)
            {
                response.Success = false;
                response.StatusCode = 400;
                response.Message = "Prices cannot be negative";
                return response;
            }

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                string whseFilter = applyToAll ? "" : "AND whse = @whse";

                // 1️⃣ Update inventory table
                var invSql = $@"
            UPDATE inventory
            SET current_cost = @current,
                average_cost = @avg,
  product_code = @productCode,
    sales_acct = @salesAcct,
                _modified = NOW()
            WHERE part_no = @partNo
            {whseFilter}
            RETURNING id";

                var inventoryIds = new List<long>();

                await using (var invCmd = new NpgsqlCommand(invSql, conn, tx))
                {
                    invCmd.Parameters.AddWithValue("partNo", model.PartNo);
                    invCmd.Parameters.AddWithValue("current", model.CurrentCost);
                    invCmd.Parameters.AddWithValue("avg", model.AverageCost);
                    invCmd.Parameters.AddWithValue("productCode", model.ProductCode ?? "");
                    invCmd.Parameters.AddWithValue("salesAcct", model.SalesAcct);

                    if (!applyToAll)
                        invCmd.Parameters.AddWithValue("whse", model.Whse);

                    await using var reader = await invCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        inventoryIds.Add(reader.GetInt64(0));
                    }
                }

                // 2️⃣ Update / Insert into inventory_sell_prices
                foreach (var inventoryId in inventoryIds)
                {
                    var updateSql = @"
                UPDATE inventory_sell_prices
                SET price = @price,
                    _modified = NOW()
                WHERE inventory_id = @inventoryId
                  AND price_level_id = 1
                RETURNING id";

                    await using var updCmd = new NpgsqlCommand(updateSql, conn, tx);
                    updCmd.Parameters.AddWithValue("inventoryId", inventoryId);
                    updCmd.Parameters.AddWithValue("price", model.SellPrice);

                    var updatedId = await updCmd.ExecuteScalarAsync();

                    // If no row updated → insert new one
                    if (updatedId == null)
                    {
                        var insertSql = @"
                    INSERT INTO inventory_sell_prices
                        (inventory_id, uom_id, price_level_id, price, _created)
                    VALUES
                        (@inventoryId, @uomId, 1, @price, NOW())";

                        await using var insCmd = new NpgsqlCommand(insertSql, conn, tx);
                        insCmd.Parameters.AddWithValue("inventoryId", inventoryId);
                        insCmd.Parameters.AddWithValue("uomId", model.UomId
                            ?? throw new Exception("UOM ID required for new price"));
                        insCmd.Parameters.AddWithValue("price", model.SellPrice);

                        await insCmd.ExecuteNonQueryAsync();
                    }
    //            var legacyUpdateSql = @"
    //UPDATE inventory_uoms
    //SET sell_prices[1] = @price
    //WHERE inventory_id=@inventoryId AND uom='EA'";

    //            await using (var cmd = new NpgsqlCommand(legacyUpdateSql, conn, tx))
    //            {
    //                cmd.Parameters.AddWithValue("inventoryId", inventoryId);
    //                cmd.Parameters.AddWithValue("price", model.SellPrice);
    //                await cmd.ExecuteNonQueryAsync();
    //            }
                }


                await tx.CommitAsync();

                response.Success = true;
                response.StatusCode = 200;
                response.Message = applyToAll
                    ? $"Prices updated for all warehouses of {model.PartNo}"
                    : $"Prices updated for {model.PartNo} in {model.Whse}";
                response.Count = inventoryIds.Count;

                return response;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                response.Success = false;
                response.StatusCode = 500;
                response.Message = "Database Error: " + ex.Message;
                return response;
            }
        }

    }
}
  