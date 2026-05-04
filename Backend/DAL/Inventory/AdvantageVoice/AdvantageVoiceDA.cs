using DAL.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using System.IO;

namespace DAL.Inventory.AdvantageVoice
{
    public class AdvantageVoiceDA : IAdvantageVoice
    {
        private readonly AppDBContext _context;
        private readonly string _pgConn;
        private readonly string _sqlConn;

        public AdvantageVoiceDA(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _pgConn = config.GetConnectionString("spire_Connection");
            _sqlConn = config.GetConnectionString("bvactivation_Connection");
        }

        public async Task<List<AdvantageImportVM>> GetPendingImportsAsync(int userId)
        {
            return await _context.AdvantageVoiceImports
                .Where(x => !x.Imported && x.UserId == userId)
                .OrderBy(x => x.ID)
                .Select(x => new AdvantageImportVM
                {
                    ID = x.ID,
                    CompanyName = x.CompanyName,
                    ShippingContact = x.ShippingContact,
                    ContactNumber = x.ContactNumber,
                    OrderDate = x.OrderDate,
                    OrderType = x.OrderType,
                    SpireOrder = x.SpireOrder,
                    GOrderNumber = x.GOrderNumber,
                    TemporaryNumber = x.TemporaryNumber,
                    MacAddress = x.MacAddress,
                    UserName = x.UserName,
                    BvPartNo = x.BvPartNo,
                    ShippingAddress = x.ShippingAddress,
                    Address = x.Address,
                    City = x.City,
                    Province = x.Province,
                    PostalCode = x.PostalCode,
                    V21Ban = x.V21Ban,
                    ContactEmail = x.ContactEmail,
                    RogersSpecialistEmail = x.RogersSpecialistEmail,
                    HardwareType = x.HardwareType,
                    PurolatorNumber = x.PurolatorNumber,
                    ReturnPurolatorNumber = x.ReturnPurolatorNumber,
                    DciInvoice = x.DciInvoice,
                    Status = x.Status,
                    CompletedDate = x.CompletedDate,
                    Note = x.Note,
                    Validated = x.Validated,
                    Reason = x.Reason,
                    Imported = x.Imported,
                    UserId = x.UserId
                })
                .ToListAsync();
        }

        public async Task<bool> ImportExcelDataAsync(Stream fileStream, int userId)
        {
            var existing = await _context.AdvantageVoiceImports.Where(x => !x.Imported && x.UserId == userId).ToListAsync();
            _context.AdvantageVoiceImports.RemoveRange(existing);
            await _context.SaveChangesAsync();

            var entities = new List<AdvantageVoiceImport>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var orderDateVal = worksheet.Cells[row, 1].Value;
                    if (orderDateVal == null) continue;

                    DateTime orderDate;
                    if (orderDateVal is DateTime dt) orderDate = dt;
                    else if (DateTime.TryParse(orderDateVal.ToString(), out var parsedDate)) orderDate = parsedDate;
                    else continue;

                    var completedDateVal = worksheet.Cells[row, 23].Value;
                    DateTime? completedDate = null;
                    if (completedDateVal is DateTime dtComp) completedDate = dtComp;
                    else if (DateTime.TryParse(completedDateVal?.ToString(), out var parsedComp)) completedDate = parsedComp;

                    var v21Ban = CleanCustomerName(worksheet.Cells[row, 2].Value?.ToString());
                    var orderType = CleanCustomerName(worksheet.Cells[row, 3].Value?.ToString());
                    var companyName = CleanCustomerName(worksheet.Cells[row, 4].Value?.ToString());
                    var shippingContact = CleanCustomerName(worksheet.Cells[row, 5].Value?.ToString());
                    var contactNumber = CleanCustomerName(worksheet.Cells[row, 6].Value?.ToString());
                    var contactEmail = CleanCustomerName(worksheet.Cells[row, 7].Value?.ToString());
                    var rogersEmail = CleanCustomerName(worksheet.Cells[row, 8].Value?.ToString());
                    var gOrderNumber = CleanCustomerName(worksheet.Cells[row, 9].Value?.ToString());
                    var userName = CleanCustomerName(worksheet.Cells[row, 10].Value?.ToString());
                    var tempNumber = CleanCustomerName(worksheet.Cells[row, 11].Value?.ToString());
                    var hardwareType = CleanCustomerName(worksheet.Cells[row, 12].Value?.ToString());
                    var bvPartNo = CleanCustomerName(worksheet.Cells[row, 13].Value?.ToString());
                    var address = CleanCustomerName(worksheet.Cells[row, 14].Value?.ToString());
                    var city = CleanCustomerName(worksheet.Cells[row, 15].Value?.ToString());
                    var province = CleanCustomerName(worksheet.Cells[row, 16].Value?.ToString());
                    var postalCode = CleanCustomerName(worksheet.Cells[row, 17].Value?.ToString());
                    var macAddress = CleanCustomerName(worksheet.Cells[row, 18].Value?.ToString());
                    var purolator = CleanCustomerName(worksheet.Cells[row, 19].Value?.ToString());
                    var returnPurolator = CleanCustomerName(worksheet.Cells[row, 20].Value?.ToString());
                    var dciInvoice = CleanCustomerName(worksheet.Cells[row, 21].Value?.ToString());
                    var status = CleanCustomerName(worksheet.Cells[row, 22].Value?.ToString());
                    var note = CleanCustomerName(worksheet.Cells[row, 24].Value?.ToString());

                    var nextTempNo = await GetNextTempOrderNo();

                    string finalGOrderNo = "";
                    if (string.IsNullOrWhiteSpace(gOrderNumber))
                    {
                        finalGOrderNo = $"ADV-V{v21Ban}-{nextTempNo}";
                    }
                    else
                    {
                        var cleanG = gOrderNumber.Replace(" ", "");
                        finalGOrderNo = $"ADV-{cleanG}-{nextTempNo}";
                    }

                    entities.Add(new AdvantageVoiceImport
                    {
                        CompanyName = companyName,
                        ShippingContact = shippingContact,
                        ContactNumber = formatPhone(contactNumber),
                        OrderDate = orderDate,
                        OrderType = orderType,
                        SpireOrder = $"ADV-{nextTempNo:D6}",
                        GOrderNumber = finalGOrderNo,
                        TemporaryNumber = formatPhone(tempNumber),
                        MacAddress = macAddress?.ToUpper(),
                        UserName = userName,
                        BvPartNo = bvPartNo,
                        ShippingAddress = address,
                        Address = address,
                        City = city,
                        Province = province,
                        PostalCode = postalCode,
                        V21Ban = v21Ban,
                        ContactEmail = contactEmail,
                        RogersSpecialistEmail = rogersEmail,
                        HardwareType = hardwareType,
                        PurolatorNumber = purolator,
                        ReturnPurolatorNumber = returnPurolator,
                        DciInvoice = dciInvoice,
                        Status = status,
                        CompletedDate = completedDate,
                        Note = note,
                        Validated = false,
                        Imported = false,
                        UserId = userId,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            await _context.AdvantageVoiceImports.AddRangeAsync(entities);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<AdvantageImportVM>> ValidateDataAsync(int userId)
        {
            var records = await _context.AdvantageVoiceImports.Where(x => !x.Imported && x.UserId == userId).ToListAsync();
            if (!records.Any()) return new List<AdvantageImportVM>();

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            await using var sqlConn = new SqlConnection(_sqlConn);
            await sqlConn.OpenAsync();

            var lineCounts = records.GroupBy(x => x.GOrderNumber).ToDictionary(g => g.Key, g => g.Count());

            foreach (var row in records)
            {
                string reason = "";
                bool failed = false;

                var skuQuery = @"SELECT product_code, misc_2 FROM inventory WHERE whse = 'CO' AND part_no = @PartNo";
                await using var cmdSku = new NpgsqlCommand(skuQuery, pgConn);
                cmdSku.Parameters.AddWithValue("@PartNo", row.BvPartNo ?? "");

                string prodCode = "";
                int misc2 = 0;
                await using (var skuReader = await cmdSku.ExecuteReaderAsync())
                {
                    if (!skuReader.Read())
                    {
                        failed = true;
                        reason += ";SKU not in Spire\\AdvantageVoice app";
                    }
                    else
                    {
                        prodCode = skuReader["product_code"]?.ToString();
                        misc2 = skuReader["misc_2"] != DBNull.Value ? Convert.ToInt32(skuReader["misc_2"]) : 0;
                    }
                }

                if (!failed)
                {
                    var typeQuery = "SELECT Type FROM tblSKU WHERE SKU = @SKU";
                    await using var cmdType = new SqlCommand(typeQuery, sqlConn);
                    cmdType.Parameters.AddWithValue("@SKU", row.BvPartNo);
                    var skuType = (await cmdType.ExecuteScalarAsync())?.ToString();

                    if (skuType == null)
                    {
                        failed = true;
                        reason += ";SKU not in Spire\\AdvantageVoice app";
                    }
                    else
                    {
                        if (prodCode == "ACC" && skuType != "Accessory")
                        {
                            failed = true;
                            reason += ";Spire part is not ACC";
                        }
                        if (prodCode == "HCC" && skuType != "Hardware")
                        {
                            failed = true;
                            reason += ";Spire part is not HCC";
                        }
                    }

                    if (prodCode == "HCC")
                    {
                        if (string.IsNullOrWhiteSpace(row.MacAddress))
                        {
                            failed = true;
                            reason += ";Missing MAC ADDRESS";
                        }
                        else if (row.MacAddress.Length != misc2 && misc2 > 0)
                        {
                            failed = true;
                            reason += $";MAC ADDRESS not {misc2} digits";
                        }

                        if (!failed)
                        {
                            var macQuery = @"SELECT 1 FROM inventory_serial_numbers 
                                             WHERE whse = 'CO' AND part_no = @PartNo AND number = @Mac 
                                             AND (closed = '0' OR closed IS NULL) 
                                             AND committed_qty = 0 AND temp_qty = 0 AND onhand_qty <> 0";
                            await using var cmdMac = new NpgsqlCommand(macQuery, pgConn);
                            cmdMac.Parameters.AddWithValue("@PartNo", row.BvPartNo);
                            cmdMac.Parameters.AddWithValue("@Mac", row.MacAddress);

                            var exists = await cmdMac.ExecuteScalarAsync();
                            if (exists == null)
                            {
                                failed = true;
                                reason += ";MAC ADDRESS not available";
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(row.MacAddress))
                        {
                            failed = true;
                            reason += ";MAC ADDRESS provided for not HCC item";
                        }
                    }

                    if (prodCode == "ACC")
                    {
                        int count = lineCounts.GetValueOrDefault(row.GOrderNumber, 0);
                        row.OrderType = (count == 1) ? "Accessory Only" : "Accessories";
                    }
                    else
                    {
                        if (row.OrderType?.ToUpper().Contains("EXCH") == true)
                            row.OrderType = "Exchange Only";
                        else
                            row.OrderType = "Hardware Only";
                    }
                }

                row.Validated = !failed;
                row.Reason = reason.TrimStart(';');
            }

            var multiHardwareGroups = records
                .Where(x => x.OrderType == "Exchange Only" || x.OrderType == "Hardware Only")
                .GroupBy(x => x.GOrderNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var row in records.Where(x => multiHardwareGroups.Contains(x.GOrderNumber)))
            {
                row.Validated = false;
                row.Reason += ";orders can have only one Hardware";
            }

            var duplicates = records
                .Where(x => !string.IsNullOrWhiteSpace(x.MacAddress))
                .GroupBy(x => x.MacAddress)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var row in records.Where(x => duplicates.Contains(x.MacAddress)))
            {
                row.Validated = false;
                row.Reason += ";Duplicate Mac address";
            }

            await _context.SaveChangesAsync();
            return await GetPendingImportsAsync(userId);
        }

        public async Task<bool> SubmitOrdersAsync(int userId)
        {
            var validRecords = await _context.AdvantageVoiceImports
                .Where(x => !x.Imported && x.Validated && x.UserId == userId)
                .ToListAsync();

            if (!validRecords.Any()) return false;

            await using var sqlConn = new SqlConnection(_sqlConn);
            await sqlConn.OpenAsync();

            var transaction = sqlConn.BeginTransaction();

            try
            {
                foreach (var rec in validRecords)
                {
                    var finalOrderNo = await GetNextOrderNo();

                    var dashIndex = rec.GOrderNumber.IndexOf('-');
                    var baseGNo = dashIndex > 0 ? rec.GOrderNumber.Substring(0, dashIndex) : rec.GOrderNumber;

                    rec.GOrderNumber = $"{baseGNo}-{finalOrderNo:D6}";
                    rec.SpireOrder = $"ADV-{finalOrderNo:D6}";

                    var insertQuery = @"INSERT INTO dbo_t_orderimport (
                        fff_commision, commission_part_no, hardware_billed_by_rogers, company_name, 
                        shipping_company_name, shipping_contact_name, bus_tel, OrderDate, voice_date_label, 
                        whse, orderID, bulk_orderid, org_web_orderID, rogers_cell_number, imei, user_name, 
                        bvpartno, shipping_address, address, city, shipping_city, shippingprovincename, 
                        hardwareprovincename, shipping_postal, postal, data_version, nds_chanelID, 
                        nds_channel_name, bv_territory_code, hardware_payment_methodID, hardware_country_code, 
                        shipping_country_code, ReadyToImport, BackOrder, DeviceOfferTypeID, UpfrontEdgePrice, 
                        V21DealerCode, AccountNumber, gst_percent, pst_percent, phone_cost, qty,
                        authorized_cost_centre, cost_centre_display_name
                    ) VALUES (
                        'TRUE', 'PPN#ADV', 'TRUE', @comp, @comp, @contact, @tel, @date, @label, 
                        'CO', @spire, @spire, @gNo, @tempNo, @mac, @user, @sku, @addr, @addr, @city, 
                        @city, @prov, @prov, @zip, @zip, 1, 0, 'ADV', 'ADV', 3, 'CDN', 'CDN', 1, 0, 1, 0, 
                        '17PBM', @v21, 0, 0, '0', '1', @purolator, 'Purolator No')";

                    await using var cmd = new SqlCommand(insertQuery, sqlConn, (SqlTransaction)transaction);
                    cmd.Parameters.AddWithValue("@comp", (object)rec.CompanyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@contact", (object)rec.ShippingContact ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tel", (object)rec.ContactNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@date", (object)rec.OrderDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@label", (object)rec.OrderType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@spire", (object)rec.SpireOrder ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gNo", (object)rec.GOrderNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tempNo", (object)rec.TemporaryNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mac", (object)rec.MacAddress ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@user", (object)rec.UserName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@sku", (object)rec.BvPartNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addr", (object)rec.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@city", (object)rec.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@prov", (object)rec.Province ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@zip", (object)rec.PostalCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@v21", (object)rec.V21Ban ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purolator", (object)rec.PurolatorNumber ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();

                    rec.Imported = true;
                    rec.ModifiedBy = userId;
                    rec.ModifiedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public byte[] GenerateTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Template");

            string[] columns = {
                "Order date", "V21 BAN", "Order Type-Hardware-Exchange-Accessory", "COMPANY NAME",
                "SHIPPING CONTACT", "CONTACT NUMBER", "Customer CONTACT EMAIL ADDRESS",
                "Rogers Delivery Specialist Email Address", "G Order Number", "First Name and Last Name",
                "Temporary Number", "Hardware Type", "Hardware SKU", "Delivery Unit and Street Address",
                "City", "Province", "Postal Code", "MAC ADDRESS", "Purolator Number",
                "RETURN PRODUCT Purolator Number", "DCI INVOICE", "Status", "COMPLETED DATE", "NOTE"
            };

            for (int i = 0; i < columns.Length; i++)
            {
                ws.Cells[1, i + 1].Value = columns[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        #region Helper Methods

        private string CleanCustomerName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Replace("\u00A0", " ");
            StringBuilder sb = new StringBuilder();
            foreach (char c in s) { if (c >= 32 && c != 127) sb.Append(c); }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private string formatPhone(string strPhone)
        {
            if (string.IsNullOrWhiteSpace(strPhone)) return "";
            string digits = new string(strPhone.Where(char.IsDigit).ToArray());
            if (digits.Length < 10) return digits;
            digits = digits.Substring(0, 10);
            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }

        private async Task<long> GetNextOrderNo()
        {
            if (string.IsNullOrEmpty(_sqlConn)) throw new Exception("Connection string missing.");
            await using var sqlConn = new SqlConnection(_sqlConn);
            await sqlConn.OpenAsync();
            var query = @"
                IF NOT EXISTS (SELECT 1 FROM tblAdvantageSettings)
                BEGIN
                    INSERT INTO tblAdvantageSettings (NextOrderNo, NextTempOrderNo) VALUES (2, 1);
                    SELECT 1;
                END
                ELSE
                BEGIN
                    UPDATE tblAdvantageSettings SET NextOrderNo = NextOrderNo + 1 OUTPUT INSERTED.NextOrderNo - 1;
                END";
            await using var cmd = new SqlCommand(query, sqlConn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result ?? 1);
        }

        private async Task<long> GetNextTempOrderNo()
        {
            if (string.IsNullOrEmpty(_sqlConn)) throw new Exception("Connection string missing.");
            await using var sqlConn = new SqlConnection(_sqlConn);
            await sqlConn.OpenAsync();
            var query = @"
                IF NOT EXISTS (SELECT 1 FROM tblAdvantageSettings)
                BEGIN
                    INSERT INTO tblAdvantageSettings (NextOrderNo, NextTempOrderNo) VALUES (1, 2);
                    SELECT 1;
                END
                ELSE
                BEGIN
                    UPDATE tblAdvantageSettings SET NextTempOrderNo = NextTempOrderNo + 1 OUTPUT INSERTED.NextTempOrderNo - 1;
                END";
            await using var cmd = new SqlCommand(query, sqlConn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result ?? 1);
        }

        #endregion
    }
}



