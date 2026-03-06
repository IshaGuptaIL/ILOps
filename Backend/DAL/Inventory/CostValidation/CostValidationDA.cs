using DAL.Common.Login;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Inventory.CostValidation
{
    
        public class CostValidationDA : ICostValidation
        {
            private readonly string _sqlConn;
            private readonly string _pgConn;

            public CostValidationDA(IConfiguration configuration)
            {
                _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
                _pgConn = configuration.GetConnectionString("spire_Connection");
            }

        private List<Dictionary<string, object>> DataTableToList(DataTable dt)
        {
            return dt.AsEnumerable().Select(row =>
                dt.Columns.Cast<DataColumn>()
                 .ToDictionary(col => col.ColumnName, col => row[col])
            ).ToList();
        }

        public async Task<ApiResposne> LoadHPC(Stream excelStream)
        {
            var response = new ApiResposne();

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                DataTable raw = new DataTable();

                // ================= READ EXCEL =================
                using (var pkg = new ExcelPackage(excelStream))
                {
                    var ws = pkg.Workbook.Worksheets[0];
                    if (ws.Dimension == null) throw new Exception("Excel sheet is empty");

                    // Create columns based on Excel Headers
                    for (int c = 1; c <= ws.Dimension.End.Column; c++)
                    {
                        string header = ws.Cells[1, c].Text.Trim();
                        if (!string.IsNullOrEmpty(header))
                            raw.Columns.Add(header);
                    }

                    // Add rows data
                    for (int r = 2; r <= ws.Dimension.End.Row; r++)
                    {
                        var dr = raw.NewRow();
                        for (int c = 1; c <= raw.Columns.Count; c++)
                        {
                            dr[c - 1] = ws.Cells[r, c].Text.Trim();
                        }
                        raw.Rows.Add(dr);
                    }
                }

                // ================= VALIDATION =================
                DataTable valid = raw.Clone();
                DataTable invalid = new DataTable();
                invalid.Columns.Add("RowNumber");
                invalid.Columns.Add("Part");
                invalid.Columns.Add("Column");
                invalid.Columns.Add("Value");
                invalid.Columns.Add("Reason");

                int excelRow = 2;

                foreach (DataRow r in raw.Rows)
                {
                    bool isValid = true;
                    string currentPart = r["Part"]?.ToString();

                    // 1. Part Number Validation
                    if (string.IsNullOrWhiteSpace(currentPart))
                    {
                        invalid.Rows.Add(excelRow, "", "Part", "", "Part is blank");
                        isValid = false;
                    }

                    // 2. RogersCost Validation (Numerical check)
                    if (!decimal.TryParse(r["RogersCost"]?.ToString(), out _))
                    {
                        invalid.Rows.Add(excelRow, currentPart, "RogersCost", r["RogersCost"], "Cost must be numeric");
                        isValid = false;
                    }

                    // 3. StartDate Validation (Flexible date check)
                    if (!DateTime.TryParse(r["StartDate"]?.ToString(), out _))
                    {
                        invalid.Rows.Add(excelRow, currentPart, "StartDate", r["StartDate"], "Invalid date format in StartDate");
                        isValid = false;
                    }

                    // 4. DelistDate Validation (Optional check)
                    var delistVal = r["DelistDate"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(delistVal) && !DateTime.TryParse(delistVal, out _))
                    {
                        invalid.Rows.Add(excelRow, currentPart, "DelistDate", delistVal, "Invalid date format in DelistDate");
                        isValid = false;
                    }

                    if (isValid) valid.ImportRow(r);
                    excelRow++;
                }

                // ================= STOP IF INVALID =================
                if (invalid.Rows.Count > 0)
                {
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.Result = new
                    {
                        ValidRows = DataTableToList(valid),
                        InvalidRows = DataTableToList(invalid),
                        InsertedCount = 0,
                        FailedCount = invalid.Rows.Count
                    };
                    return response;
                }

                // ================= DATABASE OPERATIONS =================
                await ExecuteNonQueryAsync("DELETE FROM HPCExtract");
                await ExecuteNonQueryAsync("DELETE FROM HPCExtractSummary");

                // Bulk Insert into HPCExtract
                using (var bulk = new SqlBulkCopy(_sqlConn))
                {
                    bulk.DestinationTableName = "HPCExtract";
                    // Map Excel Headers -> Database Columns
                    bulk.ColumnMappings.Add("Part", "SKU");
                    bulk.ColumnMappings.Add("RogersCost", "DealerCost");
                    bulk.ColumnMappings.Add("StartDate", "DropDate");
                    bulk.ColumnMappings.Add("DelistDate", "DelistedDate");

                    await bulk.WriteToServerAsync(valid);
                }

                // Summary Insert into HPCExtractSummary
                using (var con = new SqlConnection(_sqlConn))
                {
                    await con.OpenAsync();
                    foreach (DataRow r in valid.Rows)
                    {
                        using var cmd = new SqlCommand(@"
                    INSERT INTO HPCExtractSummary (Whse, Part, Cost, MaxOfF3, DelistDate)
                    VALUES ('CO', @Part, @Cost, @Date, @Delist)", con);

                        cmd.Parameters.AddWithValue("@Part", r["Part"]);
                        cmd.Parameters.AddWithValue("@Cost", Math.Round(decimal.Parse(r["RogersCost"].ToString()), 2));
                        cmd.Parameters.AddWithValue("@Date", DateTime.Parse(r["StartDate"].ToString()));
                        cmd.Parameters.AddWithValue("@Delist", string.IsNullOrWhiteSpace(r["DelistDate"]?.ToString())
                            ? (object)DBNull.Value : DateTime.Parse(r["DelistDate"].ToString()));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                response.Success = true;
                response.Message = "Upload successful";
                response.Result = new
                {
                    ValidRows = DataTableToList(valid),
                    InvalidRows = DataTableToList(invalid),
                    InsertedCount = valid.Rows.Count,
                    FailedCount = 0
                };
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }




        public async Task<List<HpcRecord>> GetHpcLatestAsync()
        {
            var dt = GetSqlServerData(@"
                SELECT Whse, Part, MaxOfF3 AS StartDate, Cost AS RogersCost, DelistDate
                FROM HPCExtractSummary
                ORDER BY Whse, Part
            ");

            var result = dt.AsEnumerable().Select(r => new HpcRecord
            {
                Whse = r["Whse"].ToString(),
                Part = r["Part"].ToString(),
                StartDate = (DateTime)r.Field<DateTime?>("StartDate"),
                RogersCost = Convert.ToDecimal(r["RogersCost"]),
                ExistInSpire = "Yes" // For Latest view, all exist
            }).ToList();

            return await Task.FromResult(result);
        }





        public async Task<List<HpcRecord>> GetHpcDiscrepanciesAsync()
        {
            DataTable dtSql = GetSqlServerData("SELECT * FROM HPCExtractSummary");
            DataTable dtPg = GetPostgresData(@"
                SELECT part_no, whse, description, product_code,
                       current_cost, onhand_qty, purchase_qty
                FROM inventory
            ");

            var list = new List<HpcRecord>();

            foreach (DataRow h in dtSql.Rows)
            {
                string part = h["Part"].ToString();
                string whse = h["Whse"].ToString();

                DataRow pg = dtPg.AsEnumerable()
                    .FirstOrDefault(r =>
                        r["part_no"].ToString() == part &&
                        r["whse"].ToString() == whse);

                var record = new HpcRecord
                {
                    Whse = whse,
                    Part = part,
                    Description = pg?["description"]?.ToString(),
                    StartDate = (DateTime)h.Field<DateTime?>("MaxOfF3"),
                    SpireProdCode = pg?["product_code"]?.ToString() ?? "",
                    RogersCost = Convert.ToDecimal(h["Cost"]),
                    SpireCost = pg?["current_cost"] as decimal?,
                    ExistInSpire = pg == null ? "No" : "Yes",
                    OnhandQty = pg?["onhand_qty"] as decimal?,
                    PurchaseQty = pg?["purchase_qty"] as decimal?
                };

                if (record.ExistInSpire == "No" ||
                    Math.Round(record.RogersCost, 2) != Math.Round(record.SpireCost ?? 0, 2) ||
                    record.SpireProdCode != "HCC")
                {
                    list.Add(record);
                }
            }

            return await Task.FromResult(list);
        }

        //public async Task<List<HpcRecord>> GetHpcDiscrepanciesAsync()
        //{
        //    DataTable dtSql = GetSqlServerData("SELECT * FROM HPCExtractSummary");
        //    DataTable dtPg = GetPostgresData(@"
        //SELECT part_no, whse, description, product_code, 
        //       current_cost, onhand_qty, purchase_qty 
        //FROM inventory");

        //    // LINQ Join use karein - Yeh bilkul VBA ke Left Join jaisa hai
        //    var query = from h in dtSql.AsEnumerable()
        //                join p in dtPg.AsEnumerable()
        //                    on new { P = h["Part"].ToString().Trim().ToUpper(), W = h["Whse"].ToString().Trim().ToUpper() }
        //                    equals new { P = p["part_no"].ToString().Trim().ToUpper(), W = p["whse"].ToString().Trim().ToUpper() }
        //                    into joined
        //                from pg in joined.DefaultIfEmpty() // Yeh line isse LEFT JOIN banati hai
        //                select new HpcRecord
        //                {
        //                    Whse = h["Whse"].ToString(),
        //                    Part = h["Part"].ToString(),
        //                    Description = pg?["description"]?.ToString(),
        //                    StartDate = h["MaxOfF3"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(h["MaxOfF3"]),
        //                    SpireProdCode = pg?["product_code"]?.ToString() ?? "",
        //                    RogersCost = Convert.ToDecimal(h["Cost"]),
        //                    // Safe decimal conversion
        //                    SpireCost = pg == null ? 0 : Convert.ToDecimal(pg["current_cost"]),
        //                    ExistInSpire = pg == null ? "No" : "Yes",
        //                    OnhandQty = pg == null ? 0 : Convert.ToDecimal(pg["onhand_qty"]),
        //                    PurchaseQty = pg == null ? 0 : Convert.ToDecimal(pg["purchase_qty"])
        //                };

        //    // Filter apply karein jaisa VBA ki WHERE clause mein tha
        //    var result = query.Where(r =>
        //        r.ExistInSpire == "No" ||
        //        Math.Round(r.RogersCost, 2) != Math.Round(r.SpireCost ?? 0, 2) ||
        //        r.SpireProdCode != "HCC"
        //    ).OrderBy(r => r.Whse).ThenBy(r => r.Part).ToList();

        //    return result;
        //}
        public async Task<List<CostVarianceCurrentVsAvg>> GetCostVarianceCurrentVsAvgAsync()
        {
            DataTable dt = GetPostgresData(@"
        SELECT
            whse,
            part_no,
            description,
            current_cost,
            average_cost
        FROM inventory
        WHERE whse <> 'FR'
          AND whse <> 'COADV'
          AND product_code = 'HCC'
          AND current_cost <> average_cost
        ORDER BY whse, part_no
    ");

            var result = dt.AsEnumerable()
                .Select(r => new CostVarianceCurrentVsAvg
                {
                    Whse = r["whse"].ToString(),
                    PartNo = r["part_no"].ToString(),
                    Description = r["description"].ToString(),
                    CurrentCost = Math.Round(r.Field<decimal?>("current_cost") ?? 0, 2),
                    AverageCost = Math.Round(r.Field<decimal?>("average_cost") ?? 0, 2)
                })
                .ToList();

            return await Task.FromResult(result);
        }




        public async Task<List<CostVarianceAcrossWarehouses>> GetCostVarianceAcrossWarehousesAsync()
        {
            DataTable dt = GetPostgresData(@"
        SELECT
            i.part_no,
            i.whse,
            i.description,
            i.current_cost,
            i.average_cost
        FROM inventory i
        INNER JOIN (
            SELECT part_no
            FROM inventory
            WHERE product_code = 'HCC'
              AND whse <> 'FR'
              AND whse <> 'COADV'
            GROUP BY part_no
            HAVING
                   MIN(current_cost)  <> MAX(current_cost)
                OR MIN(average_cost)  <> MAX(average_cost)
                OR MIN(current_cost)  <> MAX(average_cost)
                OR MIN(average_cost)  <> MAX(current_cost)
        ) v ON v.part_no = i.part_no
        WHERE i.whse <> 'FR'
          AND i.whse <> 'COADV'
        ORDER BY i.part_no, i.whse
    ");

            var result = dt.AsEnumerable()
                .Select(r => new CostVarianceAcrossWarehouses
                {
                    PartNo = r["part_no"].ToString(),
                    Whse = r["whse"].ToString(),
                    Description = r["description"].ToString(),
                    CurrentCost = Math.Round(r.Field<decimal?>("current_cost") ?? 0, 2),
                    AverageCost = Math.Round(r.Field<decimal?>("average_cost") ?? 0, 2)
                })
                .ToList();

            return await Task.FromResult(result);
        }

        public async Task<List<HardwareVsSpire>> GetRDHardwareVsSpireAsync()
        {
            DataTable dtHardware = GetSqlServerData(@"
                SELECT hardwareID, bv_part_number, model, dealer_cost
                FROM t_hardware
                WHERE bv_part_number IS NOT NULL
            ");

            DataTable dtInventory = GetPostgresData(@"
                SELECT part_no, description, current_cost, product_code, last_sale_date
                FROM inventory
                WHERE whse = 'CO'
            ");

            var joined = from h in dtHardware.AsEnumerable()
                         join i in dtInventory.AsEnumerable()
                             on h.Field<string>("bv_part_number").Trim().ToUpper()
                             equals i.Field<string>("part_no").Trim().ToUpper()
                         orderby h.Field<string>("bv_part_number")
                         select new HardwareVsSpire
                         {
                             hardwareID = h.Field<int>("hardwareID"),
                             spirePartNumber = h.Field<string>("bv_part_number"),
                             model = h.Field<string>("model"),
                             spireDescription = i.Field<string>("description"),
                             rDDealerCost = h.Field<decimal?>("dealer_cost") ?? 0,
                             spireCurrentCost = i.Field<decimal?>("current_cost") ?? 0,
                             productCode = i.Field<string>("product_code"),
                             lastSaleDate = i.Field<DateTime?>("last_sale_date")
                         };

            return await Task.FromResult(joined.ToList());
        }

        // ========================= HELPER FUNCTIONS =========================
        private DataTable GetSqlServerData(string sql)
        {
            DataTable dt = new DataTable();
            using var con = new SqlConnection(_sqlConn);
            using var da = new SqlDataAdapter(sql, con);
            da.Fill(dt);
            return dt;
        }


        private async Task ExecuteNonQueryAsync(string sql)
        {
            using var con = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, con);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        private DataTable GetPostgresData(string sql)
        {
            DataTable dt = new DataTable();
            using var con = new NpgsqlConnection(_pgConn);
            using var da = new NpgsqlDataAdapter(sql, con);
            da.Fill(dt);
            return dt;
        }
    }
}
