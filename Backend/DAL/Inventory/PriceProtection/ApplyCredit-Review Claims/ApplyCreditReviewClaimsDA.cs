using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace DAL.Inventory.PriceProtection.ApplyCredit_ReviewClaims
{
    public class ApplyCreditReviewClaimsDA : IApplyCreditReviewClaims
    {
        private readonly string _sqlConn;
        private const string LogPath = @"d:\LAPP\backend_error.txt";

        public ApplyCreditReviewClaimsDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection")
                ?? throw new InvalidOperationException("ConnectionString 'bvactivation_Connection' is missing.");

            EnsureTablesCreated();
        }

        private void EnsureTablesCreated()
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                conn.Open();

                // Check and update tblPPCredits structure to add the columns from VBA schema
                string checkColumnsQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblPPCredits]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[tblPPCredits] (
                        [ID] INT IDENTITY(1,1) PRIMARY KEY,
                        [PPClaimID] INT,
                        [SKU] NVARCHAR(50),
                        [ReceiptNo] NVARCHAR(50),
                        [IMEI] NVARCHAR(50),
                        [UnitCreditAmount] DECIMAL(18,4),
                        [CreditNoteNumber] NVARCHAR(100),
                        [CreditNoteDate] DATETIME,
                        [CreatedBy] NVARCHAR(100),
                        [CreatedDate] DATETIME DEFAULT GETDATE(),
                        [ModifiedBy] NVARCHAR(100),
                        [ModifiedDate] DATETIME DEFAULT GETDATE()
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tblPPCredits]') AND name = 'PPClaimID')
                        ALTER TABLE [dbo].[tblPPCredits] ADD [PPClaimID] INT;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tblPPCredits]') AND name = 'SKU')
                        ALTER TABLE [dbo].[tblPPCredits] ADD [SKU] NVARCHAR(50);
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tblPPCredits]') AND name = 'CreditNoteNumber')
                        ALTER TABLE [dbo].[tblPPCredits] ADD [CreditNoteNumber] NVARCHAR(100);
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tblPPCredits]') AND name = 'CreditNoteDate')
                        ALTER TABLE [dbo].[tblPPCredits] ADD [CreditNoteDate] DATETIME;
                END";

                using (var cmd = new SqlCommand(checkColumnsQuery, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogException("EnsureTablesCreated", ex);
            }
        }

        private void LogException(string context, Exception ex)
        {
            try
            {
                string dir = Path.GetDirectoryName(LogPath) ?? "";
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in {context}: {ex}\n";
                File.AppendAllText(LogPath, content);
            }
            catch
            {
                // Prevent crash if logger fails
            }
        }

        public async Task<List<ClaimsSummaryRow>> GetClaimsSummaryAsync()
        {
            var list = new List<ClaimsSummaryRow>();
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                string sql = @"
                WITH qryCreditTotalByClaimID AS (
                    SELECT PPClaimID, SUM(UnitCreditAmount) AS SumOfUnitCreditAmount
                    FROM tblPPCredits
                    GROUP BY PPClaimID
                )
                SELECT 
                    p.ClaimBatchID, 
                    MAX(p.PriceDropDate) AS DatePriceDrop, 
                    MAX(p.SKU) AS PartNo, 
                    MAX(p.PriceBeforeDrop) AS PriceBefore, 
                    MAX(p.PriceAfterDrop) AS PriceAfter, 
                    COUNT(p.ID) AS [Count], 
                    SUM(p.ClaimAmount) AS TotalClaimed, 
                    SUM(COALESCE(c.SumOfUnitCreditAmount, 0)) AS TotalPaid, 
                    MIN(p.ID) AS MinOfID, 
                    (SUM(p.ClaimAmount) - SUM(COALESCE(c.SumOfUnitCreditAmount, 0))) AS TotalOutstanding
                FROM tblPriceProtection p
                LEFT JOIN qryCreditTotalByClaimID c ON p.ID = c.PPClaimID
                GROUP BY p.ClaimBatchID
                ORDER BY DatePriceDrop DESC, PartNo ASC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new ClaimsSummaryRow
                    {
                        ClaimBatchID = reader["ClaimBatchID"] != DBNull.Value ? Convert.ToInt32(reader["ClaimBatchID"]) : 0,
                        DatePriceDrop = reader["DatePriceDrop"] != DBNull.Value ? Convert.ToDateTime(reader["DatePriceDrop"]) : (DateTime?)null,
                        PartNo = reader["PartNo"]?.ToString() ?? string.Empty,
                        PriceBefore = reader["PriceBefore"] != DBNull.Value ? Convert.ToDecimal(reader["PriceBefore"]) : 0,
                        PriceAfter = reader["PriceAfter"] != DBNull.Value ? Convert.ToDecimal(reader["PriceAfter"]) : 0,
                        Count = reader["Count"] != DBNull.Value ? Convert.ToInt32(reader["Count"]) : 0,
                        TotalClaimed = reader["TotalClaimed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalClaimed"]) : 0,
                        TotalPaid = reader["TotalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPaid"]) : 0,
                        MinOfID = reader["MinOfID"] != DBNull.Value ? Convert.ToInt32(reader["MinOfID"]) : 0,
                        TotalOutstanding = reader["TotalOutstanding"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstanding"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                LogException("GetClaimsSummaryAsync", ex);
                throw;
            }
            return list;
        }

        public async Task<List<CreditSummaryRow>> GetCreditSummaryAsync(int claimBatchID)
        {
            var list = new List<CreditSummaryRow>();
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                string sql = @"
                SELECT 
                    p.ClaimBatchID, 
                    c.CreditNoteNumber, 
                    MAX(p.PriceDropDate) AS DatePriceDrop, 
                    MAX(p.SKU) AS PartNo, 
                    MAX(c.CreditNoteDate) AS CreditDate, 
                    MAX(p.PriceBeforeDrop) AS MaxOfPriceBeforeDrop, 
                    MAX(p.PriceAfterDrop) AS MaxOfPriceAfterDrop, 
                    COUNT(p.ID) AS [Count], 
                    MAX(c.UnitCreditAmount) AS UnitAmount, 
                    SUM(p.ClaimAmount) AS TotalClaimed, 
                    SUM(COALESCE(c.UnitCreditAmount, 0)) AS TotalPaid, 
                    SUM(CASE WHEN c.ReceiptNo IS NULL THEN 0 ELSE 1 END) AS CreditCount, 
                    MIN(p.ID) AS MinOfID, 
                    SUM(p.ClaimAmount - COALESCE(c.UnitCreditAmount, 0)) AS TotalOutstanding
                FROM tblPriceProtection p
                LEFT JOIN tblPPCredits c ON p.ID = c.PPClaimID
                WHERE p.ClaimBatchID = @ClaimBatchID
                GROUP BY p.ClaimBatchID, c.CreditNoteNumber
                ORDER BY DatePriceDrop DESC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@ClaimBatchID", claimBatchID);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new CreditSummaryRow
                    {
                        ClaimBatchID = reader["ClaimBatchID"] != DBNull.Value ? Convert.ToInt32(reader["ClaimBatchID"]) : 0,
                        CreditNoteNumber = reader["CreditNoteNumber"] != DBNull.Value ? reader["CreditNoteNumber"].ToString() : null,
                        DatePriceDrop = reader["DatePriceDrop"] != DBNull.Value ? Convert.ToDateTime(reader["DatePriceDrop"]) : (DateTime?)null,
                        PartNo = reader["PartNo"]?.ToString() ?? string.Empty,
                        CreditDate = reader["CreditDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreditDate"]) : (DateTime?)null,
                        MaxOfPriceBeforeDrop = reader["MaxOfPriceBeforeDrop"] != DBNull.Value ? Convert.ToDecimal(reader["MaxOfPriceBeforeDrop"]) : 0,
                        MaxOfPriceAfterDrop = reader["MaxOfPriceAfterDrop"] != DBNull.Value ? Convert.ToDecimal(reader["MaxOfPriceAfterDrop"]) : 0,
                        Count = reader["Count"] != DBNull.Value ? Convert.ToInt32(reader["Count"]) : 0,
                        UnitAmount = reader["UnitAmount"] != DBNull.Value ? Convert.ToDecimal(reader["UnitAmount"]) : 0,
                        TotalClaimed = reader["TotalClaimed"] != DBNull.Value ? Convert.ToDecimal(reader["TotalClaimed"]) : 0,
                        TotalPaid = reader["TotalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPaid"]) : 0,
                        CreditCount = reader["CreditCount"] != DBNull.Value ? Convert.ToInt32(reader["CreditCount"]) : 0,
                        MinOfID = reader["MinOfID"] != DBNull.Value ? Convert.ToInt32(reader["MinOfID"]) : 0,
                        TotalOutstanding = reader["TotalOutstanding"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOutstanding"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                LogException("GetCreditSummaryAsync", ex);
                throw;
            }
            return list;
        }

        public async Task<List<UnpaidClaimsDetailRow>> GetUnpaidClaimsDetailAsync(int claimBatchID, string? creditNoteNumber)
        {
            var list = new List<UnpaidClaimsDetailRow>();
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                // If creditNoteNumber is empty string or "null", treat as null
                if (string.IsNullOrWhiteSpace(creditNoteNumber) || creditNoteNumber.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    creditNoteNumber = null;
                }

                string sql = @"
                WITH qryCreditTotalByClaimID AS (
                    SELECT PPClaimID, SUM(UnitCreditAmount) AS SumOfUnitCreditAmount
                    FROM tblPPCredits
                    GROUP BY PPClaimID
                )
                SELECT 
                    p.ClaimBatchID, 
                    p.ID, 
                    p.PriceDropDate, 
                    p.SKU, 
                    c.CreditNoteNumber, 
                    p.IMEI, 
                    p.ReceiptDate, 
                    p.ReceiptCost, 
                    p.PriceBeforeDrop, 
                    p.PriceAfterDrop, 
                    p.ClaimAmount, 
                    COALESCE(ct.SumOfUnitCreditAmount, 0) AS ClaimAmountPaid
                FROM tblPriceProtection p
                LEFT JOIN tblPPCredits c ON p.ID = c.PPClaimID
                LEFT JOIN qryCreditTotalByClaimID ct ON p.ID = ct.PPClaimID
                WHERE p.ClaimBatchID = @ClaimBatchID
                  AND ((@CreditNoteNumber IS NULL AND c.CreditNoteNumber IS NULL) 
                       OR (c.CreditNoteNumber = @CreditNoteNumber))
                ORDER BY p.IMEI DESC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@ClaimBatchID", claimBatchID);
                cmd.Parameters.AddWithValue("@CreditNoteNumber", (object?)creditNoteNumber ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new UnpaidClaimsDetailRow
                    {
                        ClaimBatchID = reader["ClaimBatchID"] != DBNull.Value ? Convert.ToInt32(reader["ClaimBatchID"]) : 0,
                        ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                        PriceDropDate = reader["PriceDropDate"] != DBNull.Value ? Convert.ToDateTime(reader["PriceDropDate"]) : (DateTime?)null,
                        SKU = reader["SKU"]?.ToString() ?? string.Empty,
                        CreditNoteNumber = reader["CreditNoteNumber"] != DBNull.Value ? reader["CreditNoteNumber"].ToString() : null,
                        IMEI = reader["IMEI"]?.ToString() ?? string.Empty,
                        ReceiptDate = reader["ReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReceiptDate"]) : (DateTime?)null,
                        ReceiptCost = reader["ReceiptCost"] != DBNull.Value ? Convert.ToDecimal(reader["ReceiptCost"]) : 0,
                        PriceBeforeDrop = reader["PriceBeforeDrop"] != DBNull.Value ? Convert.ToDecimal(reader["PriceBeforeDrop"]) : 0,
                        PriceAfterDrop = reader["PriceAfterDrop"] != DBNull.Value ? Convert.ToDecimal(reader["PriceAfterDrop"]) : 0,
                        ClaimAmount = reader["ClaimAmount"] != DBNull.Value ? Convert.ToDecimal(reader["ClaimAmount"]) : 0,
                        ClaimAmountPaid = reader["ClaimAmountPaid"] != DBNull.Value ? Convert.ToDecimal(reader["ClaimAmountPaid"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                LogException("GetUnpaidClaimsDetailAsync", ex);
                throw;
            }
            return list;
        }

        public async Task<List<CreditDetailRow>> GetCreditDetailAsync(int ppClaimID)
        {
            var list = new List<CreditDetailRow>();
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                string sql = @"
                SELECT 
                    UnitCreditAmount, 
                    CreditNoteNumber, 
                    CreditNoteDate, 
                    PPClaimID, 
                    IMEI
                FROM tblPPCredits
                WHERE PPClaimID = @PPClaimID;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@PPClaimID", ppClaimID);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new CreditDetailRow
                    {
                        UnitCreditAmount = reader["UnitCreditAmount"] != DBNull.Value ? Convert.ToDecimal(reader["UnitCreditAmount"]) : 0,
                        CreditNoteNumber = reader["CreditNoteNumber"] != DBNull.Value ? reader["CreditNoteNumber"].ToString() : null,
                        CreditNoteDate = reader["CreditNoteDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreditNoteDate"]) : (DateTime?)null,
                        PPClaimID = reader["PPClaimID"] != DBNull.Value ? Convert.ToInt32(reader["PPClaimID"]) : 0,
                        IMEI = reader["IMEI"]?.ToString() ?? string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                LogException("GetCreditDetailAsync", ex);
                throw;
            }
            return list;
        }

        public async Task<bool> ModifyCreditNoteNumberAsync(string oldNumber, string newNumber, string user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldNumber))
                {
                    throw new ArgumentException("Old Credit Note Number cannot be empty for modification.");
                }
                if (string.IsNullOrWhiteSpace(newNumber))
                {
                    throw new ArgumentException("New Credit Note Number cannot be empty.");
                }

                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                string sql = @"
                UPDATE tblPPCredits 
                SET CreditNoteNumber = @NewNumber,
                    ModifiedBy = @User,
                    ModifiedDate = GETDATE()
                WHERE CreditNoteNumber = @OldNumber;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@NewNumber", newNumber);
                cmd.Parameters.AddWithValue("@OldNumber", oldNumber);
                cmd.Parameters.AddWithValue("@User", user);

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                LogException("ModifyCreditNoteNumberAsync", ex);
                throw;
            }
        }

        public async Task<bool> ApplyCreditAsync(ApplyCreditRequest request, string user)
        {
            if (request.SelectedClaimIds == null || request.SelectedClaimIds.Count == 0)
            {
                throw new ArgumentException("No claims selected to apply credits to.");
            }
            if (string.IsNullOrWhiteSpace(request.ApplyCreditNoteNumber))
            {
                throw new ArgumentException("Credit note number cannot be empty.");
            }

            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                using var transaction = conn.BeginTransaction();

                try
                {
                    // 1. Validation check: check if any of the items already have this credit note number
                    foreach (var claimId in request.SelectedClaimIds)
                    {
                        string checkSql = @"
                        SELECT COUNT(*) 
                        FROM tblPPCredits 
                        WHERE PPClaimID = @ClaimID AND CreditNoteNumber = @CreditNoteNumber;";

                        using var checkCmd = new SqlCommand(checkSql, conn, transaction);
                        checkCmd.CommandTimeout = 600;
                        checkCmd.Parameters.AddWithValue("@ClaimID", claimId);
                        checkCmd.Parameters.AddWithValue("@CreditNoteNumber", request.ApplyCreditNoteNumber);

                        int existsCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (existsCount > 0)
                        {
                            throw new InvalidOperationException($"One or more items selected has already had credit note {request.ApplyCreditNoteNumber} assigned to it.");
                        }
                    }

                    // 2. Perform INSERTs
                    foreach (var claimId in request.SelectedClaimIds)
                    {
                        string insertSql = @"
                        INSERT INTO tblPPCredits (
                            PPClaimID, SKU, ReceiptNo, IMEI, UnitCreditAmount, 
                            CreditNoteNumber, CreditNoteDate, CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
                        )
                        SELECT 
                            ID, SKU, ReceiptNo, IMEI, @UnitCreditAmount, 
                            @CreditNoteNumber, @CreditNoteDate, @User, GETDATE(), @User, GETDATE()
                        FROM tblPriceProtection
                        WHERE ID = @ClaimID;";

                        using var insertCmd = new SqlCommand(insertSql, conn, transaction);
                        insertCmd.CommandTimeout = 600;
                        insertCmd.Parameters.AddWithValue("@UnitCreditAmount", request.CreditUnitAmount);
                        insertCmd.Parameters.AddWithValue("@CreditNoteNumber", request.ApplyCreditNoteNumber);
                        insertCmd.Parameters.AddWithValue("@CreditNoteDate", request.ApplyCreditNoteDate);
                        insertCmd.Parameters.AddWithValue("@User", user);
                        insertCmd.Parameters.AddWithValue("@ClaimID", claimId);

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogException("ApplyCreditAsync", ex);
                throw;
            }
        }

        public async Task<byte[]> ExportClaimsSummaryExcelAsync()
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                string sql = @"
                WITH qryCreditTotalByClaimID AS (
                    SELECT PPClaimID, SUM(UnitCreditAmount) AS SumOfUnitCreditAmount
                    FROM tblPPCredits
                    GROUP BY PPClaimID
                )
                SELECT 
                    p.ClaimBatchID AS [Claim Batch ID], 
                    MAX(p.PriceDropDate) AS [Date Price Drop], 
                    MAX(p.SKU) AS [Part No], 
                    MAX(p.PriceBeforeDrop) AS [Price Before Drop], 
                    MAX(p.PriceAfterDrop) AS [Price After Drop], 
                    COUNT(p.ID) AS [Count], 
                    SUM(p.ClaimAmount) AS [Total Claimed], 
                    SUM(COALESCE(c.SumOfUnitCreditAmount, 0)) AS [Total Paid], 
                    MIN(p.ID) AS [Min Of ID], 
                    (SUM(p.ClaimAmount) - SUM(COALESCE(c.SumOfUnitCreditAmount, 0))) AS [Total Outstanding]
                FROM tblPriceProtection p
                LEFT JOIN qryCreditTotalByClaimID c ON p.ID = c.PPClaimID
                GROUP BY p.ClaimBatchID
                ORDER BY [Date Price Drop] DESC, [Part No] ASC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;

                using var adapter = new SqlDataAdapter(cmd);
                using var table = new DataTable();
                adapter.Fill(table);

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Claims Summary");
                ws.Cells["A1"].LoadFromDataTable(table, true);

                // Formatting headers
                using (var range = ws.Cells[1, 1, 1, table.Columns.Count])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Format DateTime and Decimal columns in Excel
                for (int col = 1; col <= table.Columns.Count; col++)
                {
                    string colName = table.Columns[col - 1].ColumnName;
                    if (colName.Contains("Price") || colName.Contains("Total") || colName.Contains("Amount"))
                    {
                        ws.Column(col).Style.Numberformat.Format = "$#,##0.00";
                    }
                    else if (colName.Contains("Date"))
                    {
                        ws.Column(col).Style.Numberformat.Format = "yyyy-mm-dd";
                    }
                }

                if (table.Rows.Count > 0)
                {
                    ws.Cells[1, 1, table.Rows.Count + 1, table.Columns.Count].AutoFilter = true;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                LogException("ExportClaimsSummaryExcelAsync", ex);
                throw;
            }
        }
    }
}
