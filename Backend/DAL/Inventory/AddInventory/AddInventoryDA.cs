using DAL.Common.Login;
using DAL.Common.Spire;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DAL.Inventory.AddInventory
{
    public class AddInventoryDA : IAddInventory
    {
        private readonly AppDBContext _dbContext;
        private readonly string _pgConnString;
        private readonly SpireDA _spire;

        public AddInventoryDA(AppDBContext context, SpireDA spire)
        {
            _dbContext = context;
            _spire = spire;
            _pgConnString = spire.PgConnString; // Make sure PgConnString is public in SpireApiHelper
        }




        // ============================
        // Get Warehouses (Postgres)
        // ============================
        public async Task<List<WarehouseBO>> GetWarehousesAsync(int? userRoleId)
        {
            const string baseSql = @"
        SELECT whse, description 
        FROM inventory_warehouses 
        WHERE whse IS NOT NULL";

            var list = new List<WarehouseBO>();

            // ✅ RoleId 1 = Only CO warehouse
            string finalSql = userRoleId == 1
                ? baseSql + " AND whse = 'CO' ORDER BY id"
                : baseSql + " ORDER BY id";

            await using var con = new NpgsqlConnection(_pgConnString);
            await using var cmd = new NpgsqlCommand(finalSql, con);

            // ✅ Pass RoleId to method
            if (userRoleId.HasValue)
                cmd.Parameters.AddWithValue("userRoleId", userRoleId.Value);

            await con.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new WarehouseBO
                {
                    Whse = rdr["whse"].ToString(),
                    Description = rdr["description"].ToString()
                });
            }

            return list;
        }

        // ============================
        // Get Manufacturers (SQL Server)
        // ============================
        public async Task<List<ManufacturerBO>> GetManufacturersAsync()
        {
            const string sql = @"
                SELECT Id, Name, InventoryType 
                FROM tblMan 
                WHERE Name IS NOT NULL 
                ORDER BY Name";

            var list = new List<ManufacturerBO>();

            await using var conn =
                new SqlConnection(_dbContext.Database.GetConnectionString());

            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new ManufacturerBO
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    InventoryType = reader.GetString(reader.GetOrdinal("InventoryType"))
                });
            }

            return list;
        }

        public async Task<ApiResposne> CheckPartNo(string partNo, string whse)
        {
            if (string.IsNullOrWhiteSpace(partNo) || string.IsNullOrWhiteSpace(whse))
            {
                return new ApiResposne
                {
                    Success = false,
                    StatusCode = 400,
                    Message = "PartNo or Warehouse missing"
                };
            }

            try
            {
                await using var conn = new NpgsqlConnection(_pgConnString);
                await using var cmd = new NpgsqlCommand(
                    @"SELECT 1 FROM public.inventory WHERE part_no = @partNo AND whse = @whse LIMIT 1",
                    conn);

                cmd.Parameters.AddWithValue("@partNo", partNo.ToUpper());
                cmd.Parameters.AddWithValue("@whse", whse);

                await conn.OpenAsync();
                var exists = await cmd.ExecuteScalarAsync() != null;

                return new ApiResposne
                {
                    Success = true,
                    StatusCode = 200,
                    Result = new { exists = exists },
                    Message = exists ? "Part number exists" : "Part number available"
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne
                {
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }



        private void ValidateBusinessRules(AddInventoryBO model)
        {
            if (model.Type == "Hardware")
            {
                if (model.ProductCode != "HCC")
                    throw new Exception("Product Code must be HCC for Hardware");

                if (model.SalesDept != 4)
                    throw new Exception("Sales Dept must be 4 for Hardware");

                if (string.IsNullOrWhiteSpace(model.AccessoryGroup))
                    throw new Exception("Manufacturer required for Hardware");
            }

            if (model.Type == "Accessory")
            {
                if (model.ProductCode != "ACC")
                    throw new Exception("Product Code must be ACC for Accessory");

                if (model.SalesDept != 5)
                    throw new Exception("Sales Dept must be 5 for Accessory");
            }

            if (model.Description?.Length > 80)
                throw new Exception("English description must be max 80 characters");

            if (model.FrDescription?.Length > 80)
                throw new Exception("French description must be max 80 characters");
        }

        // =========================
        // Add inventory item
        // =========================
        public async Task<ApiResposne> AddInventoryItemAsync(AddInventoryBO model)
        {
            try
            {
                ValidateBusinessRules(model);
                // ======= Step 1: Check if PartNo already exists =======
                var checkResp = await CheckPartNo(model.PartNo, model.Whse!);
                var checkResult = JsonSerializer.Serialize(checkResp.Result);
                var frExists = await _spire.PartExistsAsync(model.PartNo!, "FR");

                if (frExists)
                {
                    return new ApiResposne
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = $"Part number '{model.PartNo}' already exists in warehouse 'FR'"
                    };
                }
                using var jsonDoc = JsonDocument.Parse(checkResult);
                bool exists = jsonDoc.RootElement.GetProperty("exists").GetBoolean();

                if (exists)
                {
                    return new ApiResposne
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = $"Part number '{model.PartNo}' already exists in warehouse '{model.Whse}'"
                    };
                }

                // Build EN Spire item
                var enItem = BuildSpireItemRequest(model.Whse!, model.Description!, model);
                var enResp = await _spire.SendInventoryItemAsync(enItem, "inventory/items/");

                if (enResp.HttpStatus != 201 && enResp.HttpStatus != 200)
                    return new ApiResposne
                    {
                        Success = false,
                        StatusCode = enResp.HttpStatus,
                        Message = $"Spire EN failed: {enResp.HttpStatusText}"
                    };

                // Build FR Spire item
                var frItem = BuildSpireItemRequest("FR", model.FrDescription!, model);
                var frResp = await _spire.SendInventoryItemAsync(frItem, "inventory/items/");

                if (frResp.HttpStatus != 201 && frResp.HttpStatus != 200)
                    return new ApiResposne
                    {
                        Success = false,
                        StatusCode = frResp.HttpStatus,
                        Message = $"Spire FR failed: {frResp.HttpStatusText}"
                    };

                // Save to Postgres (salesDept removed)
                var pgArray = new[]
                {
                new {
                    whse = model.Whse,
                    partNo = model.PartNo,
                    description = model.Description,
                    currentCost = model.CostPrice,
                    averageCost = model.CostPrice,
                    serialized = (model.Type == "Hardware"),
                    //userDef1 = model.AccessoryGroup,
                    allowBackorders = false
                },
                new {
                    whse = "FR",
                    partNo = model.PartNo,
                    description = model.FrDescription,
                    currentCost = model.CostPrice,
                    averageCost = model.CostPrice,
                    serialized = (model.Type == "Hardware"),
                    //userDef1 = model.AccessoryGroup,
                    allowBackorders = false
                }
            };

                var pgJson = JsonSerializer.Serialize(pgArray);
                var pgSaved = await SaveInventoryToPostgresAsync(pgJson);

                return new ApiResposne
                {
                    Success = true,
                    StatusCode = 200,
                    Result = new
                    {
                        SpireEnKey = enResp.HeaderKey,
                        SpireFrKey = frResp.HeaderKey,
                        PgSaved = pgSaved
                    },
                    Message = "Inventory added successfully!"
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne
                {
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }

        // =========================
        // Save inventory array to Postgres
        // =========================
        public async Task<bool> SaveInventoryToPostgresAsync(string jsonData)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_pgConnString);
                await conn.OpenAsync();

                using var doc = JsonDocument.Parse(jsonData);

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    // --- REQUIRED FIELDS CHECK ---
                    if (!el.TryGetProperty("whse", out var whseProp) || string.IsNullOrWhiteSpace(whseProp.GetString()))
                    {
                        continue;
                    }

                    if (!el.TryGetProperty("partNo", out var partNoProp) || string.IsNullOrWhiteSpace(partNoProp.GetString()))
                    {
                        continue;
                    }

                    using var cmd = new NpgsqlCommand(@"
INSERT INTO public.inventory
(whse, part_no, description, current_cost, average_cost, serialized, allow_back_orders)
VALUES
(@whse, @part_no, @description, @current_cost, @average_cost, @serialized, @allow_back_orders)
ON CONFLICT (whse, part_no)
DO UPDATE SET
    description = EXCLUDED.description,
    current_cost = EXCLUDED.current_cost,
    average_cost = EXCLUDED.average_cost,
    serialized = EXCLUDED.serialized,
    allow_back_orders = EXCLUDED.allow_back_orders;
", conn);

                    // --- PARAMETER MAPPING ---
                    cmd.Parameters.AddWithValue("@whse", whseProp.GetString()!);
                    cmd.Parameters.AddWithValue("@part_no", partNoProp.GetString()!);
                    cmd.Parameters.AddWithValue("@description",
                        el.TryGetProperty("description", out var desc) ? desc.GetString() ?? (object)DBNull.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@current_cost",
    el.TryGetProperty("currentCost", out var cc) ? cc.GetDecimal() : 0.00m);

                    cmd.Parameters.AddWithValue("@average_cost",
                        el.TryGetProperty("averageCost", out var ac) ? ac.GetDecimal() : 0.00m);
                    cmd.Parameters.AddWithValue("@serialized",
                        el.TryGetProperty("serialized", out var ser) ? ser.GetBoolean() : false);
                    cmd.Parameters.AddWithValue("@allow_back_orders",
                        el.TryGetProperty("allowBackorders", out var abo) ? abo.GetBoolean() : false);

                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        // =========================
        // Build Spire inventory request
        // =========================
        private SpireInventoryItemRequest BuildSpireItemRequest(string whse, string description, AddInventoryBO model)
        {
            var item = new SpireInventoryItemRequest
            {
                whse = whse,
                partNo = model.PartNo!,
                description = description?.Length > 80 ? description[..80] : description ?? string.Empty,
                currentCost = model.CostPrice,
                averageCost = model.CostPrice,
                //userDef1 = model.AccessoryGroup,

                allowBackorders = false,
                pricing = new Dictionary<string, SpireInventoryPricingDetail>()
            };
            item.userDef1 = model.AccessoryGroup;
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
                    item.serialized = false;
                    break;
                //case "License":
                //    item.groupNo = "ACC";
                //    item.salesDept = 5;
                //    item.serialized = true;
                //    break;
            }

            // Pricing
            if (model.SellingPrice.HasValue)
            {
                var priceDetail = new SpireInventoryPricingDetail();
                priceDetail.sellPrices.Add(model.SellingPrice.Value);
                item.pricing["EA"] = priceDetail;
            }

            return item;
        }
    }
}