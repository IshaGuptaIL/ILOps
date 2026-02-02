using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Npgsql;
using OfficeOpenXml;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class ReceiveIMEIController : Controller
    {
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public ReceiveIMEIController(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection"); // SQL Server
            _pgConn = configuration.GetConnectionString("spire_Connection");        // PostgreSQL (Spire)
        }

        public IActionResult Index()
        {
            return View("~/Areas/Inventory/Views/Inventory/ReceiveIMEI.cshtml");
        }

        #region Import Files

        [HttpPost]
        public IActionResult ImportPackingSlip(int poNumber, int recNo, string whse, string partNo, string guid, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Packing Slip file missing" });

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var con = new SqlConnection(_sqlConn);
            con.Open();

            // Clear previous data
            new SqlCommand("DELETE FROM TblPackingSlip", con).ExecuteNonQuery();

            using var stream = new MemoryStream();
            file.CopyTo(stream);
            using var pkg = new ExcelPackage(stream);
            var ws = pkg.Workbook.Worksheets[0];

            int row = 1;
            while (!string.IsNullOrWhiteSpace(ws.Cells[row, 1].Text))
            {
                using var cmd = new SqlCommand(@"
INSERT INTO TblPackingSlip(PONumber, RecNo, Whse, PartNo, GUID, IMEI, XLSRow)
VALUES (@PO, @Rec, @Whse, @Part, @Guid, @IMEI, @Row)", con);

                cmd.Parameters.AddWithValue("@PO", poNumber);
                cmd.Parameters.AddWithValue("@Rec", recNo);
                cmd.Parameters.AddWithValue("@Whse", whse);
                cmd.Parameters.AddWithValue("@Part", partNo);
                cmd.Parameters.AddWithValue("@Guid", guid);
                cmd.Parameters.AddWithValue("@IMEI", ws.Cells[row, 1].Text.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@Row", row);

                cmd.ExecuteNonQuery();
                row++;
            }

            return Json(new { success = true, message = $"Imported {row - 1} IMEIs from Packing Slip" });
        }

        [HttpPost]
        public IActionResult ImportScanList(int poNumber, int recNo, string whse, string partNo, string guid, string vendor, string location, IFormFile file)
        {
            if (file == null)
                return Json(new { success = false, message = "Scan List file missing" });

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var con = new SqlConnection(_sqlConn);
            con.Open();

            new SqlCommand("DELETE FROM tblScanList", con).ExecuteNonQuery();

            using var stream = new MemoryStream();
            file.CopyTo(stream);
            using var pkg = new ExcelPackage(stream);
            var ws = pkg.Workbook.Worksheets[0];

            int row = 1;
            while (!string.IsNullOrWhiteSpace(ws.Cells[row, 1].Text))
            {
                using var cmd = new SqlCommand(@"
INSERT INTO tblScanList(PONumber, RecNo, Whse, PartNo, GUID, Vendor, Location, IMEI, XLSRow)
VALUES (@PO, @Rec, @Whse, @Part, @Guid, @Vendor, @Loc, @IMEI, @Row)", con);

                cmd.Parameters.AddWithValue("@PO", poNumber);
                cmd.Parameters.AddWithValue("@Rec", recNo);
                cmd.Parameters.AddWithValue("@Whse", whse);
                cmd.Parameters.AddWithValue("@Part", partNo);
                cmd.Parameters.AddWithValue("@Guid", guid);
                cmd.Parameters.AddWithValue("@Vendor", string.IsNullOrWhiteSpace(vendor) ? (object)DBNull.Value : vendor);
                cmd.Parameters.AddWithValue("@Loc", string.IsNullOrWhiteSpace(location) ? (object)DBNull.Value : location);
                cmd.Parameters.AddWithValue("@IMEI", ws.Cells[row, 1].Text.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@Row", row);

                cmd.ExecuteNonQuery();
                row++;
            }

            return Json(new { success = true, message = $"Imported {row - 1} IMEIs" });
        }

        #endregion

        #region Get Data

        [HttpGet]
        public IActionResult GetPurchaseOrders()
        {
            var list = new List<object>();

            using var con = new NpgsqlConnection(_pgConn);
            con.Open();

            var cmd = new NpgsqlCommand(@"
SELECT 
    po.po_number, 
    po.vendor_name, 
    po.id AS po_id, 
    poi.id AS po_item_id,
    poi.part_no AS part_number, 
    poi.whse AS whse, 
    poi.order_qty AS order_qty,
    COALESCE(poi.received_qty, 0) AS received_qty, 
    poi.unit_price AS unit_cost,   -- ✅ Correct column
    poi.guid,
    poi.whse_location AS location
FROM purchase_orders po
JOIN purchase_order_items poi ON poi.po_number = po.po_number
WHERE po.status IN ('I','R','OPEN')
ORDER BY po.po_number DESC, poi.id", con);

            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new
                {
                    poNumber = rdr["po_number"].ToString(),
                    vendor = rdr["vendor_name"].ToString(),
                    poId = rdr["po_id"],
                    poItemId = rdr["po_item_id"],
                    part = rdr["part_number"].ToString(),
                    whse = rdr["whse"].ToString(),
                    ordQty = Convert.ToInt32(rdr["order_qty"]),
                    rcvdQty = 0,
                    guid = rdr["guid"].ToString(),
                     unitCost = Convert.ToDecimal(rdr["unit_cost"])
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetIMEIGrids()
        {
            using var con = new SqlConnection(_sqlConn);
            con.Open();

            var result = new
            {
                ScanList = new List<dynamic>(),
                PackingSlip = new List<dynamic>(),
                Matches = new List<dynamic>(),
                ScanNoPack = new List<dynamic>(),
                PackNoScan = new List<dynamic>(),
                Onhand = new List<dynamic>()
            };

            // Scan List
            using (var cmd = new SqlCommand("SELECT IMEI FROM tblScanList", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) result.ScanList.Add(new { IMEI = rdr.GetString(0), Invalid = false, Dupe = false });
            }

            // Packing Slip
            using (var cmd = new SqlCommand("SELECT IMEI FROM TblPackingSlip", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) result.PackingSlip.Add(new { IMEI = rdr.GetString(0), Invalid = false, Dupe = false });
            }

            // Matches
            var matches = result.ScanList.Select(s => (string)s.IMEI)
                                .Intersect(result.PackingSlip.Select(p => (string)p.IMEI));
            result.Matches.AddRange(matches.Select(m => new { IMEI = m, Invalid = false, Dupe = false }));

            // Scan-NoPackSlip
            var scanNoPack = result.ScanList.Select(s => (string)s.IMEI)
                                  .Except(result.PackingSlip.Select(p => (string)p.IMEI));
            result.ScanNoPack.AddRange(scanNoPack.Select(m => new { IMEI = m, Invalid = false, Dupe = false }));

            // PackSlip-NoScan
            var packNoScan = result.PackingSlip.Select(p => (string)p.IMEI)
                                  .Except(result.ScanList.Select(s => (string)s.IMEI));
            result.PackNoScan.AddRange(packNoScan.Select(m => new { IMEI = m, Invalid = false, Dupe = false }));

            // Onhand in Spire (mocked, replace with actual query)
            result.Onhand.AddRange(result.ScanList.Select(m => new { IMEI = (string)m.IMEI, Invalid = false, Dupe = false }));

            return Json(result);
        }
        #endregion

        #region Post Receipts


        [HttpPost]
        public IActionResult PostReceipts(long poId, long poItemId, string cmo, bool isReversal)
        {
            if (string.IsNullOrWhiteSpace(cmo))
                return Json(new { success = false, message = "CMO is required", processedCount = 0 });

            using var con = new SqlConnection(_sqlConn);
            con.Open();

            // Check unresolved errors
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM tblErrors WHERE Resolved = 0", con))
            {
                int errorCount = (int)cmd.ExecuteScalar();
                if (errorCount > 0)
                    return Json(new { success = false, message = "Unresolved errors exist", processedCount = 0 });
            }

            // Load Scan List
            var scanList = new List<(string IMEI, string Vendor, string PONumber, string PartNo)>();
            using (var cmd = new SqlCommand("SELECT IMEI, Vendor, PONumber, PartNo FROM tblScanList", con))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    scanList.Add((
                        Convert.ToString(rdr["IMEI"]),
                        rdr["Vendor"] == DBNull.Value ? null : Convert.ToString(rdr["Vendor"]),
                        rdr["PONumber"] == DBNull.Value ? null : Convert.ToString(rdr["PONumber"]),
                        rdr["PartNo"] == DBNull.Value ? null : Convert.ToString(rdr["PartNo"])
                    ));
                }
            }

            if (!scanList.Any())
                return Json(new { success = false, message = "No IMEIs found", processedCount = 0 });

            // Call Spire
            if (!ReceivePOIMEI(poId, poItemId, scanList.Select(x => x.IMEI).ToList(), isReversal, out string result))
            {
                LogError(con, cmo, poId, poItemId, result);
                return Json(new { success = false, message = result, processedCount = 0 });
            }

            // Get receipt no (Postgres)
            long receiptNo;
            using var pg = new NpgsqlConnection(_pgConn);
            pg.Open();
            using var rcmd = new NpgsqlCommand(
                @"SELECT inventory_receipt_id 
          FROM purchase_receipts 
          WHERE order_id=@poId 
          ORDER BY id DESC LIMIT 1", pg);
            rcmd.Parameters.AddWithValue("@poId", poId);
            receiptNo = Convert.ToInt64(rcmd.ExecuteScalar() ?? 0);

            if (receiptNo == 0)
                return Json(new { success = false, message = "Receipt not found", processedCount = 0 });

            // Unit cost
            decimal unitCost;
            using var costCmd = new NpgsqlCommand(
                "SELECT unit_price FROM purchase_order_items WHERE id=@id", pg);
            costCmd.Parameters.AddWithValue("@id", poItemId);
            unitCost = Convert.ToDecimal(costCmd.ExecuteScalar() ?? 0);

            // Insert HardwareReceived
            int processed = 0;
            using var icmd = new SqlCommand("", con);

            foreach (var item in scanList)
            {
                icmd.CommandText = @"
INSERT INTO HardwareReceived
(Vendor,BVReceiptNo,BVReceiptDate,CMO,PO,Part,Qty,ReceiptUnitCost,IMEI,ItemType)
VALUES
(@Vendor,@No,@Date,@CMO,@PO,@Part,@Qty,@Cost,@IMEI,'HDW')";

                icmd.Parameters.Clear();
                icmd.Parameters.AddWithValue("@Vendor", item.Vendor ?? "UNKNOWN");
                icmd.Parameters.AddWithValue("@No", receiptNo);
                icmd.Parameters.AddWithValue("@Date", DateTime.Now);
                icmd.Parameters.AddWithValue("@CMO", cmo);
                icmd.Parameters.AddWithValue("@PO", item.PONumber ?? "");
                icmd.Parameters.AddWithValue("@Part", item.PartNo ?? "");
                icmd.Parameters.AddWithValue("@Qty", isReversal ? -1 : 1);
                icmd.Parameters.AddWithValue("@Cost", unitCost);
                icmd.Parameters.AddWithValue("@IMEI", item.IMEI);

                icmd.ExecuteNonQuery();
                processed++;
            }

            return Json(new
            {
                success = true,
                message = $"Processed {processed} IMEIs",
                processedCount = processed
            });
        }
        private bool ReceivePOIMEI(long poId, long poItemId, List<string> imeis, bool isReversal, out string result)
        {
            // TODO: Replace this mock with actual Spire API / stored procedure call

            if (imeis == null || !imeis.Any())
            {
                result = "No IMEIs provided";
                return false;
            }

            try
            {
                // Simulated logic:
                if (isReversal)
                {
                    // Reversal: check if IMEIs exist in HardwareReceived for this PO/Item
                    // Normally, you would call Spire API to reverse the receipt
                    // Here we just mock a failure if first IMEI is "ERROR"
                    if (imeis[0].StartsWith("ERROR"))
                    {
                        result = "Reversal failed for IMEI " + imeis[0];
                        return false;
                    }
                    result = $"Reversed {imeis.Count} IMEIs successfully";
                }
                else
                {
                    // Normal receipt: check if already received
                    // Normally, you would call Spire API to post receipt
                    if (imeis[0].StartsWith("ERROR"))
                    {
                        result = "Receipt failed for IMEI " + imeis[0];
                        return false;
                    }
                    result = $"Received {imeis.Count} IMEIs successfully";
                }

                return true;
            }
            catch (Exception ex)
            {
                result = $"Exception: {ex.Message}";
                return false;
            }
        }

        private void LogError(SqlConnection con, string cmo, long poId, long poItemId, string errorMessage)
        {
            using var cmd = new SqlCommand(
                "INSERT INTO tblErrors (VBCode, VBDescription, PONumber, RecNo, ErrorWhile, RowCount, Resolved) " +
                "VALUES (@vbCode, @desc, @po, @recNo, @while, @rowCount, 0)", con);

            cmd.Parameters.AddWithValue("@vbCode", 999); // Placeholder
            cmd.Parameters.AddWithValue("@desc", errorMessage);
            cmd.Parameters.AddWithValue("@po", poId);
            cmd.Parameters.AddWithValue("@recNo", poItemId);
            cmd.Parameters.AddWithValue("@while", "ReceivePOIMEI");
            cmd.Parameters.AddWithValue("@rowCount", 0);
            cmd.ExecuteNonQuery();
        }


        [HttpGet]
        public IActionResult CheckErrors(long poId, long poItemId, bool isReversal)
        {
            using var con = new SqlConnection(_sqlConn);
            con.Open();

            // Clear previous errors
            new SqlCommand("DELETE FROM tblErrors", con).ExecuteNonQuery();

            int errorCount = 0;
            var errors = new List<string>();

            // 1️⃣ Scan IMEI not in Packing Slip
            int scanNotInPack = (int)new SqlCommand(@"
        SELECT COUNT(*) FROM (
            SELECT IMEI FROM tblScanList
            EXCEPT
            SELECT IMEI FROM TblPackingSlip
        ) x", con).ExecuteScalar();

            if (scanNotInPack > 0)
            {
                LogError(con, "Scan IMEI not in Packing Slip");
                errors.Add("There are entries on the Scan List that are not on the Packing Slip");
                errorCount++;
            }

            // 2️⃣ Packing Slip IMEI not in Scan List
            int packNotInScan = (int)new SqlCommand(@"
        SELECT COUNT(*) FROM (
            SELECT IMEI FROM TblPackingSlip
            EXCEPT
            SELECT IMEI FROM tblScanList
        ) x", con).ExecuteScalar();

            if (packNotInScan > 0)
            {
                LogError(con, "Packing Slip IMEI not in Scan List");
                errors.Add("There are entries on the Packing Slip that are not on the Scan List");
                errorCount++;
            }

            // 3️⃣ Duplicate Scan List
            int dupScan = (int)new SqlCommand(@"
        SELECT COUNT(*) FROM (
            SELECT IMEI FROM tblScanList
            GROUP BY IMEI HAVING COUNT(*) > 1
        ) x", con).ExecuteScalar();

            if (dupScan > 0)
            {
                LogError(con, "Duplicate IMEI in Scan List");
                errors.Add("There are duplicate IMEIs in the Scan List");
                errorCount++;
            }

            // 4️⃣ Duplicate Packing Slip
            int dupPack = (int)new SqlCommand(@"
        SELECT COUNT(*) FROM (
            SELECT IMEI FROM TblPackingSlip
            GROUP BY IMEI HAVING COUNT(*) > 1
        ) x", con).ExecuteScalar();

            if (dupPack > 0)
            {
                LogError(con, "Duplicate IMEI in Packing Slip");
                errors.Add("There are duplicate IMEIs in the Packing Slip");
                errorCount++;
            }

            return Json(new
            {
                errorCount,
                errors,
                canPost = errorCount == 0
            });
        }

        private void LogError(SqlConnection con, string errorWhile)
        {
            using var cmd = new SqlCommand(@"
INSERT INTO tblErrors
(VBCode, VBDescription, PONumber, RecNo, ErrorWhile, [RowCount], Resolved)
VALUES
(0, @desc, '', 0, @while, 0, 0)", con);

            cmd.Parameters.AddWithValue("@desc", errorWhile);
            cmd.Parameters.AddWithValue("@while", "CheckErrors");
            cmd.ExecuteNonQuery();
        }

        #endregion
    }
}
