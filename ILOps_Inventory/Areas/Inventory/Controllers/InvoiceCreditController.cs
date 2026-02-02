using ILOps_Inventory.Areas.Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class InvoiceCreditController : Controller
    {
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public InvoiceCreditController(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
            _pgConn = configuration.GetConnectionString("spire_Connection");
        }

        // Main Index page
        public IActionResult Index(string type = "Hardware", int page = 1, string po = null)
        {
            int pageSize = 10;
            var receipts = string.IsNullOrEmpty(po) ? GetReceipts(type) : GetReceiptsByPO(po, type);
            int totalItems = receipts.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages == 0 ? 1 : totalPages;

            var pagedReceipts = receipts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // RogersInvoices for first receipt (auto select first row)
            List<RogersInvoiceVM> rogersInvoices = new List<RogersInvoiceVM>();
            if (pagedReceipts.Any())
            {
                rogersInvoices = GetRogersInvoices_Internal(pagedReceipts[0].BVReceiptNo);
            }

            var model = new InvoiceCreditPageVM
            {
                SelectedType = type ?? "",
                FindPO = po ?? "",
                PageSize = pageSize,
                CurrentPage = page,
                TotalItems = totalItems,
                TotalPages = totalPages,
                MissingReceipts = pagedReceipts,
                RogersInvoices = rogersInvoices
            };

            return View("~/Areas/Inventory/Views/Inventory/InvoiceCredit.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetRogersInvoices(string bvReceiptNo)
        {
            if (string.IsNullOrEmpty(bvReceiptNo))
                return Json(new List<RogersInvoiceVM>());

            var data = GetRogersInvoices_Internal(bvReceiptNo);
            return Json(data);
        }

        private List<RogersInvoiceVM> GetRogersInvoices_Internal(string bvReceiptNo)
        {
            var list = new List<RogersInvoiceVM>();

            string sql = @"
    SELECT TransType, RefNo, TransDate, PerUnitAmount, Remarks
    FROM tblRogersInvoice
    WHERE BVReceiptNo = @BVReceiptNo
    ORDER BY TransDate DESC";

            using (var conn = new SqlConnection(_sqlConn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo); // <-- yaha correct parameter

                conn.Open();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new RogersInvoiceVM
                    {
                        TransType = reader["TransType"]?.ToString(),
                        RefNo = reader["RefNo"]?.ToString(),
                        TransDate = Convert.ToDateTime(reader["TransDate"]),
                        PerUnitAmount = reader["PerUnitAmount"] != DBNull.Value ? Convert.ToDecimal(reader["PerUnitAmount"]) : 0,
                        Remarks = reader["Remarks"]?.ToString()
                    });
                }
            }

            return list;
        }


        //private List<RogersInvoiceVM> GetRogersInvoices(string bvReceiptNo)
        //{
        //    var list = new List<RogersInvoiceVM>();

        //    string sql = @"
        //SELECT TransType, RefNo, TransDate, PerUnitAmount, Remarks
        //FROM tblRogersInvoice
        //WHERE BVReceiptNo = @BVReceiptNo
        //ORDER BY TransDate DESC";

        //    using (var conn = new SqlConnection(_sqlConn))
        //    using (var cmd = new SqlCommand(sql, conn))
        //    {
        //        cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);
        //        conn.Open();
        //        using var reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            list.Add(new RogersInvoiceVM
        //            {
        //                TransType = reader["TransType"]?.ToString(),
        //                RefNo = reader["RefNo"]?.ToString(),
        //                TransDate = reader["TransDate"] != DBNull.Value
        //                    ? Convert.ToDateTime(reader["TransDate"])
        //                    : DateTime.MinValue,
        //                PerUnitAmount = reader["PerUnitAmount"] != DBNull.Value
        //                    ? Convert.ToDecimal(reader["PerUnitAmount"])
        //                    : 0,
        //                Remarks = reader["Remarks"]?.ToString()
        //            });
        //        }

        //    }

        //    return list;
        //}

        // POST: Find receipt by BVReceiptNo
        //[HttpPost]
        //public IActionResult FindReceipt(string receiptNo, string type)
        //{
        //    if (string.IsNullOrEmpty(receiptNo))
        //        return Json(new { success = false, message = "Please enter a Receipt Number." });

        //    bool found = CheckReceiptExists(receiptNo, type);
        //    if (!found)
        //        return Json(new { success = false, message = $"Receipt {receiptNo} not found as {type}." });

        //    string whse = GetWarehouseForReceipt(receiptNo);
        //    //if (whse != "CO")
        //    //    return Json(new { success = false, message = $"Receipt {receiptNo} found, but warehouse is not CO." });

        //    return Json(new { success = true, message = $"Receipt {receiptNo} found successfully." });
        //}


        [HttpPost]
        public IActionResult FindReceipt(string receiptNo, string type)
        {
            if (string.IsNullOrEmpty(receiptNo))
                return Json(new { success = false, message = "Please enter a Receipt Number." });

            var receipt = GetReceiptDetails(receiptNo, type); // <-- implement this to fetch all fields
            if (receipt == null)
                return Json(new { success = false, message = $"Receipt {receiptNo} not found as {type}." });

            return Json(new
            {
                success = true,
                message = $"Receipt {receiptNo} found successfully.",
                data = new
                {
                    receipt.BVReceiptNo,
                    receipt.CMO,
                    ReceiptDate = receipt.ReceiptDate.ToString("yyyy-MM-dd"),
                    receipt.PONumber,
                    receipt.Whse,
                    receipt.PartNo,
                    receipt.QtyReceived,
                    receipt.UnitCost
                }
            });
        }

        // POST: Find missing receipts by PO
        [HttpPost]
        public IActionResult FindMissingByPO(string po, string type)
        {
            return RedirectToAction("Index", new { po = po, type = type, page = 1 });
        }

        // POST: Show all receipts
        [HttpPost]
        public IActionResult ShowAll(string type)
        {
            return RedirectToAction("Index", new { type });
        }

        // POST: Load latest ACC receipts (simulation)
        [HttpPost]
        public IActionResult LoadLatestAccReceipts()
        {
            TempData["Success"] = "Latest ACC receipts loaded (simulation).";
            return RedirectToAction("Index", new { type = "Accessory" });
        }

        // Add new invoice with last info
        public IActionResult EnterInvoice()
        {
            return View(); // Your new invoice entry page
        }

        [HttpPost]
        public IActionResult SelectReceipt(string receiptNo, string type)
        {
            if (string.IsNullOrEmpty(receiptNo))
                return Json(new { success = false, message = "Please enter a Receipt Number." });

            bool found = CheckReceiptExists(receiptNo, type);
            if (!found)
                return Json(new { success = false, message = $"Receipt {receiptNo} not found as {type}." });

            string whse = GetWarehouseForReceipt(receiptNo);
            if (whse != "CO")
                return Json(new { success = false, message = $"Receipt {receiptNo} found, but warehouse is not CO." });

            var rogersInvoices = GetRogersInvoices_Internal(receiptNo);

            return Json(new { success = true, message = "Receipt found.", rogersInvoices });
        }
        [HttpPost]
        public IActionResult SaveInvoice([FromBody] SaveInvoiceRequest request)
        {
            if (request == null)
                return BadRequest("Request is null");

            if (string.IsNullOrEmpty(request.RefNo))
                return BadRequest("Reference Number is required.");

            if (request.TransDate == DateTime.MinValue)
                return BadRequest("Transaction Date is required.");

            if (request.PerUnitAmount == 0)
                return BadRequest("Amount cannot be zero.");

            // +ve / -ve amount
            decimal signedAmount =
                request.TransType == "C"
                    ? -request.PerUnitAmount
                    : request.PerUnitAmount;

            // Total BEFORE save
            decimal currentTotal =
                GetRogersTotalForReceipt(request.BVReceiptNo);

            // ⭐ EDIT CASE – remove old amount first
            if (!string.IsNullOrEmpty(request.EditingRefNo))
            {
                decimal oldAmount = GetInvoiceAmount(
                    request.BVReceiptNo,
                    request.EditingRefNo);

                currentTotal -= oldAmount;
            }

            decimal newTotal = currentTotal + signedAmount;

            decimal receiptUnitCost =
                GetReceiptUnitCost(request.BVReceiptNo);

            // Mismatch check
            if (newTotal != receiptUnitCost && !request.HasMoreEntries)
            {
                bool isHardware =
                    IsHardwareReceipt(request.BVReceiptNo);

                // optional email
                // SendCostMismatchEmail(...)
            }

            var invoice = new RogersInvoiceVM
            {
                TransType = request.TransType,
                RefNo = request.RefNo,
                TransDate = request.TransDate,
                PerUnitAmount = request.PerUnitAmount,
                Remarks = request.Remarks
            };

            // 🔥 ADD vs UPDATE
            if (!string.IsNullOrEmpty(request.EditingRefNo))
            {
                UpdateRogersInvoice(
                    invoice,
                    request.BVReceiptNo,
                    request.EditingRefNo);
            }
            else
            {
                InsertRogersInvoice(
                    invoice,
                    request.BVReceiptNo);
            }

            return Ok(new
            {
                success = true,
                total = newTotal
            });
        }

        private decimal GetInvoiceAmount(string receiptNo, string refNo)
        {
            string sql = @"
        SELECT TransType, PerUnitAmount
        FROM tblRogersInvoice
        WHERE BVReceiptNo = @BVReceiptNo
          AND RefNo = @RefNo";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@BVReceiptNo", receiptNo);
            cmd.Parameters.AddWithValue("@RefNo", refNo);

            conn.Open();
            using var r = cmd.ExecuteReader();

            if (!r.Read()) return 0;

            decimal amt = Convert.ToDecimal(r["PerUnitAmount"]);
            return r["TransType"].ToString() == "C" ? -amt : amt;
        }

        private void UpdateRogersInvoice(
    RogersInvoiceVM model,
    string bvReceiptNo,
    string oldRefNo)
        {
            string sql = @"
        UPDATE tblRogersInvoice
        SET
            TransType = @TransType,
            RefNo = @NewRefNo,
            TransDate = @TransDate,
            PerUnitAmount = @PerUnitAmount,
            Remarks = @Remarks
        WHERE BVReceiptNo = @BVReceiptNo
          AND RefNo = @OldRefNo";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);
            cmd.Parameters.AddWithValue("@OldRefNo", oldRefNo);
            cmd.Parameters.AddWithValue("@NewRefNo", model.RefNo);
            cmd.Parameters.AddWithValue("@TransType", model.TransType);
            cmd.Parameters.AddWithValue("@TransDate", model.TransDate);
            cmd.Parameters.AddWithValue("@PerUnitAmount", model.PerUnitAmount);
            cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");

            conn.Open();
            cmd.ExecuteNonQuery();
        }
        private void InsertRogersInvoice(RogersInvoiceVM model, string bvReceiptNo)
        {
            string sql = @"
        INSERT INTO tblRogersInvoice
        (BVReceiptNo, TransType, RefNo, TransDate, PerUnitAmount, Remarks)
        VALUES
        (@BVReceiptNo, @TransType, @RefNo, @TransDate, @PerUnitAmount, @Remarks)";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);
            cmd.Parameters.AddWithValue("@TransType", model.TransType);
            cmd.Parameters.AddWithValue("@RefNo", model.RefNo);
            cmd.Parameters.AddWithValue("@TransDate", model.TransDate);
            cmd.Parameters.AddWithValue("@PerUnitAmount", model.PerUnitAmount);
            cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");

            conn.Open();
            cmd.ExecuteNonQuery();
        }
        private void SendCostMismatchEmail(
    string receiptNo,
    RogersInvoiceVM inv,
    decimal rogersTotal,
    decimal receiptCost)
        {
            string body = $@"
Receipt No: {receiptNo}
Invoice Ref: {inv.RefNo}
Receipt Unit Cost: {receiptCost}
Rogers Total: {rogersTotal}

Please investigate possible price protection credits.";

            // hook your SMTP / SendGrid here
        }


        private bool IsHardwareReceipt(string bvReceiptNo)
        {
            string sql = @"SELECT ItemType FROM HardwareReceived WHERE BVReceiptNo = @No";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@No", bvReceiptNo);
            conn.Open();

            return cmd.ExecuteScalar()?.ToString() == "HDW";
        }
        private decimal GetReceiptUnitCost(string bvReceiptNo)
        {
            string sql = @"SELECT ReceiptUnitCost FROM HardwareReceived WHERE BVReceiptNo = @No";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@No", bvReceiptNo);
            conn.Open();

            return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);
        }

        private decimal GetRogersTotalForReceipt(string bvReceiptNo)
        {
            decimal total = 0;

            string sql = @"
        SELECT TransType, PerUnitAmount
        FROM tblRogersInvoice
        WHERE BVReceiptNo = @BVReceiptNo";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string type = reader["TransType"]?.ToString();
                decimal amt = Convert.ToDecimal(reader["PerUnitAmount"]);

                total += (type == "C") ? -amt : amt;
            }

            return total;
        }
        private HardwareReceivedVM GetReceiptDetails(string receiptNo, string type)
        {
            if (string.IsNullOrEmpty(receiptNo))
                return null;

            string itemType = type == "Hardware" ? "HDW" : "ACC";

            string sql = @"
        SELECT 
            BVReceiptNo,
            Vendor,
            BVReceiptDate AS ReceiptDate,
            CMO,
            PO AS PONumber,
            Part AS PartNo,
            Qty,
            ReceiptUnitCost AS UnitCost,
            ItemType AS Type
        FROM HardwareReceived
        WHERE BVReceiptNo = @ReceiptNo
          AND ItemType = @ItemType";

            using (var conn = new SqlConnection(_sqlConn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ReceiptNo", receiptNo);
                cmd.Parameters.AddWithValue("@ItemType", itemType);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new HardwareReceivedVM
                    {
                        BVReceiptNo = reader["BVReceiptNo"]?.ToString() ?? "",
                        VendorName = reader["Vendor"]?.ToString() ?? "",
                        ReceiptDate = reader["ReceiptDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["ReceiptDate"])
                            : DateTime.MinValue,
                        CMO = reader["CMO"]?.ToString() ?? "",
                        PONumber = reader["PONumber"]?.ToString() ?? "",
                        PartNo = reader["PartNo"]?.ToString() ?? "",
                        QtyReceived = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0,
                        UnitCost = reader["UnitCost"] != DBNull.Value ? Convert.ToDecimal(reader["UnitCost"]) : 0,
                        Type = reader["Type"]?.ToString() ?? "",

                        // Optional: fetch warehouse from PostgreSQL
                        Whse = "CO"
                    };
                }
            }
        }

        #region Helper Methods
        private List<HardwareReceivedVM> GetReceipts(string type)
        {
            var list = new List<HardwareReceivedVM>();
            string itemType = type == "Hardware" ? "HDW" : "ACC";

            string sql = @"
SELECT 
    BVReceiptNo,
    MAX(Part) AS PartNo,
    CASE WHEN MAX(ItemType) = 'HDW' THEN COUNT(IMEI)
         ELSE MAX(Qty)
    END AS QtyReceived,
    MAX(BVReceiptDate) AS ReceiptDate,
    MAX(CMO) AS CMO,
    MAX(Vendor) AS VendorName,  -- Fixed here
    MAX(ItemType) AS Type,
    MAX(ReceiptUnitCost) AS UnitCost,
    MAX(PO) AS PONumber
FROM HardwareReceived
WHERE ItemType = @ItemType
  AND BVReceiptDate >= DATEADD(MONTH, -4, GETDATE())
GROUP BY BVReceiptNo
ORDER BY BVReceiptNo DESC";

            using (SqlConnection conn = new SqlConnection(_sqlConn))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ItemType", itemType);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string bvReceiptNo = reader["BVReceiptNo"]?.ToString() ?? "";

                        list.Add(new HardwareReceivedVM
                        {
                            BVReceiptNo = bvReceiptNo,
                            PartNo = reader["PartNo"]?.ToString() ?? "",
                            QtyReceived = reader["QtyReceived"] != DBNull.Value ? Convert.ToInt32(reader["QtyReceived"]) : 0,
                            ReceiptDate = reader["ReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReceiptDate"]) : DateTime.MinValue,
                            UnitCost = reader["UnitCost"] != DBNull.Value ? Convert.ToDecimal(reader["UnitCost"]) : 0,
                            PONumber = reader["PONumber"]?.ToString() ?? "",
                            CMO = reader["CMO"]?.ToString() ?? "",
                            Type = reader["Type"]?.ToString() ?? "",
                            VendorName = reader["VendorName"]?.ToString() ?? "",  // Fixed mapping
                            Whse = "CO"
                        });
                    }
                }
            }

            return list;
        }


        private string GetWhseFromPg(string receiptNo)
        {
            string whse = "";

            // Convert BVReceiptNo to long
            if (!long.TryParse(receiptNo, out long receiptId))
            {
                return ""; // invalid receipt number
            }

            using (var conn = new NpgsqlConnection(_pgConn))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT whse FROM purchase_receipts WHERE inventory_receipt_id = @receiptNo LIMIT 1", conn))
                {
                    // Pass as long (bigint)
                    cmd.Parameters.AddWithValue("@receiptNo", NpgsqlTypes.NpgsqlDbType.Bigint, receiptId);

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        whse = result.ToString();
                    }
                }
            }

            return whse;
        }
        private bool CheckReceiptExists(string receiptNo, string type)
        {
            string itemType = type == "Hardware" ? "HDW" : "ACC";
            string sql = @"SELECT COUNT(1) FROM HardwareReceived WHERE BVReceiptNo = @ReceiptNo AND ItemType = @ItemType";

            using (SqlConnection conn = new SqlConnection(_sqlConn))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ReceiptNo", receiptNo);
                cmd.Parameters.AddWithValue("@ItemType", itemType);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private string GetWarehouseForReceipt(string receiptNo)
        {
            if (!long.TryParse(receiptNo, out long receiptId))
                return "";

            string sql = @"SELECT whse FROM purchase_receipts WHERE inventory_receipt_id = @ReceiptNo LIMIT 1";

            using (var conn = new NpgsqlConnection(_pgConn))
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ReceiptNo", receiptId);
                conn.Open();
                return cmd.ExecuteScalar()?.ToString() ?? "";
            }
        }



        [HttpGet]
        public IActionResult GetLastInvoice(string bvReceiptNo)
        {
            string sql = @"
    SELECT TOP 1 RefNo, TransDate, TransType
    FROM tblRogersInvoice
    WHERE BVReceiptNo = @BVReceiptNo
    ORDER BY TransDate DESC";

            using var conn = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);

            conn.Open();
            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return Json(null);

            return Json(new
            {
                refNo = r["RefNo"].ToString(),
                transDate = Convert.ToDateTime(r["TransDate"]).ToString("yyyy-MM-dd"),
                transType = r["TransType"].ToString()
            });
        }

        private List<HardwareReceivedVM> GetReceiptsByPO(string po, string type)
        {
            var list = new List<HardwareReceivedVM>();
            string itemType = type == "Hardware" ? "HDW" : "ACC";

            string sql = @"
SELECT 
    BVReceiptNo,
    MAX(Vendor) AS VendorName,  -- Fixed here
    MAX(BVReceiptDate) AS ReceiptDate,
    MAX(CMO) AS CMO,
    MAX(PO) AS PONumber,
    MAX(Part) AS PartNo,
    MAX(Qty) AS QtyReceived,
    MAX(ReceiptUnitCost) AS UnitCost,
    MAX(ItemType) AS Type
FROM HardwareReceived
WHERE ItemType = @ItemType AND PO = @PO
  AND BVReceiptDate >= DATEADD(MONTH, -4, GETDATE())
GROUP BY BVReceiptNo
ORDER BY BVReceiptNo DESC";

            using (SqlConnection conn = new SqlConnection(_sqlConn))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ItemType", itemType);
                cmd.Parameters.AddWithValue("@PO", po);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new HardwareReceivedVM
                        {
                            BVReceiptNo = reader["BVReceiptNo"]?.ToString() ?? "",
                            VendorName = reader["VendorName"]?.ToString() ?? "",  // Fixed mapping
                            ReceiptDate = reader["ReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReceiptDate"]) : DateTime.MinValue,
                            CMO = reader["CMO"]?.ToString() ?? "",
                            PONumber = reader["PONumber"]?.ToString() ?? "",
                            PartNo = reader["PartNo"]?.ToString() ?? "",
                            QtyReceived = reader["QtyReceived"] != DBNull.Value ? Convert.ToInt32(reader["QtyReceived"]) : 0,
                            UnitCost = reader["UnitCost"] != DBNull.Value ? Convert.ToDecimal(reader["UnitCost"]) : 0,
                            Type = reader["Type"]?.ToString() ?? "",
                            Whse = "CO"
                        });
                    }
                }
            }

            return list;
        }

        #endregion
    }
}
