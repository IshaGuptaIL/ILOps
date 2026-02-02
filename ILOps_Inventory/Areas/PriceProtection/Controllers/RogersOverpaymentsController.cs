using ClosedXML.Excel;
using ILOps_Inventory.Areas.PriceProtection.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ILOps_Inventory.Areas.PriceProtection.Controllers
{
    [Area("PriceProtection")]
    public class RogersOverpaymentsController : Controller
    {
        private readonly string _connectionString;

        public RogersOverpaymentsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("bvactivation_Connection");
        }

        // LOAD PAGE
        [HttpGet]
        public IActionResult Index()
        {
            var files = new List<string>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "SELECT DISTINCT FileName FROM RogersOverpayments ORDER BY FileName", conn);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
                files.Add(rdr.GetString(0));

            ViewBag.FileList = files;
            return View("~/Areas/PriceProtection/Views/RogerOverpayment.cshtml");
        }

        // IMPORT EXCEL
        [HttpPost]
        public IActionResult Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected";
                return RedirectToAction("Index");
            }

            using var workbook = new XLWorkbook(file.OpenReadStream());
            var ws = workbook.Worksheet(1);
            var rows = ws.RangeUsed().RowsUsed().Skip(1);

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            foreach (var row in rows)
            {
                var dealer = row.Cell(1).GetString();
                if (string.IsNullOrWhiteSpace(dealer))
                    continue;
                decimal amount = 0;
                var amountText = row.Cell(2).GetValue<string>();
                decimal.TryParse(amountText, out amount);

                using var cmd = new SqlCommand(@"
                    INSERT INTO RogersOverpayments
                    (Dealer, Amount, FileName, CreatedOn)
                    VALUES (@Dealer, @Amount, @FileName, GETDATE())", conn);

                cmd.Parameters.AddWithValue("@Dealer", dealer);
                cmd.Parameters.AddWithValue("@Amount", amount);
                cmd.Parameters.AddWithValue("@FileName", file.FileName);

                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "File Imported Successfully";
            return RedirectToAction("Index");
        }

        // DELETE BY FILE
        [HttpPost]
        public IActionResult DeleteByFile(string fileName)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "DELETE FROM RogersOverpayments WHERE FileName = @FileName", conn);

            cmd.Parameters.AddWithValue("@FileName", fileName);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Success"] = "Records Deleted";
            return RedirectToAction("Index");
        }

        // EXPORT
        [HttpGet]
        public IActionResult Export()
        {
            var table = new DataTable();

            using var conn = new SqlConnection(_connectionString);
            using var da = new SqlDataAdapter(
                "SELECT Dealer, Amount, FileName FROM RogersOverpayments", conn);

            da.Fill(table);

            using var wb = new XLWorkbook();
            wb.Worksheets.Add(table, "RogersOverpayments");

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "RogersOverpayments.xlsx");
        }
    }
}
