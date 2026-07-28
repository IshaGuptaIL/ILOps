using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace DAL.Inventory.PriceProtection.OutputToExcel
{
    public class OutputToExcelDA : IOutputToExcel
    {
        private readonly string _sqlConn;

        public OutputToExcelDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection")
                ?? throw new InvalidOperationException("ConnectionString 'bvactivation_Connection' is missing.");
        }

        public async Task<byte[]> ExportPriceProtectionBatchAsync(int batchId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Price Protection Batch " + batchId);

            ws.Cells[1, 1].Value = "ID";
            ws.Cells[1, 2].Value = "ReceiptNo";
            ws.Cells[1, 3].Value = "ReceiptDate";
            ws.Cells[1, 4].Value = "ReceiptCost";
            ws.Cells[1, 5].Value = "PriceDropDate";
            ws.Cells[1, 6].Value = "PriceBeforeDrop";
            ws.Cells[1, 7].Value = "PriceAfterDrop";
            ws.Cells[1, 8].Value = "SKU";
            ws.Cells[1, 9].Value = "Description";
            ws.Cells[1, 10].Value = "IMEI";
            ws.Cells[1, 11].Value = "ClaimAmount";
            ws.Cells[1, 12].Value = "PONumber";
            ws.Cells[1, 13].Value = "ClaimDate";
            ws.Cells[1, 14].Value = "ClaimBatchID";
            ws.Cells[1, 15].Value = "ClaimAmountPaid";

            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = @"
                SELECT ID, ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, PriceBeforeDrop, PriceAfterDrop, 
                       SKU, Description, IMEI, ClaimAmount, PONumber, ClaimDate, ClaimBatchID, ClaimAmountPaid 
                FROM tblPriceProtection 
                WHERE ClaimBatchID = @BatchId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BatchId", batchId);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            int row = 2;
            while (await reader.ReadAsync())
            {
                ws.Cells[row, 1].Value = reader.GetInt32(0);
                ws.Cells[row, 2].Value = reader.IsDBNull(1) ? "" : reader.GetString(1);
                ws.Cells[row, 3].Value = reader.IsDBNull(2) ? "" : reader.GetDateTime(2).ToString("yyyy-MM-dd");
                ws.Cells[row, 4].Value = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
                ws.Cells[row, 5].Value = reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("yyyy-MM-dd");
                ws.Cells[row, 6].Value = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5);
                ws.Cells[row, 7].Value = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6);
                ws.Cells[row, 8].Value = reader.IsDBNull(7) ? "" : reader.GetString(7);
                ws.Cells[row, 9].Value = reader.IsDBNull(8) ? "" : reader.GetString(8);
                ws.Cells[row, 10].Value = reader.IsDBNull(9) ? "" : reader.GetString(9);
                ws.Cells[row, 11].Value = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10);
                ws.Cells[row, 12].Value = reader.IsDBNull(11) ? "" : reader.GetString(11);
                ws.Cells[row, 13].Value = reader.IsDBNull(12) ? "" : reader.GetDateTime(12).ToString("yyyy-MM-dd");
                ws.Cells[row, 14].Value = reader.IsDBNull(13) ? 0 : reader.GetInt32(13);
                ws.Cells[row, 15].Value = reader.IsDBNull(14) ? 0 : reader.GetDecimal(14);
                row++;
            }

            ws.Cells[1, 1, 1, 15].Style.Font.Bold = true;
            if (row > 2)
            {
                ws.Cells[1, 1, row - 1, 15].AutoFilter = true;
            }
            ws.Cells.AutoFitColumns();

            var fileBytes = await package.GetAsByteArrayAsync();
            return fileBytes;
        }

        public async Task<byte[]> ExportRogersOverpaymentsAsync()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Rogers Overpayments");

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

            // Check if table exists
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
            if (row > 2)
            {
                ws.Cells[1, 1, row - 1, 13].AutoFilter = true;
            }
            ws.Cells.AutoFitColumns();

            var fileBytes = await package.GetAsByteArrayAsync();
            return fileBytes;
        }

        public async Task<byte[]> ExportClaimsToCreditsAsync()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Claims to Credits");

            ws.Cells[1, 1].Value = "SKU";
            ws.Cells[1, 2].Value = "Description";
            ws.Cells[1, 3].Value = "IMEI";
            ws.Cells[1, 4].Value = "ClaimBatchID";
            ws.Cells[1, 5].Value = "ClaimDate";
            ws.Cells[1, 6].Value = "ClaimAmount";
            ws.Cells[1, 7].Value = "UnitCreditAmount";
            ws.Cells[1, 8].Value = "CreditNoteNumber";
            ws.Cells[1, 9].Value = "CreditNoteDate";

            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = @"
                SELECT tblPriceProtection.SKU, tblPriceProtection.Description, tblPriceProtection.IMEI, 
                       tblPriceProtection.ClaimBatchID, tblPriceProtection.ClaimDate, tblPriceProtection.ClaimAmount, 
                       tblPPCredits.UnitCreditAmount, tblPPCredits.CreditNoteNumber, tblPPCredits.CreditNoteDate
                FROM tblPriceProtection 
                LEFT JOIN tblPPCredits ON tblPriceProtection.ID = tblPPCredits.PPClaimID
                ORDER BY tblPriceProtection.SKU, tblPriceProtection.IMEI";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            int row = 2;
            while (await reader.ReadAsync())
            {
                ws.Cells[row, 1].Value = reader.IsDBNull(0) ? "" : reader.GetString(0);
                ws.Cells[row, 2].Value = reader.IsDBNull(1) ? "" : reader.GetString(1);
                ws.Cells[row, 3].Value = reader.IsDBNull(2) ? "" : reader.GetString(2);
                ws.Cells[row, 4].Value = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                ws.Cells[row, 5].Value = reader.IsDBNull(4) ? "" : reader.GetDateTime(4).ToString("yyyy-MM-dd");
                ws.Cells[row, 6].Value = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5);
                ws.Cells[row, 7].Value = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6);
                ws.Cells[row, 8].Value = reader.IsDBNull(7) ? "" : reader.GetString(7);
                ws.Cells[row, 9].Value = reader.IsDBNull(8) ? "" : reader.GetDateTime(8).ToString("yyyy-MM-dd");
                row++;
            }

            ws.Cells[1, 1, 1, 9].Style.Font.Bold = true;
            if (row > 2)
            {
                ws.Cells[1, 1, row - 1, 9].AutoFilter = true;
            }
            ws.Cells.AutoFitColumns();

            var fileBytes = await package.GetAsByteArrayAsync();
            return fileBytes;
        }

        public async Task<List<ClaimsToCreditsRow>> GetClaimsToCreditsDataAsync()
        {
            var list = new List<ClaimsToCreditsRow>();
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = @"
                SELECT tblPriceProtection.SKU, tblPriceProtection.Description, tblPriceProtection.IMEI, 
                       tblPriceProtection.ClaimBatchID, tblPriceProtection.ClaimDate, tblPriceProtection.ClaimAmount, 
                       tblPPCredits.UnitCreditAmount, tblPPCredits.CreditNoteNumber, tblPPCredits.CreditNoteDate
                FROM tblPriceProtection 
                LEFT JOIN tblPPCredits ON tblPriceProtection.ID = tblPPCredits.PPClaimID
                ORDER BY tblPriceProtection.SKU, tblPriceProtection.IMEI";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ClaimsToCreditsRow
                {
                    Sku = reader.IsDBNull(0) ? null : reader.GetString(0),
                    Description = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Imei = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ClaimBatchID = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    ClaimDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                    ClaimAmount = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    UnitCreditAmount = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                    CreditNoteNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreditNoteDate = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
                });
            }
            return list;
        }
    }
}
