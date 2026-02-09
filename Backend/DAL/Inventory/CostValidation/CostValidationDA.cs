using DAL.Common.Login;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
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




        public async Task<ApiResposne> LoadHPC(Stream excelStream)
        {
            var response = new ApiResposne();

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                DataTable raw = new DataTable();

                using (var pkg = new ExcelPackage(excelStream))
                {
                    var ws = pkg.Workbook.Worksheets[0];

                    foreach (var c in ws.Cells[1, 1, 1, ws.Dimension.End.Column])
                        raw.Columns.Add(c.Text);

                    for (int r = 2; r <= ws.Dimension.End.Row; r++)
                    {
                        var dr = raw.NewRow();
                        for (int c = 1; c <= ws.Dimension.End.Column; c++)
                            dr[c - 1] = ws.Cells[r, c].Text;

                        raw.Rows.Add(dr);
                    }
                }

                // ===== VALIDATION (same as your code) =====
                DataTable valid = raw.Clone();
                DataTable invalid = new DataTable();

                invalid.Columns.Add("RowNumber");
                invalid.Columns.Add("SKU");
                invalid.Columns.Add("Column");
                invalid.Columns.Add("Value");
                invalid.Columns.Add("Reason");

                int excelRow = 2;

                foreach (DataRow r in raw.Rows)
                {
                    bool isValid = true;

                    if (string.IsNullOrWhiteSpace(r["SKU"]?.ToString()))
                    {
                        invalid.Rows.Add(excelRow, "", "SKU", "", "SKU is blank");
                        isValid = false;
                    }

                    if (!decimal.TryParse(r["Dealer Cost"]?.ToString(), out _))
                    {
                        invalid.Rows.Add(
                            excelRow,
                            r["SKU"],
                            "Dealer Cost",
                            r["Dealer Cost"],
                            "Dealer Cost must be numeric"
                        );
                        isValid = false;
                    }

                    if (isValid)
                        valid.ImportRow(r);

                    excelRow++;
                }

                if (invalid.Rows.Count > 0)
                {
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.Result = invalid;
                    return response;
                }

                await ExecuteNonQueryAsync("DELETE FROM HPCExtract");
                await ExecuteNonQueryAsync("DELETE FROM HPCExtractSummary");

                using (var bulk = new SqlBulkCopy(_sqlConn))
                {
                    bulk.DestinationTableName = "HPCExtract";
                    bulk.ColumnMappings.Add("SKU", "SKU");
                    bulk.ColumnMappings.Add("Dealer Cost", "DealerCost");
                    bulk.ColumnMappings.Add("Drop Date", "DropDate");
                    bulk.ColumnMappings.Add("Delisted Date", "DelistedDate");

                    await bulk.WriteToServerAsync(valid);
                }

                response.Success = true;
                response.Message = "Upload successful";
                response.Result = new { InsertedCount = valid.Rows.Count };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }



        private void ExecuteNonQuerySql(string sql)
        {
            using var con = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, con);
            con.Open();
            cmd.ExecuteNonQuery();
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
                             HardwareID = h.Field<int>("hardwareID"),
                             BVPartNumber = h.Field<string>("bv_part_number"),
                             Model = h.Field<string>("model"),
                             SpireDescription = i.Field<string>("description"),
                             RDDealerCost = h.Field<decimal?>("dealer_cost") ?? 0,
                             SpireCurrentCost = i.Field<decimal?>("current_cost") ?? 0,
                             ProductCode = i.Field<string>("product_code"),
                             LastSaleDate = i.Field<DateTime?>("last_sale_date")
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
