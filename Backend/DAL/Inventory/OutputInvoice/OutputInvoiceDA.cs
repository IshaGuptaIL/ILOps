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
                    pdf.PageSettings.Size = PdfPageSize.A4;
                    pdf.PageSettings.Margins.All = 15;
                    PdfPageBase page = pdf.Pages.Add();

                    PdfBrush blackBrush = PdfBrushes.Black;
                    PdfBrush grayBrush = new PdfSolidBrush(new PdfRGBColor(240, 240, 240));
                    PdfPen thinPen = new PdfPen(PdfBrushes.Black, 0.5f);

                    PdfFont titleFont = new PdfFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                    PdfFont headerFont = new PdfFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
                    PdfFont normalFont = new PdfFont(PdfFontFamily.Helvetica, 8);
                    PdfFont smallFont = new PdfFont(PdfFontFamily.Helvetica, 7);
                    PdfFont smallBoldFont = new PdfFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);

                    float pageWidth = page.Canvas.ClientSize.Width;
                    float y = 25;
                    float marginLeft = 15;
                    float marginRight = 15;

                    // ============== HEADER SECTION ==============
                    page.Canvas.DrawString("Your Rogers Bill", titleFont, blackBrush, marginLeft, y);
                    page.Canvas.DrawString("No. / Numéro:", headerFont, blackBrush, 350, y);
                    page.Canvas.DrawString(formattedInv, headerFont, blackBrush, 480, y);
                    y += 15;

                    page.Canvas.DrawString("Cust No. / Numéro de client:", headerFont, blackBrush, 350, y);
                    page.Canvas.DrawString(data.CustNo ?? "N/A", headerFont, blackBrush, 480, y);
                    y += 15;

                    page.Canvas.DrawString("Date:", headerFont, blackBrush, 350, y);
                    page.Canvas.DrawString(data.InvoiceDate ?? DateTime.Now.ToString("MMM dd, yyyy"), normalFont, blackBrush, 480, y);
                    y += 25;

                    // ============== REMIT PAYMENT SECTION ==============
                    page.Canvas.DrawString("Remit Payment To: / Payer à:", headerFont, blackBrush, marginLeft, y);
                    y += 15;
                    page.Canvas.DrawString("Rogers Communications Canada Inc.", normalFont, blackBrush, marginLeft, y);
                    y += 12;
                    page.Canvas.DrawString("30 Victoria Crescent", normalFont, blackBrush, marginLeft, y);
                    y += 12;
                    page.Canvas.DrawString("Brampton, ON L6T 1E4", normalFont, blackBrush, marginLeft, y);
                    y += 25;

                    // ============== BILL TO / SHIP TO SECTION ==============
                    float billToX = marginLeft;
                    float shipToX = marginLeft + 250;
                    float addressY = y;

                    page.Canvas.DrawString("Bill to: / Facture à:", headerFont, blackBrush, billToX, addressY);
                    page.Canvas.DrawString("Ship To: / Expédier à:", headerFont, blackBrush, shipToX, addressY);
                    addressY += 15;

                    page.Canvas.DrawString(data.BillToName ?? "N/A", normalFont, blackBrush, billToX, addressY);
                    page.Canvas.DrawString(data.ShipToName ?? data.BillToName ?? "N/A", normalFont, blackBrush, shipToX, addressY);
                    addressY += 12;

                    page.Canvas.DrawString(data.BillToAddress1 ?? "", normalFont, blackBrush, billToX, addressY);
                    page.Canvas.DrawString(data.ShipToAddress1 ?? "", normalFont, blackBrush, shipToX, addressY);
                    addressY += 12;

                    if (!string.IsNullOrEmpty(data.BillToAddress2))
                    {
                        page.Canvas.DrawString(data.BillToAddress2, normalFont, blackBrush, billToX, addressY);
                        addressY += 12;
                    }

                    page.Canvas.DrawString(data.BillToCity ?? "", normalFont, blackBrush, billToX, addressY);
                    page.Canvas.DrawString(data.ShipToCity ?? "", normalFont, blackBrush, shipToX, addressY);

                    y = addressY + 25;

                    // ============== ORDER INFO BAR (FIXED ALIGNMENT) ==============
                    float infoBarTop = y;
                    float infoBarHeight = 28f;
                    float infoBarWidth = pageWidth - marginLeft - marginRight;

                    var infoRect = new RectangleF(marginLeft, infoBarTop, infoBarWidth, infoBarHeight);
                    page.Canvas.DrawRectangle(grayBrush, infoRect);
                    page.Canvas.DrawRectangle(thinPen, infoRect);

                    // Two fixed Y positions for labels and values
                    float labelsY = infoBarTop + 5;
                    float valuesY = infoBarTop + 16;

                    // Fixed X positions for each column
                    float shipViaX = marginLeft + 5;
                    float salesX = marginLeft + 135;
                    float termsX = marginLeft + 280;
                    float orderNoX = marginLeft + 400;

                    // First row - labels
                    page.Canvas.DrawString("Ship Via / Expédier Via", smallBoldFont, blackBrush, shipViaX, labelsY);
                    page.Canvas.DrawString("Salesperson / Représentant", smallBoldFont, blackBrush, salesX, labelsY);
                    page.Canvas.DrawString("Terms / Termes", smallBoldFont, blackBrush, termsX, labelsY);
                    page.Canvas.DrawString("Order No. / No. Commande", smallBoldFont, blackBrush, orderNoX, labelsY);

                    // Second row - values
                    page.Canvas.DrawString("Best way", smallFont, blackBrush, shipViaX, valuesY);
                    page.Canvas.DrawString("CCO", smallFont, blackBrush, salesX, valuesY);
                    page.Canvas.DrawString("V21 Account", smallFont, blackBrush, termsX, valuesY);
                    page.Canvas.DrawString(data.OrderNo ?? "0001367823", smallFont, blackBrush, orderNoX, valuesY);

                    y = infoBarTop + infoBarHeight + 15;

                    // ============== TABLE HEADER (FIXED ALIGNMENT) ==============
                    float tableTop = y;
                    float tableHeaderHeight = 20f;

                    var tableHeaderRect = new RectangleF(marginLeft, tableTop, infoBarWidth, tableHeaderHeight);
                    page.Canvas.DrawRectangle(grayBrush, tableHeaderRect);
                    page.Canvas.DrawRectangle(thinPen, tableHeaderRect);

                    float headerTextY = tableTop + 6;

                    // Fixed column positions
                    float colItemX = marginLeft + 5;
                    float colDescX = marginLeft + 85;
                    float colQtyX = pageWidth - marginRight - 155;
                    float colUnitX = pageWidth - marginRight - 105;
                    float colAmountX = pageWidth - marginRight - 55;

                    page.Canvas.DrawString("Item # / # Item", headerFont, blackBrush, colItemX, headerTextY);
                    page.Canvas.DrawString("Description", headerFont, blackBrush, colDescX, headerTextY);
                    page.Canvas.DrawString("Qty / Qté", headerFont, blackBrush, colQtyX, headerTextY);
                    page.Canvas.DrawString("Unit $", headerFont, blackBrush, colUnitX, headerTextY);
                    page.Canvas.DrawString("Amount", headerFont, blackBrush, colAmountX, headerTextY);

                    // Small labels below
                    page.Canvas.DrawString("$ Unité", smallFont, blackBrush, colUnitX, headerTextY + 9);
                    page.Canvas.DrawString("Montant", smallFont, blackBrush, colAmountX, headerTextY + 9);

                    y = tableTop + tableHeaderHeight + 10;

                    // ============== LINE ITEMS ==============
                    decimal subtotal = 0;
                    foreach (var line in data.Lines)
                    {
                        decimal lineTotal = line.Qty * line.Price;
                        subtotal += lineTotal;

                        // Check for page break
                        if (y > 750)
                        {
                            page = pdf.Pages.Add();
                            y = 25;

                            // Redraw table header on new page
                            tableTop = y;
                            tableHeaderRect = new RectangleF(marginLeft, tableTop, infoBarWidth, tableHeaderHeight);
                            page.Canvas.DrawRectangle(grayBrush, tableHeaderRect);
                            page.Canvas.DrawRectangle(thinPen, tableHeaderRect);

                            headerTextY = tableTop + 6;
                            page.Canvas.DrawString("Item # / # Item", headerFont, blackBrush, colItemX, headerTextY);
                            page.Canvas.DrawString("Description", headerFont, blackBrush, colDescX, headerTextY);
                            page.Canvas.DrawString("Qty / Qté", headerFont, blackBrush, colQtyX, headerTextY);
                            page.Canvas.DrawString("Unit $", headerFont, blackBrush, colUnitX, headerTextY);
                            page.Canvas.DrawString("Amount", headerFont, blackBrush, colAmountX, headerTextY);
                            page.Canvas.DrawString("$ Unité", smallFont, blackBrush, colUnitX, headerTextY + 9);
                            page.Canvas.DrawString("Montant", smallFont, blackBrush, colAmountX, headerTextY + 9);

                            y = tableTop + tableHeaderHeight + 10;
                        }

                        // Item number
                        page.Canvas.DrawString(line.PartNo ?? "", normalFont, blackBrush, colItemX, y);

                        // Description (truncate if too long)
                        string desc = line.Description ?? "";
                        if (desc.Length > 45)
                            desc = desc.Substring(0, 45) + "...";
                        page.Canvas.DrawString(desc, normalFont, blackBrush, colDescX, y);

                        // Quantity (right-aligned)
                        string qtyStr = line.Qty == 0 ? "0" : line.Qty.ToString("0");
                        SizeF qtySize = normalFont.MeasureString(qtyStr);
                        page.Canvas.DrawString(qtyStr, normalFont, blackBrush, colQtyX + 40 - qtySize.Width, y);

                        // Unit Price (right-aligned)
                        string priceStr = line.Price == 0 ? "N/C" : line.Price.ToString("0.00");
                        SizeF priceSize = normalFont.MeasureString(priceStr);
                        page.Canvas.DrawString(priceStr, normalFont, blackBrush, colUnitX + 35 - priceSize.Width, y);

                        // Amount (right-aligned)
                        string amountStr = lineTotal == 0 ? "N/C" : lineTotal.ToString("0.00");
                        SizeF amountSize = normalFont.MeasureString(amountStr);
                        page.Canvas.DrawString(amountStr, normalFont, blackBrush, colAmountX + 45 - amountSize.Width, y);

                        y += 14;
                    }

                    y += 20;

                    // ============== TOTALS SECTION ==============
                    float totalsLabelX = pageWidth - marginRight - 210;
                    float totalsValueX = pageWidth - marginRight - 55;

                    // Net Amount
                    page.Canvas.DrawString("Net Amount / Montant", normalFont, blackBrush, totalsLabelX, y);
                    string netStr = subtotal.ToString("0.00");
                    SizeF netSize = normalFont.MeasureString(netStr);
                    page.Canvas.DrawString(netStr, normalFont, blackBrush, totalsValueX + 45 - netSize.Width, y);
                    y += 15;

                    // Shipping
                    page.Canvas.DrawString("Shipping", normalFont, blackBrush, totalsLabelX, y);
                    y += 15;

                    // GST/HST
                    decimal tax = subtotal * 0.13m;
                    page.Canvas.DrawString("GST/HST", normalFont, blackBrush, totalsLabelX, y);
                    string taxStr = tax.ToString("0.00");
                    SizeF taxSize = normalFont.MeasureString(taxStr);
                    page.Canvas.DrawString(taxStr, normalFont, blackBrush, totalsValueX + 45 - taxSize.Width, y);
                    y += 15;

                    // PST/QST
                    page.Canvas.DrawString("PST/QST", normalFont, blackBrush, totalsLabelX, y);
                    y += 15;

                    // RV-UE Value
                    page.Canvas.DrawString("RV-UE Value / Valeur RV-UE", normalFont, blackBrush, totalsLabelX, y);
                    y += 20;

                    // Total Due (bold)
                    page.Canvas.DrawString("Total Due", titleFont, blackBrush, totalsLabelX, y);
                    string totalStr = (subtotal + tax).ToString("0.00");
                    SizeF totalSize = titleFont.MeasureString(totalStr);
                    page.Canvas.DrawString(totalStr, titleFont, blackBrush, totalsValueX + 45 - totalSize.Width, y);

                    y += 35;

                    // ============== FOOTER SECTION ==============
                    page.Canvas.DrawString("For payment inquiries please call", smallFont, blackBrush, marginLeft, y);
                    y += 10;
                    page.Canvas.DrawString(" 905-595-4935 (GTA)/1-866-595-1075", smallFont, blackBrush, marginLeft + 5, y);
                    y += 15;

                    page.Canvas.DrawString("Please retain a copy of this invoice as proof of purchase/return. If amount due, please remit", smallFont, blackBrush, marginLeft, y);
                    y += 10;
                    page.Canvas.DrawString("payment as noted above.", smallFont, blackBrush, marginLeft, y);
                    y += 10;
                    page.Canvas.DrawString("S.V.P. conserver une copie de cette facture comme preuve d'achat et veuillez envoyer votre", smallFont, blackBrush, marginLeft, y);
                    y += 10;
                    page.Canvas.DrawString("paiement tel qu'indiqué.", smallFont, blackBrush, marginLeft, y);

                    y += 20;
                    page.Canvas.DrawString("HST/GST / TVH/TPS: 815781448", smallFont, blackBrush, totalsLabelX, y);
                    y += 10;
                    page.Canvas.DrawString("QST/TVQ: 1219760775", smallFont, blackBrush, totalsLabelX, y);

                    pdf.SaveToFile(fullPath);
                }
                return File.Exists(fullPath);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("invoice_error.txt", ex.ToString());
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

                                        var dateVal = r["invoice_date"];
                                        if (dateVal != DBNull.Value)
                                        {
                                            detail.InvoiceDate = dateVal.ToString();
                                        }
                                        else
                                        {
                                            detail.InvoiceDate = "N/A";
                                        }
                                    }
                                }
                            }

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
            pdf.PageSettings.Margins.All = 0; // Remove default margins for precise control
            PdfPageBase page = pdf.Pages.Add();

            // Define brushes and pens
            PdfBrush blackBrush = PdfBrushes.Black;
            PdfBrush grayBrush = new PdfSolidBrush(new PdfRGBColor(245, 245, 245));
            PdfPen thinPen = new PdfPen(PdfBrushes.Black, 0.5f);
            PdfPen thickPen = new PdfPen(PdfBrushes.Black, 1.0f);

            // Define fonts
            PdfFont titleFont = new PdfFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
            PdfFont headerFont = new PdfFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
            PdfFont normalFont = new PdfFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
            PdfFont smallFont = new PdfFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Regular);
            PdfFont smallBoldFont = new PdfFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);

            float pageWidth = page.Canvas.ClientSize.Width;
            float pageHeight = page.Canvas.ClientSize.Height;
            float leftMargin = 40;
            float rightMargin = 40;
            float topMargin = 40;
            float y = topMargin;

            // ============== HEADER SECTION ==============
            // Title and Logo area
            page.Canvas.DrawString("Your Rogers Bill", titleFont, blackBrush, leftMargin, y);

            // Rogers logo placeholder (right aligned) - you can add actual logo image here
            // page.Canvas.DrawImage(PdfImage.FromFile("rogers-logo.png"), pageWidth - rightMargin - 100, y, 100, 30);

            y += 35;

            // Top right information box
            float infoBoxX = pageWidth - rightMargin - 200;
            page.Canvas.DrawString("Date:", headerFont, blackBrush, infoBoxX, y);
            page.Canvas.DrawString(data.InvoiceDate ?? DateTime.Now.ToString("MMM dd, yyyy"), normalFont, blackBrush, infoBoxX + 130, y);
            y += 15;

            page.Canvas.DrawString("No. / Numéro:", headerFont, blackBrush, infoBoxX, y);
            page.Canvas.DrawString(formattedInv, normalFont, blackBrush, infoBoxX + 130, y);
            y += 15;

            page.Canvas.DrawString("Cust No. / Numéro de client:", headerFont, blackBrush, infoBoxX, y);
            page.Canvas.DrawString(data.CustNo ?? "N/A", normalFont, blackBrush, infoBoxX + 130, y);

            y += 30;

            // ============== REMIT PAYMENT SECTION ==============
            page.Canvas.DrawString("Remit Payment To: / Payer à:", headerFont, blackBrush, leftMargin, y);
            y += 15;
            page.Canvas.DrawString("Rogers Communications Canada Inc.", normalFont, blackBrush, leftMargin, y);
            y += 12;
            page.Canvas.DrawString("30 Victoria Crescent", normalFont, blackBrush, leftMargin, y);
            y += 12;
            page.Canvas.DrawString("Brampton, ON L6T 1E4", normalFont, blackBrush, leftMargin, y);

            y += 30;

            // ============== BILL TO / SHIP TO SECTION ==============
            float billToX = leftMargin;
            float shipToX = leftMargin + 250;
            float sectionY = y;

            // Bill To
            page.Canvas.DrawString("Bill to: / Facture à:", headerFont, blackBrush, billToX, sectionY);
            sectionY += 15;
            page.Canvas.DrawString(data.BillToName ?? "4Refuel Canada LP", normalFont, blackBrush, billToX, sectionY);
            sectionY += 12;
            page.Canvas.DrawString(data.BillToAddress1 ?? "14928 56 AVE", normalFont, blackBrush, billToX, sectionY);
            sectionY += 12;
            page.Canvas.DrawString(data.BillToAddress2 ?? "103", normalFont, blackBrush, billToX, sectionY);
            sectionY += 12;
            page.Canvas.DrawString(data.BillToCity ?? "SURREY BC V3S2N5", normalFont, blackBrush, billToX, sectionY);

            // Ship To
            sectionY = y;
            page.Canvas.DrawString("Ship To: / Expédier à:", headerFont, blackBrush, shipToX, sectionY);
            sectionY += 15;
            page.Canvas.DrawString(data.ShipToName ?? "4REFUEL CANADA LP", normalFont, blackBrush, shipToX, sectionY);
            sectionY += 12;
            page.Canvas.DrawString(data.ShipToAddress1 ?? "2676 MAC ST", normalFont, blackBrush, shipToX, sectionY);
            sectionY += 12;
            page.Canvas.DrawString(data.ShipToCity ?? "OTTAWA ON K1V 8V1", normalFont, blackBrush, shipToX, sectionY);

            y = sectionY + 30;

            // ============== ORDER INFO BAR ==============
            float infoBarY = y;
            page.Canvas.DrawRectangle(grayBrush, new RectangleF(leftMargin, infoBarY, pageWidth - leftMargin - rightMargin, 20));
            page.Canvas.DrawRectangle(thinPen, new RectangleF(leftMargin, infoBarY, pageWidth - leftMargin - rightMargin, 20));

            float infoY = infoBarY + 6;
            page.Canvas.DrawString("Ship Via / Expédier Via", smallBoldFont, blackBrush, leftMargin + 5, infoY);
            page.Canvas.DrawString("Salesperson / Représentant", smallBoldFont, blackBrush, leftMargin + 120, infoY);
            page.Canvas.DrawString("Terms / Termes", smallBoldFont, blackBrush, leftMargin + 260, infoY);
            page.Canvas.DrawString("Order No. / No. Commande", smallBoldFont, blackBrush, leftMargin + 360, infoY);

            infoY += 10;
            page.Canvas.DrawString("Best way", smallFont, blackBrush, leftMargin + 5, infoY);
            page.Canvas.DrawString("CCO", smallFont, blackBrush, leftMargin + 120, infoY);
            page.Canvas.DrawString("V21 Account", smallFont, blackBrush, leftMargin + 260, infoY);
            page.Canvas.DrawString(data.OrderNo ?? "0001367823", smallFont, blackBrush, leftMargin + 360, infoY);

            y = infoBarY + 35;

            // ============== TABLE SECTION ==============
            // Table header background
            float tableHeaderY = y;
            page.Canvas.DrawRectangle(grayBrush, new RectangleF(leftMargin, tableHeaderY, pageWidth - leftMargin - rightMargin, 18));
            page.Canvas.DrawLine(thickPen, leftMargin, tableHeaderY + 18, pageWidth - rightMargin, tableHeaderY + 18);

            // Table column headers
            float colItemX = leftMargin + 5;
            float colDescX = leftMargin + 100;
            float colQtyX = pageWidth - rightMargin - 160;
            float colUnitX = pageWidth - rightMargin - 110;
            float colAmountX = pageWidth - rightMargin - 60;

            float headerY = tableHeaderY + 5;
            page.Canvas.DrawString("Item # / # Item", headerFont, blackBrush, colItemX, headerY);
            page.Canvas.DrawString("Description", headerFont, blackBrush, colDescX, headerY);
            page.Canvas.DrawString("Qty / Qté", headerFont, blackBrush, colQtyX, headerY);
            page.Canvas.DrawString("Unit $", headerFont, blackBrush, colUnitX, headerY);
            page.Canvas.DrawString("Amount", headerFont, blackBrush, colAmountX, headerY);
            page.Canvas.DrawString("$ Unité", smallFont, blackBrush, colUnitX, headerY + 8);
            page.Canvas.DrawString("Montant", smallFont, blackBrush, colAmountX, headerY + 8);

            y = tableHeaderY + 25;

            // Line Items
            decimal subtotal = 0;
            foreach (var line in data.Lines)
            {
                decimal lineTotal = line.Qty * line.Price;
                subtotal += lineTotal;

                // Check for page break
                if (y > pageHeight - 150)
                {
                    page = pdf.Pages.Add();
                    y = topMargin;
                    // Redraw table headers on new page
                    page.Canvas.DrawRectangle(grayBrush, new RectangleF(leftMargin, y, pageWidth - leftMargin - rightMargin, 18));
                    page.Canvas.DrawString("Item # / # Item", headerFont, blackBrush, colItemX, y + 5);
                    page.Canvas.DrawString("Description", headerFont, blackBrush, colDescX, y + 5);
                    page.Canvas.DrawString("Qty / Qté", headerFont, blackBrush, colQtyX, y + 5);
                    page.Canvas.DrawString("Unit $", headerFont, blackBrush, colUnitX, y + 5);
                    page.Canvas.DrawString("Amount", headerFont, blackBrush, colAmountX, y + 5);
                    y += 25;
                }

                // Item number
                page.Canvas.DrawString(line.PartNo ?? "", normalFont, blackBrush, colItemX, y);

                // Description (handle multi-line if needed)
                string desc = line.Description ?? "";
                if (desc.Length > 50)
                {
                    page.Canvas.DrawString(desc.Substring(0, 50), normalFont, blackBrush, colDescX, y);
                    y += 10;
                    if (desc.Length > 100)
                        page.Canvas.DrawString(desc.Substring(50, Math.Min(50, desc.Length - 50)) + "...", normalFont, blackBrush, colDescX, y);
                    else
                        page.Canvas.DrawString(desc.Substring(50), normalFont, blackBrush, colDescX, y);
                }
                else
                {
                    page.Canvas.DrawString(desc, normalFont, blackBrush, colDescX, y);
                }

                // Quantity (right-aligned)
                string qtyStr = line.Qty.ToString("0");
                SizeF qtySize = normalFont.MeasureString(qtyStr);
                page.Canvas.DrawString(qtyStr, normalFont, blackBrush, colQtyX + 35 - qtySize.Width, y);

                // Unit Price (right-aligned)
                string priceStr = line.Price == 0 ? "N/C" : line.Price.ToString("0.00");
                SizeF priceSize = normalFont.MeasureString(priceStr);
                page.Canvas.DrawString(priceStr, normalFont, blackBrush, colUnitX + 35 - priceSize.Width, y);

                // Amount (right-aligned)
                string amountStr = lineTotal == 0 ? "N/C" : lineTotal.ToString("0.00");
                SizeF amountSize = normalFont.MeasureString(amountStr);
                page.Canvas.DrawString(amountStr, normalFont, blackBrush, colAmountX + 50 - amountSize.Width, y);

                y += 18;

                // Draw separator line
                page.Canvas.DrawLine(thinPen, leftMargin, y, pageWidth - rightMargin, y);
                y += 3;
            }

            y += 15;

            // ============== TOTALS SECTION ==============
            float totalsLabelX = pageWidth - rightMargin - 200;
            float totalsValueX = pageWidth - rightMargin - 60;

            // Net Amount
            page.Canvas.DrawString("Net Amount / Montant", normalFont, blackBrush, totalsLabelX, y);
            string netStr = subtotal.ToString("0.00");
            SizeF netSize = normalFont.MeasureString(netStr);
            page.Canvas.DrawString(netStr, normalFont, blackBrush, totalsValueX + 50 - netSize.Width, y);
            y += 15;

            // Shipping
            page.Canvas.DrawString("Shipping", normalFont, blackBrush, totalsLabelX, y);
            y += 15;

            // GST/HST
            decimal tax = subtotal * 0.13m;
            page.Canvas.DrawString("GST/HST", normalFont, blackBrush, totalsLabelX, y);
            string taxStr = tax.ToString("0.00");
            SizeF taxSize = normalFont.MeasureString(taxStr);
            page.Canvas.DrawString(taxStr, normalFont, blackBrush, totalsValueX + 50 - taxSize.Width, y);
            y += 15;

            // PST/QST
            page.Canvas.DrawString("PST/QST", normalFont, blackBrush, totalsLabelX, y);
            y += 15;

            // RV-UE Value
            page.Canvas.DrawString("RV-UE Value / Valeur RV-UE", normalFont, blackBrush, totalsLabelX, y);
            y += 20;

            // Total Due (highlighted)
            page.Canvas.DrawRectangle(grayBrush, new RectangleF(totalsLabelX - 5, y - 2, 200, 18));
            page.Canvas.DrawString("Total Due", headerFont, blackBrush, totalsLabelX, y + 2);
            string totalStr = (subtotal + tax).ToString("0.00");
            SizeF totalSize = headerFont.MeasureString(totalStr);
            page.Canvas.DrawString(totalStr, headerFont, blackBrush, totalsValueX + 50 - totalSize.Width, y + 2);

            y += 35;

            // ============== FOOTER SECTION ==============
            page.Canvas.DrawString("For payment inquiries please call", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString(" 905-595-4935 (GTA)/1-866-595-1075", smallFont, blackBrush, leftMargin + 5, y);
            y += 15;

            page.Canvas.DrawString("Please retain a copy of this invoice as proof of purchase/return. If amount due, please remit", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("payment as noted above.", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("S.V.P. conserver une copie de cette facture comme preuve d'achat et veuillez envoyer votre", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("paiement tel qu'indiqué.", smallFont, blackBrush, leftMargin, y);

            y += 20;
            page.Canvas.DrawString("HST/GST / TVH/TPS: 815781448", smallFont, blackBrush, totalsLabelX, y);
            y += 10;
            page.Canvas.DrawString("QST/TVQ: 1219760775", smallFont, blackBrush, totalsLabelX, y);
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