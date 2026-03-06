using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DAL.Inventory.OutputInvoice
{
    public class OutputInvoiceDA : IOutputInvoice
    {
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public OutputInvoiceDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
            _pgConn = configuration.GetConnectionString("spire_Connection");
        }

        // GET PAGED LIST
        public async Task<PagedInvoiceResponse> GetInvoiceListPaged(int pageNumber, int pageSize)
        {
            var response = new PagedInvoiceResponse
            {
                Data = new List<InvoiceItem>()
            };

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                string countSql = "SELECT COUNT(*) FROM tblInvoiceList";

                using (var cmd = new SqlCommand(countSql, conn))
                {
                    response.TotalCount = (int)await cmd.ExecuteScalarAsync();
                }

                string dataSql = @"SELECT InvoiceNo 
                                   FROM tblInvoiceList
                                   ORDER BY ID
                                   OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY";

                using (var cmd = new SqlCommand(dataSql, conn))
                {
                    cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@size", pageSize);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(new InvoiceItem
                            {
                                InvoiceNo = reader["InvoiceNo"].ToString()
                            });
                        }
                    }
                }
            }

            return response;
        }

        // GET FULL LIST
        public async Task<List<InvoiceItem>> GetInvoiceList()
        {
            var list = new List<InvoiceItem>();

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                string sql = "SELECT InvoiceNo FROM  tblInvoiceList ORDER BY ID";

                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new InvoiceItem
                        {
                            InvoiceNo = reader["InvoiceNo"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        // CHECK POSTGRES HISTORY
        public async Task<string> CheckSpireHistory(string invoiceNo)
        {
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                string sql = @"SELECT invoice_no
                               FROM sales_history
                               WHERE invoice_no = @inv
                               LIMIT 1";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@inv", invoiceNo);

                    var result = await cmd.ExecuteScalarAsync();

                    return result?.ToString() ?? "";
                }
            }
        }

        // CLEAR INVOICE LIST
        public async Task<bool> ClearInvoiceList()
        {
            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                string sql = "DELETE FROM tblInvoiceList";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return true;
        }

        // PROCESS SINGLE INVOICE
        public async Task<bool> ProcessInvoiceOutput(string invoiceNo, string folder, string prefix, bool isSpire)
        {
            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string formattedInv = invoiceNo.PadLeft(10, '0');

                string fileName = string.IsNullOrEmpty(prefix)
                    ? $"{formattedInv}.pdf"
                    : $"{prefix}-{formattedInv}.pdf";

                string fullPath = Path.Combine(folder, fileName);

                Console.WriteLine("Saving PDF to: " + fullPath);

                InvoiceDetail data = await GetInvoiceDataFromDb(invoiceNo, isSpire);

                if (data == null || data.Lines.Count == 0)
                {
                    Console.WriteLine("No data found for invoice " + invoiceNo);
                    return false;
                }

                using (PdfDocument pdf = new PdfDocument())
                {
                    PdfPageBase page = pdf.Pages.Add(PdfPageSize.A4);

                    PdfFont titleFont = new PdfFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
                    PdfFont normalFont = new PdfFont(PdfFontFamily.Helvetica, 9);

                    float y = 30;

                    page.Canvas.DrawString("Invoice", titleFont, PdfBrushes.Black, 250, y);
                    y += 40;

                    page.Canvas.DrawString("Invoice No: " + formattedInv, normalFont, PdfBrushes.Black, 30, y);
                    y += 20;

                    page.Canvas.DrawString("Customer: " + data.BillToName, normalFont, PdfBrushes.Black, 30, y);
                    y += 20;

                    page.Canvas.DrawString("Date: " + data.InvoiceDate, normalFont, PdfBrushes.Black, 30, y);
                    y += 40;

                    decimal total = 0;

                    foreach (var line in data.Lines)
                    {
                        string text = $"{line.PartNo}   {line.Description}   Qty:{line.Qty}   Price:{line.Price}";
                        page.Canvas.DrawString(text, normalFont, PdfBrushes.Black, 30, y);

                        total += line.Qty * line.Price;

                        y += 20;

                        if (y > 750)
                        {
                            page = pdf.Pages.Add(PdfPageSize.A4);
                            y = 30;
                        }
                    }

                    y += 20;

                    page.Canvas.DrawString("Total: " + total.ToString("C"), titleFont, PdfBrushes.Black, 30, y);

                    Console.WriteLine("Saving PDF to: " + fullPath);

                    pdf.SaveToFile(fullPath);

                    Console.WriteLine("Exists after save: " + File.Exists(fullPath));
                }

                if (File.Exists(fullPath))
                    Console.WriteLine("PDF Created Successfully");
                else
                    Console.WriteLine("PDF NOT Created");

                return true;
            }
            catch (Exception ex)
            {
                File.WriteAllText("invoice_error.txt", ex.ToString());
                return false;
            }
        }

        // FETCH DATA FROM DATABASE
        private async Task<InvoiceDetail> GetInvoiceDataFromDb(string invoiceNo, bool isSpire)
        {
            var detail = new InvoiceDetail();

            try
            {
                if (isSpire)
                {
                    using (var conn = new NpgsqlConnection(_pgConn))
                    {
                        await conn.OpenAsync();

                        // 1. Header Query (PostgreSQL)
                        string headerSql = @"SELECT cust_name, cust_no, invoice_date
                                     FROM sales_history
                                     WHERE invoice_no=@inv
                                     LIMIT 1";

                        using (var cmd = new NpgsqlCommand(headerSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@inv", invoiceNo);
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                if (await r.ReadAsync())
                                {
                                    detail.BillToName = r["cust_name"]?.ToString() ?? "N/A";
                                    detail.CustNo = r["cust_no"]?.ToString() ?? "N/A";

                                    // Naya Fix: DateOnly handling
                                    var dateVal = r["invoice_date"];
                                    if (dateVal != DBNull.Value)
                                    {
                                        // Agar DateOnly hai toh usko directly string mein convert karein
                                        detail.InvoiceDate = dateVal.ToString();
                                    }
                                    else
                                    {
                                        detail.InvoiceDate = "N/A";
                                    }
                                }
                            }
                        }

                        // 2. Lines Query (PostgreSQL)
                        string lineSql = @"SELECT part_no, description, order_qty, unit_price
                                   FROM sales_history_items
                                   WHERE invoice_no=@inv";

                        using (var cmd = new NpgsqlCommand(lineSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@inv", invoiceNo);
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                while (await r.ReadAsync())
                                {
                                    detail.Lines.Add(new InvoiceItemLine
                                    {
                                        PartNo = r["part_no"]?.ToString() ?? "",
                                        Description = r["description"]?.ToString() ?? "",
                                        Qty = r["order_qty"] != DBNull.Value ? Convert.ToDecimal(r["order_qty"]) : 0,
                                        Price = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var conn = new SqlConnection(_sqlConn))
                    {
                        await conn.OpenAsync();

                        // 1. Header Query (SQL Server)
                        string headerSql = @"SELECT CUSTOMER_NAME, CUSTOMER_NO, INVOICE_DATE
                                     FROM OESalesHistoryHeader
                                     WHERE INVOICE_NO=@inv";

                        using (var cmd = new SqlCommand(headerSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@inv", invoiceNo);
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                if (await r.ReadAsync())
                                {
                                    detail.BillToName = r["CUSTOMER_NAME"]?.ToString() ?? "N/A";
                                    detail.CustNo = r["CUSTOMER_NO"]?.ToString() ?? "N/A";
                                    detail.InvoiceDate = r["INVOICE_DATE"] != DBNull.Value
                                        ? Convert.ToDateTime(r["INVOICE_DATE"]).ToString("yyyy-MM-dd")
                                        : "N/A";
                                }
                            }
                        }

                        // 2. Lines Query (SQL Server)
                        string lineSql = @"SELECT PART_NO, DESCRIPTION, QTY_ORDERED, UNIT_PRICE
                                   FROM OESalesHistoryDetails
                                   WHERE INVOICE_NO=@inv";

                        using (var cmd = new SqlCommand(lineSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@inv", invoiceNo);
                            using (var r = await cmd.ExecuteReaderAsync())
                            {
                                while (await r.ReadAsync())
                                {
                                    detail.Lines.Add(new InvoiceItemLine
                                    {
                                        PartNo = r["PART_NO"]?.ToString() ?? "",
                                        Description = r["DESCRIPTION"]?.ToString() ?? "",
                                        Qty = r["QTY_ORDERED"] != DBNull.Value ? Convert.ToDecimal(r["QTY_ORDERED"]) : 0,
                                        Price = r["UNIT_PRICE"] != DBNull.Value ? Convert.ToDecimal(r["UNIT_PRICE"]) : 0
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Yahan error log hoga
                Console.WriteLine($"Error fetching data for Invoice {invoiceNo}: {ex.Message}");

                // Optionally, aap error file mein bhi likh sakte hain
                await File.AppendAllTextAsync("db_error_log.txt", $"{DateTime.Now}: {ex.ToString()}{Environment.NewLine}");

                // Return empty detail or rethrow
                return detail;
            }

            return detail;
        }

        // PROCESS ALL INVOICES
        public async Task<int> ProcessAllInvoices(string folder, string prefix, string invType)
        {
            var invoices = await GetInvoiceList();

            int counter = 0;

            foreach (var item in invoices)
            {
                counter++;

                Console.WriteLine($"Processing {counter} / {invoices.Count}");

                string spireCheck = await CheckSpireHistory(item.InvoiceNo);

                bool isSpire = !string.IsNullOrEmpty(spireCheck);

                await ProcessInvoiceOutput(item.InvoiceNo, folder, prefix, isSpire);
            }

            return counter;
        }
    }
}