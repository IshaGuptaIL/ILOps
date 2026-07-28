
using DAL.Common.Login;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
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

        // PROCESS SINGLE INVOICE (Consolidated to use GeneratePdfLayout)
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

                byte[] pdfBytes = await CreateInvoicePdfBytes(invoiceNo, prefix, isSpire);
                if (pdfBytes != null)
                {
                    await File.WriteAllBytesAsync(fullPath, pdfBytes);
                    return File.Exists(fullPath);
                }

                Console.WriteLine("No data or error generating PDF for " + invoiceNo);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing invoice {invoiceNo}: {ex.Message}");
                await File.AppendAllTextAsync("invoice_error.txt", $"{DateTime.Now}: {ex.ToString()}{Environment.NewLine}");
                return false;
            }
        }


        public async Task<InvoiceDetail> GetInvoiceDataFromDb(string invoiceNo, bool isSpire)
        {
            var detail = new InvoiceDetail();

            try
            {
                if (isSpire)
                {
                    using (var conn = new NpgsqlConnection(_pgConn))
                    {
                        await conn.OpenAsync();

                        string headerSql = @"SELECT cust_name, cust_no, invoice_date, order_no, 
                                                 '' as ship_name, '' as ship_address1, '' as ship_address2, '' as ship_city,
                                                 0 as tax_amount
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
                                    detail.OrderNo = r["order_no"]?.ToString() ?? "";
                                    detail.ShipToName = r["ship_name"]?.ToString() ?? detail.BillToName;
                                    detail.ShipToAddress1 = r["ship_address1"]?.ToString() ?? "";
                                    detail.ShipToAddress2 = r["ship_address2"]?.ToString() ?? "";
                                    detail.ShipToCity = r["ship_city"]?.ToString() ?? "";
                                    detail.GST_HST = r["tax_amount"] != DBNull.Value ? Convert.ToDecimal(r["tax_amount"]) : 0;

                                    var dateVal = r["invoice_date"];
                                    if (dateVal != DBNull.Value)
                                    {
                                        detail.InvoiceDate = Convert.ToDateTime(dateVal).ToString("MMM dd, yyyy");
                                    }
                                    else
                                    {
                                        detail.InvoiceDate = "N/A";
                                    }
                                }
                            }
                        }

                        string lineSql = @"SELECT shi.part_no, shi.description, shi.order_qty, shi.unit_price,
                                               (SELECT number FROM inventory_serial_transactions ist 
                                                WHERE ist.link_no = shi.invoice_no AND ist.part_no = shi.part_no 
                                                AND ist.sales_qty > 0 LIMIT 1) as serial_no
                                       FROM sales_history_items shi
                                       WHERE shi.invoice_no=@inv";

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
                                        Price = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0,
                                        SerialNo = r["serial_no"]?.ToString() ?? ""
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

                        string headerSql = @"SELECT CUSTOMER_NAME, CUSTOMER_NO, INVOICE_DATE, ORDER_NO
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
                                    detail.OrderNo = r["ORDER_NO"]?.ToString() ?? "";
                                    detail.InvoiceDate = r["INVOICE_DATE"] != DBNull.Value
                                        ? Convert.ToDateTime(r["INVOICE_DATE"]).ToString("MMM dd, yyyy")
                                        : "N/A";
                                }
                            }
                        }

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
                Console.WriteLine($"Error fetching data for Invoice {invoiceNo}: {ex.Message}");

                await File.AppendAllTextAsync("db_error_log.txt", $"{DateTime.Now}: {ex.ToString()}{Environment.NewLine}");

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

        private void GeneratePdfLayout(PdfDocument pdf, InvoiceDetail data, string formattedInv)
        {
            pdf.PageSettings.Size = PdfPageSize.A4;
            pdf.PageSettings.Margins.All = 0;
            PdfPageBase page = pdf.Pages.Add();

            PdfBrush blackBrush = PdfBrushes.Black;
            PdfBrush redBrush = new PdfSolidBrush(new PdfRGBColor(234, 18, 35)); // Rogers Red
            PdfBrush grayBrush = new PdfSolidBrush(new PdfRGBColor(245, 245, 245));
            PdfBrush darkGrayBrush = new PdfSolidBrush(new PdfRGBColor(80, 80, 80));
            PdfPen thinPen = new PdfPen(PdfBrushes.Black, 0.5f);
            PdfPen thickPen = new PdfPen(PdfBrushes.Black, 1.0f);
            PdfPen lightPen = new PdfPen(new PdfSolidBrush(new PdfRGBColor(200, 200, 200)), 0.5f);

            // Define fonts (Sans-serif to match Rogers brand)
            PdfFont titleFont = new PdfFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
            PdfFont logoFont = new PdfFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
            PdfFont headerFont = new PdfFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
            PdfFont normalFont = new PdfFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
            PdfFont normalBoldFont = new PdfFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
            PdfFont smallFont = new PdfFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Regular);
            PdfFont smallBoldFont = new PdfFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);

            float pageWidth = page.Canvas.ClientSize.Width;
            float leftMargin = 40;
            float rightMargin = 40;
            float y = 35;

            // ============== HEADER SECTION ==============
            // Top Left: "Your Rogers Bill"
            page.Canvas.DrawString("Your Rogers Bill", titleFont, blackBrush, leftMargin, y + 5);

            // Top Center: Rogers Logo (Simulated Graphics)
            float logoX = pageWidth / 2 - 50;
            float logoY = y;
            // Draw two red overlapping circles
            page.Canvas.DrawPie(redBrush, logoX, logoY, 20, 20, 0, 360);
            page.Canvas.DrawPie(redBrush, logoX + 12, logoY, 20, 20, 0, 360);
            // Draw "ROGERS" text
            page.Canvas.DrawString("ROGERS", logoFont, redBrush, logoX + 38, logoY - 2);
            page.Canvas.DrawString("™", smallFont, redBrush, logoX + 115, logoY);

            // Top Right Info Box
            float infoX = pageWidth - rightMargin - 180;
            float infoValX = pageWidth - rightMargin;

            string dateStr = data.InvoiceDate ?? DateTime.Now.ToString("MMM dd, yyyy");
            page.Canvas.DrawString("Date:", headerFont, blackBrush, infoX, y);
            page.Canvas.DrawString(dateStr, normalFont, blackBrush, infoValX - normalFont.MeasureString(dateStr).Width, y);
            y += 13;

            page.Canvas.DrawString("No. / Numéro:", headerFont, blackBrush, infoX, y);
            page.Canvas.DrawString(formattedInv, normalBoldFont, blackBrush, infoValX - normalBoldFont.MeasureString(formattedInv).Width, y);
            y += 13;

            page.Canvas.DrawString("Cust No. / Numéro de client:", headerFont, blackBrush, infoX, y);
            string custNo = data.CustNo ?? "N/A";
            page.Canvas.DrawString(custNo, normalFont, blackBrush, infoValX - normalFont.MeasureString(custNo).Width, y);

            y += 35;

            // ============== REMIT PAYMENT TO ==============
            page.Canvas.DrawString("Remit Payment To: / Payer à:", smallBoldFont, blackBrush, leftMargin, y);
            y += 12;
            page.Canvas.DrawString("Rogers Communications Canada Inc.", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("30 Victoria Crescent", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("Brampton, ON L6T 1E4", smallFont, blackBrush, leftMargin, y);

            y += 35;

            // ============== BILL TO / SHIP TO side-by-side ==============
            float billToX = leftMargin;
            float shipToX = pageWidth / 2 + 10;
            float sectionY = y;

            // Bill To Box
            page.Canvas.DrawString("Bill to: / Facture à:", normalBoldFont, blackBrush, billToX, sectionY);
            sectionY += 14;
            page.Canvas.DrawString(data.BillToName ?? "N/A", normalFont, blackBrush, billToX, sectionY);
            sectionY += 10;
            page.Canvas.DrawString(data.BillToAddress1 ?? "", normalFont, blackBrush, billToX, sectionY);
            if (!string.IsNullOrEmpty(data.BillToAddress2))
            {
                sectionY += 10;
                page.Canvas.DrawString(data.BillToAddress2, normalFont, blackBrush, billToX, sectionY);
            }
            sectionY += 10;
            page.Canvas.DrawString((data.BillToCity ?? "") + (string.IsNullOrEmpty(data.BillToCity) ? "" : " ") + (data.CustNo ?? ""), normalFont, blackBrush, billToX, sectionY);

            // Ship To Box
            float shipY = y;
            page.Canvas.DrawString("Ship To: / Expédier à:", normalBoldFont, blackBrush, shipToX, shipY);
            shipY += 14;
            page.Canvas.DrawString(data.ShipToName ?? data.BillToName ?? "N/A", normalFont, blackBrush, shipToX, shipY);
            shipY += 10;
            page.Canvas.DrawString(data.ShipToAddress1 ?? data.BillToAddress1 ?? "", normalFont, blackBrush, shipToX, shipY);
            if (!string.IsNullOrEmpty(data.ShipToAddress2))
            {
                shipY += 10;
                page.Canvas.DrawString(data.ShipToAddress2, normalFont, blackBrush, shipToX, shipY);
            }
            shipY += 10;
            page.Canvas.DrawString(data.ShipToCity ?? data.BillToCity ?? "", normalFont, blackBrush, shipToX, shipY);

            y = Math.Max(sectionY, shipY) + 30;

            // ============== ORDER SUMMARY BAR (4 Columns) ==============
            float barHeight = 35;
            float barWidth = pageWidth - leftMargin - rightMargin;
            page.Canvas.DrawRectangle(thinPen, leftMargin, y, barWidth, barHeight);

            float colWidth = barWidth / 4;
            page.Canvas.DrawLine(thinPen, leftMargin + colWidth, y, leftMargin + colWidth, y + barHeight);
            page.Canvas.DrawLine(thinPen, leftMargin + colWidth * 2, y, leftMargin + colWidth * 2, y + barHeight);
            page.Canvas.DrawLine(thinPen, leftMargin + colWidth * 3, y, leftMargin + colWidth * 3, y + barHeight);

            float textY = y + 5;
            // Col 1
            page.Canvas.DrawString("Ship Via / Expédier Via", smallBoldFont, blackBrush, leftMargin + 5, textY);
            page.Canvas.DrawString("Best way", normalFont, blackBrush, leftMargin + 5, textY + 12);
            // Col 2
            page.Canvas.DrawString("Salesperson / Représentant", smallBoldFont, blackBrush, leftMargin + colWidth + 5, textY);
            page.Canvas.DrawString("CCO", normalFont, blackBrush, leftMargin + colWidth + 5, textY + 12);
            // Col 3
            page.Canvas.DrawString("Terms / Termes", smallBoldFont, blackBrush, leftMargin + colWidth * 2 + 5, textY);
            page.Canvas.DrawString("V21 Account", normalFont, blackBrush, leftMargin + colWidth * 2 + 5, textY + 12);
            // Col 4
            page.Canvas.DrawString("Order No.", smallBoldFont, blackBrush, leftMargin + colWidth * 3 + 5, textY);
            page.Canvas.DrawString("No. Commande", smallBoldFont, blackBrush, leftMargin + colWidth * 3 + colWidth - smallBoldFont.MeasureString("No. Commande").Width - 5, textY);
            string orderNo = data.OrderNo ?? "0001367823"; // Sample fallback
            page.Canvas.DrawString(orderNo, normalFont, blackBrush, leftMargin + colWidth * 3 + colWidth - normalFont.MeasureString(orderNo).Width - 5, textY + 12);

            y += barHeight + 20;

            // ============== INVOICE DETAILS TABLE ==============
            float tableStart = y;
            float col1X = leftMargin;
            float col2X = leftMargin + 85;
            float col3X = pageWidth - rightMargin - 150;
            float col4X = pageWidth - rightMargin - 95;
            float col5X = pageWidth - rightMargin - 45;
            float tableEnd = pageWidth - rightMargin;

            // Table Header
            page.Canvas.DrawRectangle(thinPen, leftMargin, y, barWidth, 20);
            page.Canvas.DrawLine(thinPen, col2X, y, col2X, y + 20);
            page.Canvas.DrawLine(thinPen, col3X, y, col3X, y + 20);
            page.Canvas.DrawLine(thinPen, col4X, y, col4X, y + 20);
            page.Canvas.DrawLine(thinPen, col5X, y, col5X, y + 20);

            float headerY = y + 6;
            page.Canvas.DrawString("Item # / # Item", smallBoldFont, blackBrush, col1X + 5, headerY);
            page.Canvas.DrawString("Description", smallBoldFont, blackBrush, col2X + 5, headerY);
            page.Canvas.DrawString("Qty / Qté", smallBoldFont, blackBrush, col3X + 5, headerY);
            page.Canvas.DrawString("Unit $", smallBoldFont, blackBrush, col4X + 15, headerY - 3);
            page.Canvas.DrawString("$ Unité", smallBoldFont, blackBrush, col4X + 15, headerY + 5);
            page.Canvas.DrawString("Amount", smallBoldFont, blackBrush, col5X + 10, headerY - 3);
            page.Canvas.DrawString("Montant", smallBoldFont, blackBrush, col5X + 10, headerY + 5);

            y += 20;
            float contentStartY = y;

            foreach (var line in data.Lines)
            {
                if (y > page.Canvas.ClientSize.Height - 150)
                {
                    // Draw outer borders for current page
                    page.Canvas.DrawLine(thinPen, leftMargin, contentStartY, leftMargin, y);
                    page.Canvas.DrawLine(thinPen, col2X, contentStartY, col2X, y);
                    page.Canvas.DrawLine(thinPen, col3X, contentStartY, col3X, y);
                    page.Canvas.DrawLine(thinPen, col4X, contentStartY, col4X, y);
                    page.Canvas.DrawLine(thinPen, col5X, contentStartY, col5X, y);
                    page.Canvas.DrawLine(thinPen, tableEnd, contentStartY, tableEnd, y);
                    page.Canvas.DrawLine(thinPen, leftMargin, y, tableEnd, y);

                    page = pdf.Pages.Add();
                    y = 40;
                    contentStartY = y;
                    // Draw header again on new page if needed, but usually legacy reports just continue
                }

                float rowY = y + 5;
                page.Canvas.DrawString(line.PartNo ?? "", normalFont, blackBrush, col1X + 5, rowY);

                // Description with sub-details
                float descY = rowY;
                page.Canvas.DrawString(line.Description ?? "", normalFont, blackBrush, col2X + 5, descY);
                descY += 10;
                if (!string.IsNullOrEmpty(line.SerialNo))
                {
                    page.Canvas.DrawString("S/N: " + line.SerialNo, smallFont, blackBrush, col2X + 5, descY);
                    descY += 9;
                    page.Canvas.DrawString("Type: DATA", smallFont, blackBrush, col2X + 5, descY);
                    descY += 9;
                }

                string qtyStr = line.Qty.ToString("N0");
                if (line.Qty < 0) qtyStr = "-" + Math.Abs(line.Qty).ToString("N0");
                page.Canvas.DrawString(qtyStr, normalFont, blackBrush, col4X - 5 - normalFont.MeasureString(qtyStr).Width, rowY);

                string priceStr = line.Price == 0 ? "" : line.Price.ToString("N2");
                page.Canvas.DrawString(priceStr, normalFont, blackBrush, col5X - 5 - normalFont.MeasureString(priceStr).Width, rowY);

                string totalStr = line.LineTotal == 0 ? "N/C" : line.LineTotal.ToString("N2");
                page.Canvas.DrawString(totalStr, normalFont, blackBrush, tableEnd - 5 - normalFont.MeasureString(totalStr).Width, rowY);

                y = Math.Max(y + 15, descY + 5);
            }

            // Finish table borders
            page.Canvas.DrawLine(thinPen, leftMargin, contentStartY, leftMargin, y);
            page.Canvas.DrawLine(thinPen, col2X, contentStartY, col2X, y);
            page.Canvas.DrawLine(thinPen, col3X, contentStartY, col3X, y);
            page.Canvas.DrawLine(thinPen, col4X, contentStartY, col4X, y);
            page.Canvas.DrawLine(thinPen, col5X, contentStartY, col5X, y);
            page.Canvas.DrawLine(thinPen, tableEnd, contentStartY, tableEnd, y);
            page.Canvas.DrawLine(thinPen, leftMargin, y, tableEnd, y);

            y += 20;

            // ============== TOTALS SECTION ==============
            float totalsX = pageWidth - rightMargin - 200;
            float totalsValX = tableEnd - 5;

            decimal subtotal = 0;
            foreach (var l in data.Lines) subtotal += l.LineTotal;

            void DrawTotalLine(string label, decimal value, ref float curY, bool isBold = false)
            {
                PdfFont f = isBold ? normalBoldFont : normalFont;
                page.Canvas.DrawString(label, f, blackBrush, totalsX, curY);
                string valStr = value == 0 ? "0.00" : value.ToString("N2");
                page.Canvas.DrawString(valStr, f, blackBrush, totalsValX - f.MeasureString(valStr).Width, curY);
                curY += 14;
            }

            DrawTotalLine("Net Amount / Montant", subtotal, ref y);
            DrawTotalLine("Shipping", data.Shipping, ref y);
            DrawTotalLine("GST/HST", data.GST_HST, ref y);
            DrawTotalLine("PST/QST", data.PST_QST, ref y);
            DrawTotalLine("RV-UE Value / Valeur RV-UE", data.RV_Value, ref y);

            y += 5;
            page.Canvas.DrawLine(thickPen, totalsX, y, tableEnd, y);
            y += 5;

            decimal totalDue = subtotal + data.Shipping + data.GST_HST + data.PST_QST + data.RV_Value;
            page.Canvas.DrawString("Total Due", titleFont, blackBrush, totalsX, y);
            string totalStrFinal = totalDue.ToString("N2");
            page.Canvas.DrawString(totalStrFinal, titleFont, blackBrush, totalsValX - titleFont.MeasureString(totalStrFinal).Width, y);

            // ============== FOOTER ==============
            float footerY = page.Canvas.ClientSize.Height - 80;
            page.Canvas.DrawString("Please retain a copy of this invoice as proof of puchase/return. If amount due, please remit", smallFont, blackBrush, leftMargin, footerY);
            footerY += 9;
            page.Canvas.DrawString("payment as noted above.", smallFont, blackBrush, leftMargin, footerY);
            footerY += 12;
            page.Canvas.DrawString("S.V.P. conserver une copie de cette facture comme preuve d'achat et veuillez envoyer votre", smallFont, blackBrush, leftMargin, footerY);
            footerY += 9;
            page.Canvas.DrawString("paiement tel qu'indiqué.", smallFont, blackBrush, leftMargin, footerY);

            footerY += 15;
            page.Canvas.DrawString("HST/GST / TVH/TPS: 815781448", smallFont, blackBrush, leftMargin, footerY);
            footerY += 9;
            page.Canvas.DrawString("QST/TVQ: 1219760775", smallFont, blackBrush, leftMargin, footerY);
        }

        // --- UPDATED PDF GENERATION (PREVENT CODE DUPLICATION) ---
        private async Task<byte[]> CreateInvoicePdfBytes(string invoiceNo, string prefix, bool isSpire)
        {
            InvoiceDetail data = await GetInvoiceDataFromDb(invoiceNo, isSpire);
            if (data == null || data.Lines.Count == 0) return null;

            using (PdfDocument pdf = new PdfDocument())
            {
                GeneratePdfLayout(pdf, data, invoiceNo.PadLeft(10, '0'));

                using (MemoryStream pdfStream = new MemoryStream())
                {
                    pdf.SaveToStream(pdfStream);
                    return pdfStream.ToArray();
                }
            }
        }

        public async Task<ApiResposne> UploadAndMatchTemplate(Stream excelStream)
        {
            var response = new ApiResposne();
            HashSet<string> uniqueInvoices = new HashSet<string>();
            DataTable validInvoices = new DataTable();
            validInvoices.Columns.Add("InvoiceNo");

            DataTable invalid = new DataTable();
            invalid.Columns.Add("RowNumber");
            invalid.Columns.Add("Value");
            invalid.Columns.Add("Reason");

            int totalRowsProcessed = 0;

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var pkg = new ExcelPackage(excelStream))
                {
                    var ws = pkg.Workbook.Worksheets[0];
                    int rowCount = ws.Dimension?.End.Row ?? 0;
                    totalRowsProcessed = rowCount > 1 ? rowCount - 1 : 0;

                    for (int r = 2; r <= rowCount; r++)
                    {
                        string imei = ws.Cells[r, 1].Text.Trim();
                        string orderNo = ws.Cells[r, 2].Text.Trim();
                        string invoiceNo = ws.Cells[r, 3].Text.Trim();

                        if (string.IsNullOrEmpty(imei) && string.IsNullOrEmpty(orderNo) && string.IsNullOrEmpty(invoiceNo))
                            continue;

                        string foundInvoice = null;

                        // 1. Check Invoice Number directly in Spire
                        if (!string.IsNullOrEmpty(invoiceNo))
                            foundInvoice = await CheckSpireHistory(invoiceNo);

                        // 2. If not found, check Order Number in Spire
                        if (string.IsNullOrEmpty(foundInvoice) && !string.IsNullOrEmpty(orderNo))
                            foundInvoice = await GetInvoiceByOrderNo(orderNo);

                        // 3. If still not found, check IMEI in Spire
                        if (string.IsNullOrEmpty(foundInvoice) && !string.IsNullOrEmpty(imei))
                            foundInvoice = await GetInvoiceByIMEI(imei);

                        if (!string.IsNullOrEmpty(foundInvoice))
                        {
                            if (!uniqueInvoices.Contains(foundInvoice))
                            {
                                uniqueInvoices.Add(foundInvoice);
                                validInvoices.Rows.Add(foundInvoice);
                            }
                        }
                        else
                        {
                            // If Spire has no record, mark as Invalid
                            string details = $"IMEI:{imei} Ord:{orderNo} Inv:{invoiceNo}".Trim();
                            invalid.Rows.Add(r, details, "Not found in Spire database");
                        }
                    }
                }

                // Save valid ones to tblInvoiceList (SQL Server side for processing)
                if (validInvoices.Rows.Count > 0)
                {
                    using (var bulk = new SqlBulkCopy(_sqlConn))
                    {
                        bulk.DestinationTableName = "tblInvoiceList";
                        bulk.ColumnMappings.Add("InvoiceNo", "InvoiceNo");
                        await bulk.WriteToServerAsync(validInvoices);
                    }
                }

                response.Success = true;
                response.Message = $"Processed {totalRowsProcessed} rows.";
                response.Result = new
                {
                    InsertedCount = validInvoices.Rows.Count,
                    FailedCount = invalid.Rows.Count,
                    InvalidRows = DataTableToList(invalid)
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }
        private List<Dictionary<string, object>> DataTableToList(DataTable dt)
        {
            return dt.AsEnumerable().Select(row =>
                dt.Columns.Cast<DataColumn>()
                 .ToDictionary(col => col.ColumnName, col => row[col])
            ).ToList();
        }

        // Helper: Match using Order Number
        private async Task<string> GetInvoiceByOrderNo(string orderNo)
        {
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                string sql = "SELECT invoice_no FROM sales_history WHERE order_no = @ord LIMIT 1";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ord", orderNo);
                    var res = await cmd.ExecuteScalarAsync();
                    return res?.ToString();
                }
            }
        }

        private async Task<string> GetInvoiceByIMEI(string imei)
        {
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                string sql = @"
            SELECT invoice_no FROM (
                SELECT ist.link_no as invoice_no, 
                       ROW_NUMBER() OVER (ORDER BY shi.invoice_date DESC) as rn
                FROM inventory_serial_transactions ist
                JOIN sales_history_items shi ON ist.link_no = shi.invoice_no AND ist.part_no = shi.part_no
                WHERE ist.number = @imei AND ist.sales_qty > 0
            ) t WHERE rn = 1";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@imei", imei);
                    var res = await cmd.ExecuteScalarAsync();
                    return res?.ToString();
                }
            }
        }

        public async Task<byte[]> GenerateInvoicesZip(string prefix)
        {
            var invoices = await GetInvoiceList();

            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var item in invoices)
                    {
                        string spireCheck = await CheckSpireHistory(item.InvoiceNo);
                        bool isSpire = !string.IsNullOrEmpty(spireCheck);

                        byte[] pdfBytes = await CreateInvoicePdfBytes(item.InvoiceNo, prefix, isSpire);

                        if (pdfBytes != null)
                        {
                            string formattedInv = item.InvoiceNo.PadLeft(10, '0');
                            string entryName = string.IsNullOrEmpty(prefix)
                                ? $"{formattedInv}.pdf"
                                : $"{prefix}-{formattedInv}.pdf";

                            var zipEntry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);

                            using (var entryStream = zipEntry.Open())
                            {
                                await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                            }
                        }
                    }
                }
                return ms.ToArray();
            }
        }




    }
}
