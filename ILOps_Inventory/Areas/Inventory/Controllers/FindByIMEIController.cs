using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ILOps_Inventory.Areas.Inventory.Models;

namespace ILOps_Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    public class FindByIMEIController : Controller
    {
        private readonly string _sqlConn;

        public FindByIMEIController(IConfiguration config)
        {
            _sqlConn = config.GetConnectionString("bvactivation_Connection");
        }

        [HttpGet]
        public IActionResult FindByImei()
        {
            return View("~/Areas/Inventory/Views/Inventory/FindByImei.cshtml");
        }

        [HttpPost]
        public IActionResult Search(string imei)
        {
            if (string.IsNullOrEmpty(imei))
                return Json(new { success = false, message = "IMEI is required" });

            HardwareReceivedVM receipt = null;

            string sql = @"
        SELECT TOP 1 *, Vendor  -- make sure Vendor column exists in HardwareReceived table
        FROM HardwareReceived
        WHERE IMEI = @IMEI";

            using (var conn = new SqlConnection(_sqlConn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@IMEI", imei);
                conn.Open();

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    receipt = new HardwareReceivedVM
                    {
                        VendorName = r["Vendor"]?.ToString(),  // <-- new
                        BVReceiptNo = r["BVReceiptNo"].ToString(),
                        PONumber = r["PO"]?.ToString(),
                        PartNo = r["Part"]?.ToString(),
                        QtyReceived = Convert.ToInt32(r["Qty"]),
                        UnitCost = Convert.ToDecimal(r["ReceiptUnitCost"]),
                        ReceiptDate = Convert.ToDateTime(r["BVReceiptDate"]),
                        CMO = r["CMO"]?.ToString()
                    };
                }
            }

            if (receipt == null)
                return Json(new { success = false, message = "IMEI not found" });

            return Json(new { success = true, receipt });
        }


        [HttpGet]
        public IActionResult GetRogersInvoices(string bvReceiptNo)
        {
            var list = new List<RogersInvoiceVM>();

            string sql = @"
                SELECT TransType, RefNo, TransDate, PerUnitAmount, Qty, Remarks
                FROM tblRogersInvoice
                WHERE BVReceiptNo = @BVReceiptNo";

            using (var conn = new SqlConnection(_sqlConn))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BVReceiptNo", bvReceiptNo);
                conn.Open();

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new RogersInvoiceVM
                    {
                        TransType = r["TransType"]?.ToString(),
                        RefNo = r["RefNo"]?.ToString(),
                        TransDate = (DateTime)(r["TransDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(r["TransDate"])),
                        PerUnitAmount = r["PerUnitAmount"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(r["PerUnitAmount"]),
                        Qty = r["Qty"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(r["Qty"]),
                        Remarks = r["Remarks"]?.ToString()
                    });
                }
            }

            return Json(list);
        }
    }
}
