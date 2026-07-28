using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace DAL.Inventory.PriceProtection.RogerOverPayments
{
    public class RogerOverPaymentsDA : IRogerOverPayments
    {
        private readonly string _sqlConn;

        public RogerOverPaymentsDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection")
                ?? throw new InvalidOperationException("ConnectionString 'bvactivation_Connection' is missing.");
        }

        public async Task<List<ImportedFileRow>> GetImportedFilesSummaryAsync()
        {
            var list = new List<ImportedFileRow>();
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string checkTable = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRogersOverpayments]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[tblRogersOverpayments] (
                        [ID] INT IDENTITY(1,1) PRIMARY KEY,
                        [DEALER] NVARCHAR(255),
                        [ORDER_NUMBER] NVARCHAR(100),
                        [INVOICE_NUMBER] NVARCHAR(100),
                        [IMEI] NVARCHAR(50),
                        [SKU] NVARCHAR(50),
                        [SKU_DESCRIPTION] NVARCHAR(255),
                        [NEW_PRICE] DECIMAL(18,4),
                        [DEALER_COST] DECIMAL(18,4),
                        [PP_AMOUNT] DECIMAL(18,4),
                        [CM_No] NVARCHAR(100),
                        [CM_Date] DATETIME,
                        [DateImported] DATETIME DEFAULT GETDATE(),
                        [Filename] NVARCHAR(255)
                    );
                END";
            using (var cmdTable = new SqlCommand(checkTable, conn))
            {
                await cmdTable.ExecuteNonQueryAsync();
            }

            string sql = @"
                SELECT Filename, MIN(DateImported) AS ImportedDate, COUNT(*) AS Count
                FROM tblRogersOverpayments
                GROUP BY Filename
                ORDER BY ImportedDate DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ImportedFileRow
                {
                    Filename = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                    ImportedDate = reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                    Count = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                });
            }
            return list;
        }

        public async Task<bool> ImportRogersOverpaymentsAsync(Stream fileStream, string filename)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null) return false;

            int rowCount = worksheet.Dimension.Rows;

            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string checkTable = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblRogersOverpayments]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[tblRogersOverpayments] (
                        [ID] INT IDENTITY(1,1) PRIMARY KEY,
                        [DEALER] NVARCHAR(255),
                        [ORDER_NUMBER] NVARCHAR(100),
                        [INVOICE_NUMBER] NVARCHAR(100),
                        [IMEI] NVARCHAR(50),
                        [SKU] NVARCHAR(50),
                        [SKU_DESCRIPTION] NVARCHAR(255),
                        [NEW_PRICE] DECIMAL(18,4),
                        [DEALER_COST] DECIMAL(18,4),
                        [PP_AMOUNT] DECIMAL(18,4),
                        [CM_No] NVARCHAR(100),
                        [CM_Date] DATETIME,
                        [DateImported] DATETIME DEFAULT GETDATE(),
                        [Filename] NVARCHAR(255)
                    );
                END";
            using (var cmdTable = new SqlCommand(checkTable, conn))
            {
                await cmdTable.ExecuteNonQueryAsync();
            }

            using var transaction = conn.BeginTransaction();
            try
            {
                // Row 1 is header, start from row 2
                for (int row = 2; row <= rowCount; row++)
                {
                    string dealer = worksheet.Cells[row, 1].Text;
                    if (string.IsNullOrWhiteSpace(dealer)) continue; // skip empty rows

                    string orderNum = worksheet.Cells[row, 2].Text;
                    string invoiceNum = worksheet.Cells[row, 3].Text;
                    string imei = worksheet.Cells[row, 4].Text;
                    string sku = worksheet.Cells[row, 5].Text;
                    string description = worksheet.Cells[row, 6].Text;

                    decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal newPrice);
                    decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal dealerCost);
                    decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal ppAmount);

                    string cmNo = worksheet.Cells[row, 10].Text;
                    DateTime? cmDate = null;
                    if (DateTime.TryParse(worksheet.Cells[row, 11].Text, out DateTime parsedDate))
                    {
                        cmDate = parsedDate;
                    }

                    string insertSql = @"
                        INSERT INTO tblRogersOverpayments (DEALER, ORDER_NUMBER, INVOICE_NUMBER, IMEI, SKU, SKU_DESCRIPTION, NEW_PRICE, DEALER_COST, PP_AMOUNT, CM_No, CM_Date, Filename)
                        VALUES (@Dealer, @OrderNum, @InvoiceNum, @Imei, @Sku, @Description, @NewPrice, @DealerCost, @PpAmount, @CmNo, @CmDate, @Filename)";

                    using var cmd = new SqlCommand(insertSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@Dealer", dealer);
                    cmd.Parameters.AddWithValue("@OrderNum", orderNum ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@InvoiceNum", invoiceNum ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Imei", imei ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Sku", sku ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewPrice", newPrice);
                    cmd.Parameters.AddWithValue("@DealerCost", dealerCost);
                    cmd.Parameters.AddWithValue("@PpAmount", ppAmount);
                    cmd.Parameters.AddWithValue("@CmNo", cmNo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CmDate", cmDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Filename", filename);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RemoveRecordsByFileAsync(string filename)
        {
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string sql = "DELETE FROM tblRogersOverpayments WHERE Filename = @Filename";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Filename", filename ?? (object)DBNull.Value);
            cmd.CommandTimeout = 600;

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<byte[]> ExportAllOverpaymentsExcelAsync()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Rogers Overpayments All");

            ws.Cells[1, 1].Value = "DEALER";
            ws.Cells[1, 2].Value = "ORDER_NUMBER";
            ws.Cells[1, 3].Value = "INVOICE_NUMBER";
            ws.Cells[1, 4].Value = "IMEI";
            ws.Cells[1, 5].Value = "SKU";
            ws.Cells[1, 6].Value = "SKU_DESCRIPTION";
            ws.Cells[1, 7].Value = "NEW_PRICE";
            ws.Cells[1, 8].Value = "DEALER_COST";
            ws.Cells[1, 9].Value = "PP_AMOUNT";
            ws.Cells[1, 10].Value = "CM_No";
            ws.Cells[1, 11].Value = "CM_Date";
            ws.Cells[1, 12].Value = "DateImported";
            ws.Cells[1, 13].Value = "Filename";

            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string sql = @"
                SELECT DEALER, ORDER_NUMBER, INVOICE_NUMBER, IMEI, SKU, SKU_DESCRIPTION, NEW_PRICE, DEALER_COST, PP_AMOUNT, CM_No, CM_Date, DateImported, Filename 
                FROM tblRogersOverpayments";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            int row = 2;
            while (await reader.ReadAsync())
            {
                ws.Cells[row, 1].Value = reader.IsDBNull(0) ? "" : reader.GetString(0);
                ws.Cells[row, 2].Value = reader.IsDBNull(1) ? "" : reader.GetString(1);
                ws.Cells[row, 3].Value = reader.IsDBNull(2) ? "" : reader.GetString(2);
                ws.Cells[row, 4].Value = reader.IsDBNull(3) ? "" : reader.GetString(3);
                ws.Cells[row, 5].Value = reader.IsDBNull(4) ? "" : reader.GetString(4);
                ws.Cells[row, 6].Value = reader.IsDBNull(5) ? "" : reader.GetString(5);
                ws.Cells[row, 7].Value = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6);
                ws.Cells[row, 8].Value = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7);
                ws.Cells[row, 9].Value = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8);
                ws.Cells[row, 10].Value = reader.IsDBNull(9) ? "" : reader.GetString(9);
                ws.Cells[row, 11].Value = reader.IsDBNull(10) ? "" : reader.GetDateTime(10).ToString("yyyy-MM-dd");
                ws.Cells[row, 12].Value = reader.IsDBNull(11) ? "" : reader.GetDateTime(11).ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cells[row, 13].Value = reader.IsDBNull(12) ? "" : reader.GetString(12);
                row++;
            }

            ws.Cells[1, 1, 1, 13].Style.Font.Bold = true;
            ws.Cells.AutoFitColumns();

            var fileBytes = await package.GetAsByteArrayAsync();
            return fileBytes;
        }
    }
}
