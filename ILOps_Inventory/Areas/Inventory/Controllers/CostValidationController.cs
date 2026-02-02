using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Npgsql;
using OfficeOpenXml;
using System.Data;
using System.Linq;
using System.Globalization;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class CostValidationController : Controller
    {
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public CostValidationController(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
            _pgConn = configuration.GetConnectionString("spire_Connection");
        }

        // ========================= INDEX =========================
        public IActionResult Index(string viewType = "")
        {
            if (TempData["UploadSuccess"] != null && string.IsNullOrEmpty(viewType))
            {
                viewType = "Latest";
                ViewBag.AutoLoaded = true;
            }

            DataTable dt = null;

            if (!string.IsNullOrEmpty(viewType))
            {
                if (viewType == "Latest")
                {
                    dt = GetSqlServerData(@"
                SELECT Whse, Part, MaxOfF3 AS StartDate, Cost AS RogersCost, DelistDate
                FROM HPCExtractSummary
                ORDER BY Whse, Part");

                    ViewBag.Title = "HPC Latest";
                }
               
                else if (viewType == "Discrepancies")
                {
                    DataTable dtSql = GetSqlServerData("SELECT * FROM HPCExtractSummary");

                    DataTable dtPg = GetPostgresData(@"
                        SELECT part_no, whse, description, product_code,
                               current_cost, onhand_qty, purchase_qty
                        FROM inventory");

                    dt = new DataTable();
                    dt.Columns.Add("Whse");
                    dt.Columns.Add("Part");
                    dt.Columns.Add("Description");
                    dt.Columns.Add("StartDate");
                    dt.Columns.Add("SpireProdCode");
                    dt.Columns.Add("RogersCost");
                    dt.Columns.Add("SpireCost");
                    dt.Columns.Add("ExistInSpire");
                    dt.Columns.Add("OnhandQty");
                    dt.Columns.Add("PurchaseQty");

                    foreach (DataRow h in dtSql.Rows)
                    {
                        string part = h["Part"].ToString();
                        string whse = h["Whse"].ToString();

                        DataRow pg = dtPg.AsEnumerable()
                            .FirstOrDefault(r =>
                                r["part_no"].ToString() == part &&
                                r["whse"].ToString() == whse);

                        DataRow row = dt.NewRow();
                        row["Whse"] = whse;
                        row["Part"] = part;
                        row["Description"] = pg?["description"] ?? DBNull.Value;
                        row["StartDate"] = h["MaxOfF3"];
                        row["SpireProdCode"] = pg?["product_code"] ?? "";
                        row["RogersCost"] = h["Cost"];
                        row["SpireCost"] = pg?["current_cost"] ?? DBNull.Value;
                        row["ExistInSpire"] = pg == null ? "No" : "Yes";
                        row["OnhandQty"] = pg?["onhand_qty"] ?? DBNull.Value;
                        row["PurchaseQty"] = pg?["purchase_qty"] ?? DBNull.Value;

                        if (
                            row["ExistInSpire"].ToString() == "No" ||
                            Math.Round(Convert.ToDecimal(row["RogersCost"]), 2) != Math.Round(Convert.ToDecimal(row["SpireCost"] ?? 0), 2) ||
                            row["SpireProdCode"].ToString() != "HCC"
                        )
                        {
                            dt.Rows.Add(row);
                        }
                    }

                    ViewBag.Title = "HPC Discrepancies";
                }
            }

            // ===== INVALID ROWS FROM UPLOAD =====
            if (TempData["InvalidRows"] != null)
            {
                ViewBag.InvalidRows =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<DataTable>(
                        TempData["InvalidRows"].ToString());

                ViewBag.InsertedCount = TempData["InsertedCount"];
                ViewBag.FailedCount = TempData["FailedCount"];
            }

            return View("~/Areas/Inventory/Views/Inventory/CostValidation.cshtml", dt);
        }

        // ========================= LOAD HPC =========================
        [HttpPost]
        public IActionResult LoadHPC(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return Json(new { success = false, message = "Please select a valid Excel file." });

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                DataTable raw = new DataTable();

                // ================= READ EXCEL =================
                using (var stream = excelFile.OpenReadStream())
                using (var pkg = new ExcelPackage(stream))
                {
                    var ws = pkg.Workbook.Worksheets[0];

                    foreach (var c in ws.Cells[1, 1, 1, ws.Dimension.End.Column])
                        raw.Columns.Add(c.Text);

                    for (int r = 2; r <= ws.Dimension.End.Row; r++)
                    {
                        DataRow dr = raw.NewRow();

                        for (int c = 1; c <= ws.Dimension.End.Column; c++)
                        {
                            dr[c - 1] = ws.Cells[r, c].Text; // 👈 KEY FIX
                        }

                        raw.Rows.Add(dr);
                    }
                }

                // ================= VALIDATION =================
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

                    // SKU
                    if (string.IsNullOrWhiteSpace(r["SKU"]?.ToString()))
                    {
                        invalid.Rows.Add(excelRow, "", "SKU", "", "SKU is blank");
                        isValid = false;
                    }

                    // Dealer Cost
                    if (!decimal.TryParse(r["Dealer Cost"]?.ToString(), out _))
                    {
                        invalid.Rows.Add(
                            excelRow,
                            r["SKU"]?.ToString(),
                            "Dealer Cost",
                             r["Dealer Cost"]?.ToString(),
    $"Dealer Cost must be numeric (e.g. 12.50). Entered value: {r["Dealer Cost"]}"
                        );
                        isValid = false;
                    }

                    // Delisted Date - optional but MUST be yyyy-MM-dd if provided
                    if (!string.IsNullOrWhiteSpace(r["Delisted Date"]?.ToString()) &&
                        !DateTime.TryParseExact(
                            r["Delisted Date"]?.ToString(),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out _))
                    {
                        invalid.Rows.Add(
                            excelRow,
                            r["SKU"]?.ToString(),
                            "Delisted Date",
                            r["Delisted Date"]?.ToString(),
                            "Delisted Date must be in yyyy-MM-dd format (example: 2025-12-31)"
                        );
                        isValid = false;
                    }


                    // Drop Date
                    if (!DateTime.TryParseExact(
         r["Drop Date"]?.ToString(),
         "yyyy-MM-dd",
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out _))
                    {
                        invalid.Rows.Add(
                            excelRow,
                            r["SKU"]?.ToString(),
                            "Drop Date",
                            r["Drop Date"]?.ToString(),
                            "Drop Date must be in yyyy-MM-dd format (example: 2025-12-31)"
                        );
                        isValid = false;
                    }
                    if (isValid)
                        valid.ImportRow(r);

                    excelRow++;
                }

                // ================= STOP IF ANY INVALID =================
                if (invalid.Rows.Count > 0)
                {
                    TempData["InvalidRows"] =
                        Newtonsoft.Json.JsonConvert.SerializeObject(invalid);

                    TempData["InsertedCount"] = 0;
                    TempData["FailedCount"] = invalid.Rows.Count;

                    return Json(new
                    {
                        success = false,
                        message = "Upload failed. Please fix invalid records and re-upload."
                    });
                }

                // ================= CLEAR TABLES =================
                ExecuteNonQuerySql("DELETE FROM HPCExtract");
                ExecuteNonQuerySql("DELETE FROM HPCExtractSummary");

                // ================= BULK INSERT =================
                using (var bulk = new SqlBulkCopy(_sqlConn))
                {
                    bulk.DestinationTableName = "HPCExtract";
                    bulk.ColumnMappings.Add("SKU", "SKU");
                    bulk.ColumnMappings.Add("Dealer Cost", "DealerCost");
                    bulk.ColumnMappings.Add("Drop Date", "DropDate");
                    bulk.ColumnMappings.Add("Delisted Date", "DelistedDate");
                    bulk.WriteToServer(valid);
                }

                // ================= SUMMARY INSERT =================
                using (var con = new SqlConnection(_sqlConn))
                {
                    con.Open();

                    foreach (DataRow r in valid.Rows)
                    {
                        using var cmd = new SqlCommand(@"
                    INSERT INTO HPCExtractSummary
                    (Whse, Part, Cost, MaxOfF3, DelistDate)
                    VALUES ('CO', @Part, @Cost, @Date, @Delist)", con);

                        cmd.Parameters.AddWithValue("@Part", r["SKU"]);
                        cmd.Parameters.AddWithValue(
                            "@Cost",
                            Math.Round(decimal.Parse(r["Dealer Cost"].ToString()), 2)
                        );
                        cmd.Parameters.AddWithValue(
                            "@Date",
                            DateTime.Parse(r["Drop Date"].ToString())
                        );
                        cmd.Parameters.AddWithValue(
                            "@Delist",
                            string.IsNullOrWhiteSpace(r["Delisted Date"]?.ToString())
                                ? (object)DBNull.Value
                                : DateTime.Parse(r["Delisted Date"].ToString())
                        );

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["InsertedCount"] = valid.Rows.Count;
                TempData["FailedCount"] = 0;
                TempData["UploadSuccess"] = "1";

                return Json(new
                {
                    success = true,
                    message = "Upload successful. All records are valid."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // ========================= TEMPLATE =========================
        public IActionResult DownloadHPCTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("HPC Template");

            ws.Cells[1, 1].Value = "SKU";
            ws.Cells[1, 2].Value = "Dealer Cost";
            ws.Cells[1, 3].Value = "Drop Date";
            ws.Cells[1, 4].Value = "Delisted Date";

            return File(pkg.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "HPC_Template.xlsx");
        }

        // ========================= COST VARIANCE ACROSS WAREHOUSES =========================
        public IActionResult CostVarianceAcrossWarehouses()
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

            ViewBag.Title = "Cost Variance Across Warehouses";
            return View("~/Areas/Inventory/Views/Inventory/CostValidation.cshtml", dt);
        }

        // ========================= CURRENT vs AVG VARIANCE PER ITEM =========================
        public IActionResult CostVarianceCurrentVsAvg()
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

            ViewBag.Title = "Compare Variance Current vs Avg Per Item";
            return View("~/Areas/Inventory/Views/Inventory/CostValidation.cshtml", dt);
        }

        // ========================= RD Hardware vs Spire Current =========================
        public IActionResult RDHardwareToSpireCurrent()
        {
            DataTable dtHardware = GetSqlServerData(@"
        SELECT
            hardwareID,
            bv_part_number,
            model,
            dealer_cost
        FROM t_hardware
        WHERE bv_part_number IS NOT NULL
    ");

            DataTable dtInventory = GetPostgresData(@"
        SELECT
            part_no,
            description,
            current_cost,
            product_code,
            last_sale_date
        FROM inventory
        WHERE whse = 'CO'
    ");

            DataTable result = new DataTable();
            result.Columns.Add("hardwareID", typeof(int));
            result.Columns.Add("bv_part_number", typeof(string));
            result.Columns.Add("model", typeof(string));
            result.Columns.Add("SpireDescription", typeof(string));
            result.Columns.Add("RDDealerCost", typeof(decimal));
            result.Columns.Add("SpireCurrentCost", typeof(decimal));
            result.Columns.Add("product_code", typeof(string));
            result.Columns.Add("last_sale_date", typeof(DateTime));

            var joined =
         from h in dtHardware.AsEnumerable()
         join i in dtInventory.AsEnumerable()
             on h.Field<string>("bv_part_number").Trim().ToUpper()
             equals i.Field<string>("part_no").Trim().ToUpper()
         orderby h.Field<string>("bv_part_number")
         select new
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

            foreach (var r in joined)
            {
                result.Rows.Add(
                    r.HardwareID,
                    r.BVPartNumber,
                    r.Model,
                    r.SpireDescription,
                    Math.Round(r.RDDealerCost, 2),
                    Math.Round(r.SpireCurrentCost, 2),
                    r.ProductCode,
                    r.LastSaleDate ?? (object)DBNull.Value
                );
            }

            ViewBag.Title = "Compare RD Hardware Cost To Spire Current";

            return View("~/Areas/Inventory/Views/Inventory/CostValidation.cshtml", result);
        }

        // ========================= EXPORT FUNCTION =========================
        public IActionResult ExportGrid(string viewType)
        {
            DataTable dt = null;

            if (viewType == "Latest")
            {
                dt = GetSqlServerData(@"
                    SELECT Whse, Part, MaxOfF3 AS StartDate,
                           Cost AS RogersCost, DelistDate
                    FROM HPCExtractSummary
                    ORDER BY Whse, Part
                ");
            }
            else if (viewType == "Discrepancies")
            {
                DataTable dtSql = GetSqlServerData("SELECT * FROM HPCExtractSummary");
                DataTable dtPg = GetPostgresData(@"
                    SELECT part_no, whse, description, product_code,
                           current_cost, onhand_qty, purchase_qty
                    FROM inventory
                ");

                dt = new DataTable();
                dt.Columns.Add("Whse");
                dt.Columns.Add("Part");
                dt.Columns.Add("Description");
                dt.Columns.Add("StartDate");
                dt.Columns.Add("SpireProdCode");
                dt.Columns.Add("RogersCost");
                dt.Columns.Add("SpireCost");
                dt.Columns.Add("ExistInSpire");
                dt.Columns.Add("OnhandQty");
                dt.Columns.Add("PurchaseQty");

                foreach (DataRow h in dtSql.Rows)
                {
                    var pg = dtPg.AsEnumerable().FirstOrDefault(r =>
                        r["part_no"].ToString() == h["Part"].ToString() &&
                        r["whse"].ToString() == h["Whse"].ToString());

                    DataRow row = dt.NewRow();
                    row["Whse"] = h["Whse"];
                    row["Part"] = h["Part"];
                    row["Description"] = pg?["description"] ?? DBNull.Value;
                    row["StartDate"] = h["MaxOfF3"];
                    row["SpireProdCode"] = pg?["product_code"] ?? "";
                    row["RogersCost"] = h["Cost"];
                    row["SpireCost"] = pg?["current_cost"] ?? DBNull.Value;
                    row["ExistInSpire"] = pg == null ? "No" : "Yes";
                    row["OnhandQty"] = pg?["onhand_qty"] ?? DBNull.Value;
                    row["PurchaseQty"] = pg?["purchase_qty"] ?? DBNull.Value;

                    if (row["ExistInSpire"].ToString() == "No" ||
                        Math.Round(Convert.ToDecimal(row["RogersCost"]), 2) !=
                        Math.Round(Convert.ToDecimal(row["SpireCost"] ?? 0), 2) ||
                        row["SpireProdCode"].ToString() != "HCC")
                    {
                        dt.Rows.Add(row);
                    }
                }
            }
            else if (viewType == "CostVarianceAcrossWarehouses")
            {
                dt = GetPostgresData(@"
                    SELECT part_no, whse, description,
                           current_cost, average_cost
                    FROM inventory
                    WHERE product_code = 'HCC'
                      AND whse NOT IN ('FR','COADV')
                      AND current_cost <> average_cost
                    ORDER BY part_no, whse
                ");
            }
            else if (viewType == "CurrentVsAvg")
            {
                dt = GetPostgresData(@"
                    SELECT whse, part_no, description,
                           current_cost, average_cost
                    FROM inventory
                    WHERE product_code = 'HCC'
                      AND whse NOT IN ('FR','COADV')
                      AND current_cost <> average_cost
                ");
            }

            else if (viewType == "RDHardwareToSpire")
            {
                DataTable dtHardware = GetSqlServerData(@"
        SELECT
            hardwareID,
            bv_part_number,
            model,
            dealer_cost
        FROM t_hardware
        WHERE bv_part_number IS NOT NULL
    ");

                DataTable dtInventory = GetPostgresData(@"
        SELECT
            part_no,
            description,
            current_cost,
            product_code,
            last_sale_date
        FROM inventory
        WHERE whse = 'CO'
    ");

                dt = new DataTable();
                dt.Columns.Add("hardwareID");
                dt.Columns.Add("bv_part_number");
                dt.Columns.Add("model");
                dt.Columns.Add("SpireDescription");
                dt.Columns.Add("RDDealerCost");
                dt.Columns.Add("SpireCurrentCost");
                dt.Columns.Add("product_code");
                dt.Columns.Add("last_sale_date");

                var joined =
                    from h in dtHardware.AsEnumerable()
                    join i in dtInventory.AsEnumerable()
                        on h.Field<string>("bv_part_number").Trim().ToUpper()
                        equals i.Field<string>("part_no").Trim().ToUpper()
                    orderby h.Field<string>("bv_part_number")
                    select new
                    {
                        h,
                        i
                    };

                foreach (var r in joined)
                {
                    decimal dealerCost = r.h["dealer_cost"] != DBNull.Value
                        ? Math.Round(Convert.ToDecimal(r.h["dealer_cost"]), 2)
                        : 0m;

                    decimal spireCurrentCost = r.i["current_cost"] != DBNull.Value
                        ? Math.Round(Convert.ToDecimal(r.i["current_cost"]), 2)
                        : 0m;

                    dt.Rows.Add(
                        r.h["hardwareID"],
                        r.h["bv_part_number"],
                        r.h["model"],
                        r.i["description"],
                        dealerCost,
                        spireCurrentCost,
                        r.i["product_code"],
                        r.i["last_sale_date"]
                    );
                }
            }

            return ExportDataTableToExcel(
                dt,
                $"CostValidation_{viewType}.xlsx"
            );
        }

        private FileResult ExportDataTableToExcel(DataTable dt, string fileName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Data");

            // Headers
            for (int c = 0; c < dt.Columns.Count; c++)
            {
                ws.Cells[1, c + 1].Value = dt.Columns[c].ColumnName;
                ws.Cells[1, c + 1].Style.Font.Bold = true;
            }

            // Rows
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    ws.Cells[r + 2, c + 1].Value = dt.Rows[r][c];
                }
            }

            ws.Cells.AutoFitColumns();

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        // ========================= HELPERS =========================
        private void ExecuteNonQuerySql(string sql)
        {
            using var con = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, con);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        private DataTable GetSqlServerData(string sql)
        {
            DataTable dt = new DataTable();
            using var con = new SqlConnection(_sqlConn);
            using var da = new SqlDataAdapter(sql, con);
            da.Fill(dt);
            return dt;
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
