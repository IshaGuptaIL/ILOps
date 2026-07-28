using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DAL.Inventory.PriceProtection.ImeiSearch
{
    public class ImeiSearchDA : IImeiSearch
    {
        private readonly string _sqlConn;

        public ImeiSearchDA(IConfiguration configuration)
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

                // Ensure tblPriceProtection has Flag and Memo
                string checkPP = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblPriceProtection]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[tblPriceProtection] (
                        [ID] INT IDENTITY(1,1) PRIMARY KEY,
                        [ReceiptNo] NVARCHAR(50),
                        [ReceiptDate] DATETIME,
                        [ReceiptCost] DECIMAL(18,4),
                        [PriceDropDate] DATETIME,
                        [PriceBeforeDrop] DECIMAL(18,4),
                        [PriceAfterDrop] DECIMAL(18,4),
                        [SKU] NVARCHAR(50),
                        [Description] NVARCHAR(255),
                        [IMEI] NVARCHAR(50),
                        [ClaimAmount] DECIMAL(18,4),
                        [PreviousClaim] DECIMAL(18,4),
                        [PONumber] NVARCHAR(50),
                        [ClaimDate] DATETIME,
                        [ClaimBatchID] INT,
                        [ClaimAmountPaid] DECIMAL(18,4),
                        [Flag] BIT NULL,
                        [Memo] NVARCHAR(MAX) NULL,
                        [CreatedBy] NVARCHAR(100),
                        [CreatedDate] DATETIME DEFAULT GETDATE(),
                        [ModifiedBy] NVARCHAR(100),
                        [ModifiedDate] DATETIME DEFAULT GETDATE()
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tblPriceProtection]') AND name = 'Flag')
                        ALTER TABLE [dbo].[tblPriceProtection] ADD [Flag] BIT NULL;
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tblPriceProtection]') AND name = 'Memo')
                        ALTER TABLE [dbo].[tblPriceProtection] ADD [Memo] NVARCHAR(MAX) NULL;
                END";
                using (var cmd = new SqlCommand(checkPP, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Ensure tblPPCredits has PPClaimID, SKU, CreditNoteNumber, CreditNoteDate
                string checkCredits = @"
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
                using (var cmd = new SqlCommand(checkCredits, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in EnsureTablesCreated (ImeiSearchDA): " + ex.Message);
            }
        }

        public async Task<List<ImeiSearchClaimRow>> GetClaimsByImeiAsync(string imei)
        {
            var list = new List<ImeiSearchClaimRow>();
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string query = @"
                SELECT 
                    ID, ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, PriceBeforeDrop, PriceAfterDrop, 
                    SKU, Description, IMEI, ClaimDate, ClaimAmount, 
                    CAST(CASE WHEN ClaimAmountPaid >= ClaimAmount THEN 1 ELSE 0 END AS BIT) AS ClaimPaid,
                    CAST(0 AS BIT) AS Flag,
                    CAST(PreviousClaim AS VARCHAR(50)) AS PreviousClaim,
                    '' AS Memo, PONumber,
                    COALESCE((SELECT SUM(UnitCreditAmount) FROM tblPPCredits WHERE PPClaimID = tblPriceProtection.ID), 0) AS UnitCredit,
                    0.00 AS UnitDebit,
                    (ReceiptCost - COALESCE((SELECT SUM(UnitCreditAmount) FROM tblPPCredits WHERE PPClaimID = tblPriceProtection.ID), 0)) AS NetUnitCost,
                    '' AS LastInvoice,
                    COALESCE((SELECT TOP 1 CreditNoteNumber FROM tblPPCredits WHERE PPClaimID = tblPriceProtection.ID ORDER BY CreditNoteDate DESC), '') AS LastCredit,
                    NULL AS LastInvoiceDate,
                    (SELECT TOP 1 CreditNoteDate FROM tblPPCredits WHERE PPClaimID = tblPriceProtection.ID ORDER BY CreditNoteDate DESC) AS LastCreditDate
                FROM tblPriceProtection
                WHERE @Imei IS NULL OR @Imei = '' OR IMEI = @Imei";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Imei", imei ?? (object)DBNull.Value);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ImeiSearchClaimRow
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    ReceiptNo = reader.IsDBNull(reader.GetOrdinal("ReceiptNo")) ? null : reader.GetString(reader.GetOrdinal("ReceiptNo")),
                    ReceiptDate = reader.IsDBNull(reader.GetOrdinal("ReceiptDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReceiptDate")),
                    ReceiptCost = reader.IsDBNull(reader.GetOrdinal("ReceiptCost")) ? 0 : reader.GetDecimal(reader.GetOrdinal("ReceiptCost")),
                    PriceDropDate = reader.IsDBNull(reader.GetOrdinal("PriceDropDate")) ? null : reader.GetDateTime(reader.GetOrdinal("PriceDropDate")),
                    PriceBeforeDrop = reader.IsDBNull(reader.GetOrdinal("PriceBeforeDrop")) ? 0 : reader.GetDecimal(reader.GetOrdinal("PriceBeforeDrop")),
                    PriceAfterDrop = reader.IsDBNull(reader.GetOrdinal("PriceAfterDrop")) ? 0 : reader.GetDecimal(reader.GetOrdinal("PriceAfterDrop")),
                    SKU = reader.IsDBNull(reader.GetOrdinal("SKU")) ? null : reader.GetString(reader.GetOrdinal("SKU")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    IMEI = reader.IsDBNull(reader.GetOrdinal("IMEI")) ? null : reader.GetString(reader.GetOrdinal("IMEI")),
                    ClaimDate = reader.IsDBNull(reader.GetOrdinal("ClaimDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ClaimDate")),
                    ClaimAmount = reader.IsDBNull(reader.GetOrdinal("ClaimAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("ClaimAmount")),
                    ClaimPaid = reader.GetBoolean(reader.GetOrdinal("ClaimPaid")),
                    Flag = reader.GetBoolean(reader.GetOrdinal("Flag")),
                    PreviousClaim = reader.IsDBNull(reader.GetOrdinal("PreviousClaim")) ? null : reader.GetString(reader.GetOrdinal("PreviousClaim")),
                    Memo = reader.IsDBNull(reader.GetOrdinal("Memo")) ? null : reader.GetString(reader.GetOrdinal("Memo")),
                    PONumber = reader.IsDBNull(reader.GetOrdinal("PONumber")) ? null : reader.GetString(reader.GetOrdinal("PONumber")),
                    UnitCredit = reader.IsDBNull(reader.GetOrdinal("UnitCredit")) ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitCredit")),
                    UnitDebit = reader.IsDBNull(reader.GetOrdinal("UnitDebit")) ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitDebit")),
                    NetUnitCost = reader.IsDBNull(reader.GetOrdinal("NetUnitCost")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NetUnitCost")),
                    LastInvoice = reader.IsDBNull(reader.GetOrdinal("LastInvoice")) ? null : reader.GetString(reader.GetOrdinal("LastInvoice")),
                    LastCredit = reader.IsDBNull(reader.GetOrdinal("LastCredit")) ? null : reader.GetString(reader.GetOrdinal("LastCredit")),
                    LastInvoiceDate = reader.IsDBNull(reader.GetOrdinal("LastInvoiceDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastInvoiceDate")),
                    LastCreditDate = reader.IsDBNull(reader.GetOrdinal("LastCreditDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastCreditDate"))
                });
            }
            return list;
        }

        public async Task<List<ImeiSearchCreditRow>> GetCreditsByImeiAsync(string imei)
        {
            var list = new List<ImeiSearchCreditRow>();
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string query = @"
                SELECT PPClaimID, ReceiptNo, SKU, IMEI, UnitCreditAmount, CreditNoteNumber, CreditNoteDate
                FROM tblPPCredits
                WHERE @Imei IS NULL OR @Imei = '' OR IMEI = @Imei";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Imei", imei ?? (object)DBNull.Value);
            cmd.CommandTimeout = 600;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ImeiSearchCreditRow
                {
                    PPClaimID = reader.IsDBNull(reader.GetOrdinal("PPClaimID")) ? 0 : reader.GetInt32(reader.GetOrdinal("PPClaimID")),
                    ReceiptNo = reader.IsDBNull(reader.GetOrdinal("ReceiptNo")) ? null : reader.GetString(reader.GetOrdinal("ReceiptNo")),
                    SKU = reader.IsDBNull(reader.GetOrdinal("SKU")) ? null : reader.GetString(reader.GetOrdinal("SKU")),
                    IMEI = reader.IsDBNull(reader.GetOrdinal("IMEI")) ? null : reader.GetString(reader.GetOrdinal("IMEI")),
                    UnitCreditAmount = reader.IsDBNull(reader.GetOrdinal("UnitCreditAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("UnitCreditAmount")),
                    CreditNoteNumber = reader.IsDBNull(reader.GetOrdinal("CreditNoteNumber")) ? null : reader.GetString(reader.GetOrdinal("CreditNoteNumber")),
                    CreditNoteDate = reader.IsDBNull(reader.GetOrdinal("CreditNoteDate")) ? null : reader.GetDateTime(reader.GetOrdinal("CreditNoteDate"))
                });
            }
            return list;
        }

        public async Task<List<ImeiSearchOverpaymentRow>> GetOverpaymentsByImeiAsync(string imei)
        {
            var list = new List<ImeiSearchOverpaymentRow>();
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
            using (var cmd = new SqlCommand(checkTable, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            string query = @"
                SELECT DEALER, ORDER_NUMBER, INVOICE_NUMBER, IMEI, SKU, SKU_DESCRIPTION, NEW_PRICE, DEALER_COST, PP_AMOUNT, CM_No, CM_Date, DateImported, Filename
                FROM tblRogersOverpayments
                WHERE @Imei IS NULL OR @Imei = '' OR IMEI = @Imei";

            using var cmd2 = new SqlCommand(query, conn);
            cmd2.Parameters.AddWithValue("@Imei", imei ?? (object)DBNull.Value);
            cmd2.CommandTimeout = 600;

            using var reader = await cmd2.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ImeiSearchOverpaymentRow
                {
                    DEALER = reader.IsDBNull(reader.GetOrdinal("DEALER")) ? null : reader.GetString(reader.GetOrdinal("DEALER")),
                    ORDER_NUMBER = reader.IsDBNull(reader.GetOrdinal("ORDER_NUMBER")) ? null : reader.GetString(reader.GetOrdinal("ORDER_NUMBER")),
                    INVOICE_NUMBER = reader.IsDBNull(reader.GetOrdinal("INVOICE_NUMBER")) ? null : reader.GetString(reader.GetOrdinal("INVOICE_NUMBER")),
                    IMEI = reader.IsDBNull(reader.GetOrdinal("IMEI")) ? null : reader.GetString(reader.GetOrdinal("IMEI")),
                    SKU = reader.IsDBNull(reader.GetOrdinal("SKU")) ? null : reader.GetString(reader.GetOrdinal("SKU")),
                    SKU_DESCRIPTION = reader.IsDBNull(reader.GetOrdinal("SKU_DESCRIPTION")) ? null : reader.GetString(reader.GetOrdinal("SKU_DESCRIPTION")),
                    NEW_PRICE = reader.IsDBNull(reader.GetOrdinal("NEW_PRICE")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NEW_PRICE")),
                    DEALER_COST = reader.IsDBNull(reader.GetOrdinal("DEALER_COST")) ? 0 : reader.GetDecimal(reader.GetOrdinal("DEALER_COST")),
                    PP_AMOUNT = reader.IsDBNull(reader.GetOrdinal("PP_AMOUNT")) ? 0 : reader.GetDecimal(reader.GetOrdinal("PP_AMOUNT")),
                    CM_No = reader.IsDBNull(reader.GetOrdinal("CM_No")) ? null : reader.GetString(reader.GetOrdinal("CM_No")),
                    CM_Date = reader.IsDBNull(reader.GetOrdinal("CM_Date")) ? null : reader.GetDateTime(reader.GetOrdinal("CM_Date")),
                    DateImported = reader.IsDBNull(reader.GetOrdinal("DateImported")) ? null : reader.GetDateTime(reader.GetOrdinal("DateImported")),
                    Filename = reader.IsDBNull(reader.GetOrdinal("Filename")) ? null : reader.GetString(reader.GetOrdinal("Filename"))
                });
            }
            return list;
        }
    }
}
