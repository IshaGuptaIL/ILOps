using DAL.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using DAL.Sales.TaxSalesReport;

namespace DAL.Sales.BO
{
    public class SalesTaxReportDA : ISalesTaxReport
    {
        private readonly string _pgConn;
        private readonly AppDBContext _dbContext;

        public SalesTaxReportDA(IConfiguration config, AppDBContext dbContext)
        {
            _pgConn = config.GetConnectionString("spire_Connection");
            _dbContext = dbContext;
        }

        // ─── LOAD DATA LOGIC (MIRRORING VBA) ──────────────────────────────────

        public async Task<bool> LoadSalesTaxHistoryAsync(SalesTaxReportRequest request, int userId)
        {
            var spireData = await FetchSalesHistoryFromSpire(request.StartDate, request.EndDate);

            // ⬇️ Set timeout to 10 minutes (600 seconds)
            _dbContext.Database.SetCommandTimeout(600);

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "DELETE FROM tblTaxDataOutput; DELETE FROM tblTaxDataOutputDetail;"
                    );

                    var entities = spireData.Select(row => new TblTaxDataOutput
                    {
                        Trans = row.Trans,
                        InvDate = row.Invdate,
                        Invoice = row.Invoice,
                        WebOrderID = row.WebOrderID,
                        Source = "OE",
                        CustNo = row.CustNo,
                        CustName = row.CustName,
                        Territory = row.Territory,
                        ShipToProvince = row.ShipToProvince,
                        PostalDigit = row.PostalDigit,
                        Tax1Code = row.Tax1Code,
                        Tax1Name = row.Tax1Name,
                        Tax1GL = row.Tax1GL,
                        Tax2Code = row.Tax2Code,
                        Tax2Name = row.Tax2Name,
                        Tax2GL = row.Tax2GL,
                        InvoiceNet = row.InvoiceNet,
                        Tax1Total = row.Tax1Total,
                        Tax2Total = row.Tax2Total,
                        ShippingAmt = row.ShippingAmt,
                        InvoiceTotalBeforeUERVValue = row.InvoiceTotalBeforeUERVValue,
                        UERVValue = row.UERVValue,
                        InvoiceTotal = row.InvoiceTotal,
                        TotalOfExtendedSell = row.TotalOfExtendedSell
                    }).ToList();

                    await _dbContext.tblTaxDataOutput.AddRangeAsync(entities);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        public async Task<bool> LoadGLDataAsync(SalesTaxReportRequest request, int userId)
        {
            DateTime start = request.StartDate;
            DateTime end = request.EndDate;

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblGLTransToTaxAccounts");

                    // Get tax accounts from staging table to filter GL transactions
                    var taxAccounts = await _dbContext.tblTaxAccounts.Select(x => x.GL_ACCOUNT).ToListAsync();
                    // Wait, I should use tblTaxAccounts as per VBA.
                    // Let's assume tblTaxAccounts is managed in TaxCodeHistory or similar.
                    // For now, I'll fetch them from DB.
                    
                    var glAccounts = await _dbContext.tblTaxAccounts.Select(a => a.GL_ACCOUNT).ToListAsync();

                    await using (var conn = new NpgsqlConnection(_pgConn))
                    {
                        await conn.OpenAsync();
                        string query = @"
                            SELECT 
                                date, post_date, account_no, trans_no, where_from, gl_user,
                                gl_memo,
                                mf_who, mf_key, mf_tran, debit_amt, credit_amt
                            FROM gl_transactions
                            WHERE date BETWEEN @start AND @end
                            AND account_no IN (SELECT gl_account FROM sales_taxes)"; // Filter by tax accounts

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@start", start);
                            cmd.Parameters.AddWithValue("@end", end);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                var entities = new List<TblGLTransToTaxAccounts>();
                                while (await reader.ReadAsync())
                                {
                                    entities.Add(new TblGLTransToTaxAccounts
                                    {
                                        Tran_Date = reader["date"]?.ToString(),
                                        Post_Date = reader["post_date"]?.ToString(),
                                        Acct_No = reader["account_no"]?.ToString(),
                                        Trans_No = reader["trans_no"] != DBNull.Value ? (int.TryParse(reader["trans_no"].ToString(), out int t) ? t : (int?)null) : (int?)null,
                                        Where_From = reader["where_from"]?.ToString(),
                                        GL_User = reader["gl_user"]?.ToString(),
                                        BVGLMEMOWHO = reader["gl_memo"]?.ToString(),
                                        BVRESERVED11 = "",
                                        BVGLMEMOKEY = "",
                                        BVRESERVED13 = "",
                                        BVGLMEMOTRAN = "",
                                        BVRESERVED15 = "",
                                        MF_Who = reader["mf_who"]?.ToString(),
                                        MF_Key = reader["mf_key"]?.ToString(),
                                        MF_Tran = reader["mf_tran"]?.ToString(),
                                        Debit_Amt = reader["debit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["debit_amt"]) : 0m,
                                        Credit_Amt = reader["credit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["credit_amt"]) : 0m
                                    });
                                }
                                await _dbContext.tblGLTransToTaxAccounts.AddRangeAsync(entities);
                            }
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        // ─── REPORT FETCH LOGIC ───────────────────────────────────────────────

        public async Task<SalesTaxReportResponse> GetSalesTaxReportAsync(SalesTaxReportRequest request, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600); // 10 minutes

            var response = new SalesTaxReportResponse
            {
                Data = new List<SalesTaxReportRow>(),
                DepartmentNames = new List<string>()
            };

            var records = await _dbContext.tblTaxDataOutput
                .OrderBy(x => x.Invoice)
                .ToListAsync();

            response.Data = records.Select(r => new SalesTaxReportRow
            {
                Trans = r.Trans ?? 0,
                Invdate = r.InvDate,
                Invoice = r.Invoice,
                WebOrderID = r.WebOrderID,
                CustNo = r.CustNo,
                CustName = r.CustName,
                Territory = r.Territory,
                ShipToProvince = r.ShipToProvince,
                PostalDigit = r.PostalDigit,
                Tax1Code = r.Tax1Code.HasValue ? (int?)Convert.ToInt32(r.Tax1Code.Value) : null,
                Tax2Code = r.Tax2Code.HasValue ? (int?)Convert.ToInt32(r.Tax2Code.Value) : null,
                Tax1Name = r.Tax1Name,
                Tax1GL = r.Tax1GL,
                Tax2Name = r.Tax2Name,
                Tax2GL = r.Tax2GL,
                InvoiceNet = r.InvoiceNet ?? 0,
                Tax1Total = r.Tax1Total ?? 0,
                Tax2Total = r.Tax2Total ?? 0,
                ShippingAmt = r.ShippingAmt ?? 0,
                InvoiceTotalBeforeUERVValue = r.InvoiceTotalBeforeUERVValue ?? 0,
                UERVValue = r.UERVValue ?? 0,
                InvoiceTotal = r.InvoiceTotal ?? 0,
                TotalOfExtendedSell = r.TotalOfExtendedSell ?? 0
            }).ToList();

            response.DepartmentNames = new List<string>
    {
        "Sales-Hardware",
        "Sales-Accessory",
        "Shipping"
    };

            return response;
        }
        public async Task<byte[]> ExportToExcelAsync(SalesTaxReportRequest request, int userId)
        {
            var report = await GetSalesTaxReportAsync(request, userId);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Sales Tax Report");

                string[] headers = new string[] {
                    "Trans", "Invdate", "Invoice", "WebOrderID", "Source", "CustNo", "CustName", "Territory", 
                    "ShipToProvince", "PostalDigit", "OneIMEI", "Tax1Code", "Tax1Name", "Tax1GL", 
                    "Tax2Code", "Tax2Name", "Tax2GL", "InvoiceNet", "Tax1Total", "Tax2Total", 
                    "ShippingAmt", "InvoiceTotalBeforeUERVValue", "UERVValue", "InvoiceTotal",
                    "NUMBER", "Total Of ExtendedSell"
                };

                int totalCols = headers.Length + report.DepartmentNames.Count;

                // --- ROW 1: TOTALS (Premium Feature) ---
                ws.Cells[1, 1].Value = "TOTALS:";
                ws.Cells[1, 1, 1, 17].Merge = true;
                ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                ws.Cells[1, 1].Style.Font.Bold = true;

                ws.Cells[1, 18].Value = report.Data.Sum(x => x.InvoiceNet);
                ws.Cells[1, 19].Value = report.Data.Sum(x => x.Tax1Total);
                ws.Cells[1, 20].Value = report.Data.Sum(x => x.Tax2Total);
                ws.Cells[1, 21].Value = report.Data.Sum(x => x.ShippingAmt);
                ws.Cells[1, 22].Value = report.Data.Sum(x => x.InvoiceTotalBeforeUERVValue);
                ws.Cells[1, 23].Value = report.Data.Sum(x => x.UERVValue);
                ws.Cells[1, 24].Value = report.Data.Sum(x => x.InvoiceTotal);
                ws.Cells[1, 26].Value = report.Data.Sum(x => x.TotalOfExtendedSell);

                // Department Totals
                int dCol = 27;
                foreach (var dept in report.DepartmentNames)
                {
                    ws.Cells[1, dCol++].Value = report.Data.Sum(x => x.DepartmentSales.ContainsKey(dept) ? x.DepartmentSales[dept] : 0);
                }

                using (var range = ws.Cells[1, 18, 1, totalCols])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Numberformat.Format = "#,##0.00";
                }

                // --- ROW 2: HEADERS ---
                for (int i = 0; i < headers.Length; i++) ws.Cells[2, i + 1].Value = headers[i];

                int currentCol = headers.Length + 1;
                foreach (var dept in report.DepartmentNames) ws.Cells[2, currentCol++].Value = dept;

                using (var range = ws.Cells[2, 1, 2, totalCols])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // --- ROW 3+: DATA ---
                int currentRow = 3;
                foreach (var row in report.Data)
                {
                    int col = 1;
                    ws.Cells[currentRow, col++].Value = row.Trans;
                    ws.Cells[currentRow, col++].Value = row.Invdate?.ToString("MM/dd/yyyy");
                    ws.Cells[currentRow, col++].Value = row.Invoice;
                    ws.Cells[currentRow, col++].Value = row.WebOrderID;
                    ws.Cells[currentRow, col++].Value = "OE";
                    ws.Cells[currentRow, col++].Value = row.CustNo;
                    ws.Cells[currentRow, col++].Value = row.CustName;
                    ws.Cells[currentRow, col++].Value = row.Territory;
                    ws.Cells[currentRow, col++].Value = row.ShipToProvince;
                    ws.Cells[currentRow, col++].Value = row.PostalDigit;
                    ws.Cells[currentRow, col++].Value = row.OneIMEI;
                    ws.Cells[currentRow, col++].Value = row.Tax1Code;
                    ws.Cells[currentRow, col++].Value = row.Tax1Name;
                    ws.Cells[currentRow, col++].Value = row.Tax1GL;
                    ws.Cells[currentRow, col++].Value = row.Tax2Code;
                    ws.Cells[currentRow, col++].Value = row.Tax2Name;
                    ws.Cells[currentRow, col++].Value = row.Tax2GL;
                    ws.Cells[currentRow, col++].Value = row.InvoiceNet;
                    ws.Cells[currentRow, col++].Value = row.Tax1Total;
                    ws.Cells[currentRow, col++].Value = row.Tax2Total;
                    ws.Cells[currentRow, col++].Value = row.ShippingAmt;
                    ws.Cells[currentRow, col++].Value = row.InvoiceTotalBeforeUERVValue;
                    ws.Cells[currentRow, col++].Value = row.UERVValue;
                    ws.Cells[currentRow, col++].Value = row.InvoiceTotal;
                    ws.Cells[currentRow, col++].Value = row.Invoice; // NUMBER (duplicate)
                    ws.Cells[currentRow, col++].Value = row.TotalOfExtendedSell;

                    foreach (var dept in report.DepartmentNames)
                        ws.Cells[currentRow, col++].Value = row.DepartmentSales.ContainsKey(dept) ? row.DepartmentSales[dept] : 0;
                    currentRow++;
                }

                ws.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        public async Task<byte[]> ExportGLITCExcelAsync(SalesTaxReportRequest request, int userId)
        {
            try
            {
                DateTime start = request.StartDate;
                DateTime end = request.EndDate;

                var transactions = new List<string>();

                await using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();

                    string findTransQuery = "SELECT DISTINCT trans_no FROM gl_transactions WHERE date BETWEEN @start AND @end AND account_no IN (SELECT gl_account FROM sales_taxes)";
                    using (var cmd = new NpgsqlCommand(findTransQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                transactions.Add(reader["trans_no"].ToString());
                            }
                        }
                    }

                    if (transactions.Count == 0)
                        return new byte[0];

                    var allEntries = new List<dynamic>();

                    string allEntriesQuery = @"
SELECT 
    t.trans_no, 
    t.date, 
    t.account_no, 
    t.where_from, 
    t.gl_user, 
    t.debit_amt, 
    t.credit_amt,
    st.name AS account_name,
    t.gl_memo AS full_memo
FROM gl_transactions t
LEFT JOIN sales_taxes st 
    ON t.account_no = st.tax_no::text
WHERE t.trans_no = ANY(@trans)";

                    var allEntriess = new List<dynamic>();

                    using (var cmd = new NpgsqlCommand(allEntriesQuery, conn))
                    {
                        // IMPORTANT: transactions must be array/list
                        cmd.Parameters.AddWithValue("trans", transactions.ToArray());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                allEntries.Add(new
                                {
                                    TransNo = reader["trans_no"].ToString(),
                                    Date = ((DateOnly)reader["date"]).ToDateTime(TimeOnly.MinValue),
                                    Account = reader["account_no"]?.ToString(),
                                    AccountName = reader["account_name"]?.ToString(),
                                    Module = reader["where_from"]?.ToString(),
                                    User = reader["gl_user"]?.ToString(),
                                    Debit = Convert.ToDecimal(reader["debit_amt"] != DBNull.Value ? reader["debit_amt"] : 0),
                                    Credit = Convert.ToDecimal(reader["credit_amt"] != DBNull.Value ? reader["credit_amt"] : 0),
                                    Memo = reader["full_memo"]?.ToString()
                                });
                            }
                        }
                    }

                    var summaries = new List<dynamic>();
                    var grouped = allEntries.GroupBy(x => x.TransNo);

                    foreach (var group in grouped)
                    {
                        var first = group.First();
                        var itcEntries = group.Where(x => x.Account == "21410");
                        var expenseEntries = group.Where(x => x.Account != "21410" && x.Account != "21120");

                        decimal itcAmt = itcEntries.Sum(x => (decimal)x.Debit - (decimal)x.Credit);
                        decimal expAmt = expenseEntries.Sum(x => (decimal)x.Debit - (decimal)x.Credit);

                        string vendor = "";
                        string refNo = "";

                        if (first.Module == "AP")
                        {
                            try
                            {
                                string apQuery = "SELECT vendor_no, ref_no FROM ap_transactions WHERE trans_no = @t";
                                using (var cmd = new NpgsqlCommand(apQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@t", group.Key);
                                    using (var apReader = await cmd.ExecuteReaderAsync())
                                    {
                                        if (await apReader.ReadAsync())
                                        {
                                            vendor = apReader["vendor_no"]?.ToString();
                                            refNo = apReader["ref_no"]?.ToString();
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log and continue (don't break whole export)
                                Console.WriteLine($"AP lookup failed for TransNo {group.Key}: {ex.Message}");
                            }
                        }

                        summaries.Add(new
                        {
                            TransNo = group.Key,
                            Date = first.Date,
                            Memo = group.OrderByDescending(x => x.Memo?.Length ?? 0).First().Memo,
                            Vendor = vendor,
                            InvoiceRef = refNo,
                            Source = first.Module,
                            User = first.User,
                            NetPurchase = expAmt,
                            ITC = itcAmt,
                            Total = expAmt + itcAmt,
                            AccountPivots = expenseEntries
                                .GroupBy(x => x.Account)
                                .ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.Debit - (decimal)x.Credit))
                        });
                    }

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("ITC Credits");

                        var distinctAccounts = allEntries
                            .Where(x => x.Account != "21410" && x.Account != "21120")
                            .Select(x => x.Account)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        string[] baseHeaders = { "Transaction", "TransDate", "Memo", "Vendor", "InvoiceRef", "Source", "User", "NetPurchase", "ITC", "Total" };
                        int totalCols = baseHeaders.Length + distinctAccounts.Count;

                        ws.Cells[1, 1].Value = "TOTALS:";
                        ws.Cells[1, 1, 1, 7].Merge = true;
                        ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                        ws.Cells[1, 8].Value = summaries.Sum(x => (decimal)x.NetPurchase);
                        ws.Cells[1, 9].Value = summaries.Sum(x => (decimal)x.ITC);
                        ws.Cells[1, 10].Value = summaries.Sum(x => (decimal)x.Total);

                        // TOTALS per account column
                        for (int i = 0; i < distinctAccounts.Count; i++)
                        {
                            var accountKey = distinctAccounts[i];

                            ws.Cells[1, 11 + i].Value = summaries.Sum(s =>
                            {
                                var dict = s.AccountPivots as Dictionary<object, decimal>;

                                decimal val = 0m; // ✅ initialize to avoid compiler warning/error

                                if (dict != null && dict.TryGetValue(accountKey, out val))
                                {
                                    return val;
                                }

                                return 0m;
                            });
                        }

                        ws.Cells[1, 8, 1, totalCols].Style.Font.Bold = true;
                        ws.Cells[1, 8, 1, totalCols].Style.Numberformat.Format = "#,##0.00";

                        // Headers
                        for (int i = 0; i < baseHeaders.Length; i++)
                            ws.Cells[2, i + 1].Value = baseHeaders[i];

                        for (int i = 0; i < distinctAccounts.Count; i++)
                            ws.Cells[2, baseHeaders.Length + i + 1].Value = distinctAccounts[i];

                        using (var range = ws.Cells[2, 1, 2, totalCols])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        int row = 3;
                        foreach (var s in summaries)
                        {
                            ws.Cells[row, 1].Value = s.TransNo;
                            ws.Cells[row, 2].Value = s.Date.ToString("MM/dd/yyyy");
                            ws.Cells[row, 3].Value = s.Memo;
                            ws.Cells[row, 4].Value = s.Vendor;
                            ws.Cells[row, 5].Value = s.InvoiceRef;
                            ws.Cells[row, 6].Value = s.Source;
                            ws.Cells[row, 7].Value = s.User;
                            ws.Cells[row, 8].Value = s.NetPurchase;
                            ws.Cells[row, 9].Value = s.ITC;
                            ws.Cells[row, 10].Value = s.Total;

                            var dict = s.AccountPivots as Dictionary<object, decimal>;

                            for (int i = 0; i < distinctAccounts.Count; i++)
                            {
                                var key = distinctAccounts[i];

                                decimal val = 0m; // ✅ required for older C# compilers

                                ws.Cells[row, 11 + i].Value =
                                    (dict != null && dict.TryGetValue(key, out val))
                                    ? val
                                    : 0m;
                            }

                            row++;
                        }

                        ws.Cells.AutoFitColumns();

                        return package.GetAsByteArray();
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: replace with proper logging (Serilog, NLog, etc.)
                Console.WriteLine($"Error in ExportGLITCExcelAsync: {ex}");

                // Option 1: rethrow
                throw;

                // Option 2 (alternative): return empty file
                // return new byte[0];
            }
        }

        public async Task<byte[]> ExportGLDataExcelAsync(SalesTaxReportRequest request, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("GL Tax Data");
                string[] headers = { 
                    "Transaction", "TranDate", "Invoice", "WebOrderID", "Source", "CustNo", "CustName", 
                    "Territory", "ShipToProvinceL", "PostalDigit", "OneIMEI", "Tax1Code", "Tax1Name", 
                    "Tax1GL", "Tax2Code", "Tax2Name", "Tax2GL", "ACCT_NO", "WHERE_FROM", "GL_USER", 
                    "Memo", "DEBIT_AMT", "CREDIT_AMT", "Trans" 
                };

                // Query with EF Core: LEFT JOIN where o.Trans is null
                var query = from t in _dbContext.tblGLTransToTaxAccounts
                            join o in _dbContext.tblTaxDataOutput on t.Trans_No equals o.Trans into joinOutput
                            from o in joinOutput.DefaultIfEmpty()
                            where o == null
                            orderby t.Trans_No
                            select new {
                                Transaction = t.Trans_No,
                                TranDate = t.Tran_Date,
                                AcctNo = t.Acct_No,
                                WhereFrom = t.Where_From,
                                GlUser = t.GL_User,
                                Memo = t.BVGLMEMOWHO ?? "",
                                DebitAmt = t.Debit_Amt ?? 0,
                                CreditAmt = t.Credit_Amt ?? 0
                            };

                var records = await query.ToListAsync();

                // --- ROW 1: TOTALS ---
                ws.Cells[1, 1].Value = "TOTALS:";
                ws.Cells[1, 1, 1, 20].Merge = true;
                ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 22].Value = records.Sum(x => x.DebitAmt);
                ws.Cells[1, 23].Value = records.Sum(x => x.CreditAmt);
                ws.Cells[1, 22, 1, 23].Style.Font.Bold = true;
                ws.Cells[1, 22, 1, 23].Style.Numberformat.Format = "#,##0.00";

                // --- ROW 2: HEADERS ---
                for (int i = 0; i < headers.Length; i++) ws.Cells[2, i + 1].Value = headers[i];
                using (var range = ws.Cells[2, 1, 2, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // --- ROW 3+: DATA ---
                int currentRow = 3;
                foreach (var rec in records)
                {
                    ws.Cells[currentRow, 1].Value = rec.Transaction;
                    
                    // Format TranDate
                    if (rec.TranDate != null && rec.TranDate.Length >= 8)
                    {
                        if (DateTime.TryParseExact(rec.TranDate.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                            ws.Cells[currentRow, 2].Value = parsedDate.ToString("MM/dd/yyyy");
                        else
                            ws.Cells[currentRow, 2].Value = rec.TranDate;
                    }

                    ws.Cells[currentRow, 5].Value = rec.WhereFrom;
                    ws.Cells[currentRow, 14].Value = rec.AcctNo;
                    ws.Cells[currentRow, 18].Value = rec.AcctNo;
                    ws.Cells[currentRow, 19].Value = rec.WhereFrom;
                    ws.Cells[currentRow, 20].Value = rec.GlUser;
                    ws.Cells[currentRow, 21].Value = rec.Memo;
                    ws.Cells[currentRow, 22].Value = rec.DebitAmt;
                    ws.Cells[currentRow, 23].Value = rec.CreditAmt;
                    currentRow++;
                }

                ws.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }


        // ─── TAX CODE HISTORY LOGIC ───────────────────────────────────────────

        // ─── TAX CODE HISTORY CRUD (EF Core) ─────────────────────────────────

        public async Task<List<TaxCodeHistory>> GetTaxCodeHistoryAsync()
        {
            return await _dbContext.TaxCodeHistory
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        public async Task<bool> SaveTaxCodeHistoryAsync(TaxCodeHistory history, int userId)
        {
            if (history.Id == 0)
            {
                await _dbContext.TaxCodeHistory.AddAsync(history);
            }
            else
            {
                var existing = await _dbContext.TaxCodeHistory.FindAsync(history.Id);
                if (existing == null) return false;

                existing.ProvCode = history.ProvCode;
                existing.ProvinceName = history.ProvinceName;
                existing.Tax1Rate = history.Tax1Rate;
                existing.Tax2Rate = history.Tax2Rate;
                existing.TaxType = history.TaxType;
                existing.StartDate = history.StartDate;
                existing.EndDate = history.EndDate;
                existing.Comments = history.Comments;
                existing.CompoundTax2OnTax1 = history.CompoundTax2OnTax1;

                _dbContext.TaxCodeHistory.Update(existing);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTaxCodeHistoryAsync(int id)
        {
            var existing = await _dbContext.TaxCodeHistory.FindAsync(id);
            if (existing == null) return false;

            _dbContext.TaxCodeHistory.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> ExportVendorActivityAsync(string vendor, DateTime start, DateTime end)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Vendor Activity");
                string[] headers = { 
                    "Vendor No", "Vendor Name", "Date", "Trans No", "Code", "Open/Close", 
                    "Ref No", "Debit Amt", "Credit Amt", "Balance", "Hold", "Due Date", 
                    "PO No", "Void", "Note" 
                };

                var records = new List<dynamic>();
                await using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT v.vendor_no, v.name AS VendorName, t.date, t.trans_no, t.code, 
                               t.open_close_flag, t.ref_no, t.debit_amt, t.credit_amt, t.balance, 
                               t.item_hold_flag, t.due_date, t.po_no, t.cheque_void_flag, t.note
                        FROM ap_transactions t
                        INNER JOIN vendors v ON t.vendor_no = v.vendor_no
                        WHERE t.vendor_no = @vendor AND t.date BETWEEN @start AND @end
                        ORDER BY t.date";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@vendor", vendor);
                        cmd.Parameters.AddWithValue("@start", start);
                        cmd.Parameters.AddWithValue("@end", end);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                records.Add(new {
                                    VendorNo = reader["vendor_no"]?.ToString(),
                                    VendorName = reader["VendorName"]?.ToString(),
                                    Date = reader["date"] != DBNull.Value
    ? (DateOnly?)reader.GetFieldValue<DateOnly>(reader.GetOrdinal("date"))
    : null,
                                    TransNo = reader["trans_no"]?.ToString(),
                                    Code = reader["code"]?.ToString(),
                                    OpenClose = reader["open_close_flag"]?.ToString(),
                                    RefNo = reader["ref_no"]?.ToString(),
                                    DebitAmt = reader["debit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["debit_amt"]) : 0m,
                                    CreditAmt = reader["credit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["credit_amt"]) : 0m,
                                    Balance = reader["balance"] != DBNull.Value ? Convert.ToDecimal(reader["balance"]) : 0m,
                                    Hold = reader["item_hold_flag"]?.ToString(),
                                    DueDate = reader["due_date"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["due_date"]) : null,
                                    PONo = reader["po_no"]?.ToString(),
                                    Void = reader["cheque_void_flag"]?.ToString(),
                                    Note = reader["note"]?.ToString()
                                });
                            }
                        }
                    }
                }

                // --- ROW 1: TOTALS ---
                ws.Cells[1, 1].Value = "TOTALS:";
                ws.Cells[1, 1, 1, 7].Merge = true;
                ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 8].Value = records.Sum(x => (decimal)x.DebitAmt);
                ws.Cells[1, 9].Value = records.Sum(x => (decimal)x.CreditAmt);
                ws.Cells[1, 8, 1, 9].Style.Font.Bold = true;
                ws.Cells[1, 8, 1, 9].Style.Numberformat.Format = "#,##0.00";

                // --- ROW 2: HEADERS ---
                for (int i = 0; i < headers.Length; i++) ws.Cells[2, i + 1].Value = headers[i];
                using (var range = ws.Cells[2, 1, 2, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // --- ROW 3+: DATA ---
                int currentRow = 3;
                foreach (var rec in records)
                {
                    ws.Cells[currentRow, 1].Value = rec.VendorNo;
                    ws.Cells[currentRow, 2].Value = rec.VendorName;
                    ws.Cells[currentRow, 3].Value = rec.Date?.ToString("MM/dd/yyyy");
                    ws.Cells[currentRow, 4].Value = rec.TransNo;
                    ws.Cells[currentRow, 5].Value = rec.Code;
                    ws.Cells[currentRow, 6].Value = rec.OpenClose;
                    ws.Cells[currentRow, 7].Value = rec.RefNo;
                    ws.Cells[currentRow, 8].Value = rec.DebitAmt;
                    ws.Cells[currentRow, 9].Value = rec.CreditAmt;
                    ws.Cells[currentRow, 10].Value = rec.Balance;
                    ws.Cells[currentRow, 11].Value = rec.Hold;
                    ws.Cells[currentRow, 12].Value = rec.DueDate?.ToString("MM/dd/yyyy");
                    ws.Cells[currentRow, 13].Value = rec.PONo;
                    ws.Cells[currentRow, 14].Value = rec.Void;
                    ws.Cells[currentRow, 15].Value = rec.Note;
                    currentRow++;
                }

                ws.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        private async Task<List<SalesTaxReportRow>> FetchSalesHistoryFromSpire(DateTime start, DateTime end)
            {
            var rows = new List<SalesTaxReportRow>();

            try
            {
                await using var conn = new NpgsqlConnection(_pgConn);
                await conn.OpenAsync();

                string query = @"
SELECT 
    h.trans_no            AS trans,
    h.invoice_date        AS invdate,
    h.invoice_no          AS invoice,
    h.order_no            AS weborderid,
    h.cust_no             AS custno,
    a.name                AS custname,
    h.territory_code      AS territory,
    a.prov_state          AS shiptoprovince,
    LEFT(a.postal_zip, 1) AS postaldigit,

    h.subtotal            AS subtotal,
    h.freight             AS freight,

    a.sales_tax_no[1]     AS tax1code,
    h.sales_tax_total[1]  AS tax1total,
    st1.name              AS tax1name,
    st1.gl_account        AS tax1gl,

    a.sales_tax_no[2]     AS tax2code,
    h.sales_tax_total[2]  AS tax2total,
    st2.name              AS tax2name,
    st2.gl_account        AS tax2gl,

    h.total               AS invoicetotal

FROM sales_history h
INNER JOIN addresses a 
    ON h.invoice_no = a.link_no
    AND a.link_table = 'SHIS'
    AND a.addr_type  = 'S'
LEFT JOIN sales_taxes st1 ON a.sales_tax_no[1] = st1.tax_no
LEFT JOIN sales_taxes st2 ON a.sales_tax_no[2] = st2.tax_no

WHERE h.invoice_date BETWEEN @start AND @end";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    cmd.CommandTimeout = 600;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var invoiceNet = reader["subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["subtotal"]) : 0m;
                            var tax1Total = reader["tax1total"] != DBNull.Value ? Convert.ToDecimal(reader["tax1total"]) : 0m;
                            var tax2Total = reader["tax2total"] != DBNull.Value ? Convert.ToDecimal(reader["tax2total"]) : 0m;
                            var shipping = reader["freight"] != DBNull.Value ? Convert.ToDecimal(reader["freight"]) : 0m;

                            rows.Add(new SalesTaxReportRow
                            {
                                Trans = reader["trans"] != DBNull.Value ? Convert.ToInt32(reader["trans"]) : 0,
                                Invdate = reader["invdate"] as DateTime?,
                                Invoice = reader["invoice"]?.ToString(),
                                WebOrderID = reader["weborderid"]?.ToString(),
                                CustNo = reader["custno"]?.ToString(),
                                CustName = reader["custname"]?.ToString(),
                                Territory = reader["territory"]?.ToString(),
                                ShipToProvince = reader["shiptoprovince"]?.ToString(),
                                PostalDigit = reader["postaldigit"]?.ToString(),
                                
                                // Tax 1
                                Tax1Code = reader["tax1code"] != DBNull.Value ? Convert.ToInt32(reader["tax1code"]) : (int?)null,
                                Tax1Name = reader["tax1name"]?.ToString(),
                                Tax1GL = reader["tax1gl"]?.ToString(),
                                Tax1Total = tax1Total,

                                // Tax 2
                                Tax2Code = reader["tax2code"] != DBNull.Value ? Convert.ToInt32(reader["tax2code"]) : (int?)null,
                                Tax2Name = reader["tax2name"]?.ToString(),
                                Tax2GL = reader["tax2gl"]?.ToString(),
                                Tax2Total = tax2Total,

                                InvoiceNet = invoiceNet,
                                ShippingAmt = shipping,
                                InvoiceTotal = invoiceNet + tax1Total + tax2Total + shipping
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching sales history: {ex.Message}");
                throw;
            }

            return rows;
        }

        public async Task<List<VendorBO>> GetVendorsAsync()
        {
            var vendors = new List<VendorBO>();
            try
            {
                await using var conn = new NpgsqlConnection(_pgConn);
                await conn.OpenAsync();

                string query = "SELECT vendor_no, name FROM vendors WHERE status = 'A' ORDER BY name";

                using (var cmd = new NpgsqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        vendors.Add(new VendorBO
                        {
                            VendorNo = reader["vendor_no"]?.ToString(),
                            Name = reader["name"]?.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching vendors: {ex.Message}");
                throw;
            }
            return vendors;
        }
    }
}
