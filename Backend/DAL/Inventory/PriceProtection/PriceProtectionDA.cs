using DAL.Inventory.PriceProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Inventory.PriceProtection
{
    public class PriceProtectionDA : IPriceProtection
    {
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public PriceProtectionDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection")
                ?? throw new InvalidOperationException("ConnectionString 'bvactivation_Connection' is missing.");
            _pgConn = configuration.GetConnectionString("spire_Connection")
                ?? throw new InvalidOperationException("ConnectionString 'spire_Connection' is missing.");

            EnsureTablesCreated();
        }

        private void EnsureTablesCreated()
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                conn.Open();

                // 1. Create tblPriceProtectionBatch
                string createBatch = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblPriceProtectionBatch]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[tblPriceProtectionBatch] (
                        [ID] INT IDENTITY(1,1) PRIMARY KEY,
                        [ReceiptNo] NVARCHAR(50),
                        [ReceiptDate] DATETIME,
                        [ReceiptCost] DECIMAL(18,4),
                        [PriceDropDate] DATETIME,
                        [SKU] NVARCHAR(50),
                        [Description] NVARCHAR(255),
                        [IMEI] NVARCHAR(50),
                        [ClaimDate] DATETIME,
                        [ClaimAmount] DECIMAL(18,4),
                        [PriceBeforeDrop] DECIMAL(18,4),
                        [PriceAfterDrop] DECIMAL(18,4),
                        [PreviousClaim] DECIMAL(18,4),
                        [Memo] NVARCHAR(MAX),
                        [PONumber] NVARCHAR(50),
                        [ClaimAmountPaid] DECIMAL(18,4),
                        [CreatedBy] NVARCHAR(100),
                        [CreatedDate] DATETIME DEFAULT GETDATE(),
                        [ModifiedBy] NVARCHAR(100),
                        [ModifiedDate] DATETIME DEFAULT GETDATE()
                    );
                END";
                using (var cmd = new SqlCommand(createBatch, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // 2. Create tblPriceProtection
                string createPP = @"
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
                using (var cmd = new SqlCommand(createPP, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // 3. Create tblPPCredits
                string createCredits = @"
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
                using (var cmd = new SqlCommand(createCredits, conn))
                {
                    cmd.ExecuteNonQuery();
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in EnsureTablesCreated: " + ex.Message);
            }
        }

        public async Task<int> GetNextBatchIDAsync()
        {
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = "SELECT COALESCE(MAX(ClaimBatchID), 0) + 1 FROM tblPriceProtection";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            var val = await cmd.ExecuteScalarAsync();
            return val != null ? Convert.ToInt32(val) : 1;
        }

        #region Onhand Claim Methods

        private async Task<List<OnhandSerialTemp>> GetOnhandSerialsFromSpireAsync(string sku, DateTime onhandDate)
        {
            var list = new List<OnhandSerialTemp>();

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();

            // SQL logic replicated from Access queries to select serials still onhand on that date
            string spireSql = @"
            WITH SerialMaxPO AS (
                SELECT 
                    t.number, 
                    MAX(t.id) as max_id
                FROM inventory_serial_transactions t
                LEFT JOIN sales_history sh ON t.link_no = sh.invoice_no AND t.link_type = 'SHIS'
                WHERE t.whse = 'CO' 
                  AND t.part_no = @PartNo 
                  AND t.link_type = 'PORD' 
                  AND COALESCE(t.receipt_date, sh.invoice_date) < @OnhandDate
                  AND t.recvd_qty > 0
                GROUP BY t.number
            ),
            SerialPOInfo AS (
                SELECT 
                    m.number,
                    m.max_id,
                    COALESCE(t.receipt_date, sh.invoice_date) as LatestPOReceiptDate,
                    t.receipt_no as LatestPOReceiptNo,
                    t.unit_cost as LatestPOReceiptCost,
                    t.link_no as LatestPONumber,
                    t.whse,
                    t.part_no
                FROM SerialMaxPO m
                JOIN inventory_serial_transactions t ON m.max_id = t.id
                LEFT JOIN sales_history sh ON t.link_no = sh.invoice_no AND t.link_type = 'SHIS'
            ),
            SerialReversals AS (
                SELECT DISTINCT i.number
                FROM SerialPOInfo i
                JOIN inventory_serial_transactions t 
                  ON i.number = t.number 
                 AND i.part_no = t.part_no 
                 AND i.whse = t.whse
                WHERE t.id > i.max_id 
                  AND t.link_type = 'PORD' 
                  AND t.recvd_qty < 0
            ),
            SalesQty AS (
                SELECT 
                    i.number,
                    COALESCE(SUM(t.sales_qty), 0) as sales_qty
                FROM SerialPOInfo i
                LEFT JOIN inventory_serial_transactions t 
                  ON i.number = t.number 
                 AND i.part_no = t.part_no 
                 AND i.whse = t.whse
                 AND t.link_type = 'SHIS'
                 AND COALESCE(t.receipt_date, (SELECT invoice_date FROM sales_history WHERE invoice_no = t.link_no LIMIT 1)) >= i.LatestPOReceiptDate 
                 AND COALESCE(t.receipt_date, (SELECT invoice_date FROM sales_history WHERE invoice_no = t.link_no LIMIT 1)) < @OnhandDate
                GROUP BY i.number
            )
            SELECT 
                i.whse as WAREHOUSE,
                i.part_no as PART_NO,
                i.number as NUMBER,
                i.max_id as LatestPOSNTransID,
                i.LatestPOReceiptDate,
                i.LatestPOReceiptNo,
                i.LatestPOReceiptCost,
                s.sales_qty as SalesQtySinceReceipt,
                i.LatestPONumber
            FROM SerialPOInfo i
            LEFT JOIN SalesQty s ON i.number = s.number
            WHERE i.number NOT IN (SELECT number FROM SerialReversals)
              AND COALESCE(s.sales_qty, 0) <= 0;";

            using var cmd = new NpgsqlCommand(spireSql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@PartNo", sku);
            cmd.Parameters.AddWithValue("@OnhandDate", onhandDate);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new OnhandSerialTemp
                {
                    Warehouse = reader["WAREHOUSE"].ToString(),
                    PartNo = reader["PART_NO"].ToString(),
                    Number = reader["NUMBER"].ToString(),
                    LatestPOReceiptDate = reader["LatestPOReceiptDate"] != DBNull.Value
            ? ((DateOnly)reader["LatestPOReceiptDate"]).ToDateTime(TimeOnly.MinValue)
            : null,
                    LatestPOReceiptNo = reader["LatestPOReceiptNo"].ToString(),
                    LatestPOReceiptCost = reader["LatestPOReceiptCost"] != DBNull.Value ? Convert.ToDecimal(reader["LatestPOReceiptCost"]) : 0,
                    LatestPONumber = reader["LatestPONumber"].ToString()
                });
            }

            return list;
        }

        private async Task<string> GetSkuDescriptionAsync(string sku)
        {
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            string sql = "SELECT description FROM inventory WHERE part_no = @Sku LIMIT 1";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Sku", sku);
            var res = await cmd.ExecuteScalarAsync();
            return res?.ToString() ?? "";
        }

        public async Task<bool> LoadClaimDataAsync(string sku, DateTime onhandDate)
        {
            try
            {
                var serials = await GetOnhandSerialsFromSpireAsync(sku, onhandDate);
                string description = await GetSkuDescriptionAsync(sku);

                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                // Clear current batch
                string clearSql = "DELETE FROM tblPriceProtectionBatch";
                using (var cmd = new SqlCommand(clearSql, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insert new rows with default values
                foreach (var s in serials)
                {
                    // Previous claim & credit check
                    decimal previousClaim = await GetPreviousClaimAmountAsync(s.Number, s.LatestPOReceiptNo, conn);
                    decimal claimAmountPaid = await GetPreviousClaimPaidAsync(s.Number, s.LatestPOReceiptNo, conn);

                    string insertSql = @"
                    INSERT INTO tblPriceProtectionBatch (
                        ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, SKU, Description, IMEI, ClaimDate, ClaimAmount,
                        PriceBeforeDrop, PriceAfterDrop, PreviousClaim, Memo, PONumber, ClaimAmountPaid, CreatedBy, ModifiedBy
                    ) VALUES (
                        @ReceiptNo, @ReceiptDate, @ReceiptCost, @PriceDropDate, @SKU, @Description, @IMEI, GETDATE(), 0,
                        0, 0, @PreviousClaim, '', @PONumber, @ClaimAmountPaid, 'System', 'System'
                    )";

                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@ReceiptNo", SafeSub(s.LatestPOReceiptNo, 50));
                    cmd.Parameters.AddWithValue("@ReceiptDate", (object)s.LatestPOReceiptDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReceiptCost", s.LatestPOReceiptCost);
                    cmd.Parameters.AddWithValue("@PriceDropDate", onhandDate);
                    cmd.Parameters.AddWithValue("@SKU", SafeSub(sku, 50));
                    cmd.Parameters.AddWithValue("@Description", SafeSub(description, 255));
                    cmd.Parameters.AddWithValue("@IMEI", SafeSub(s.Number, 50));
                    cmd.Parameters.AddWithValue("@PreviousClaim", previousClaim);
                    cmd.Parameters.AddWithValue("@PONumber", SafeSub(s.LatestPONumber, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmountPaid", claimAmountPaid);

                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                var errorText = $"=== Error in LoadClaimDataAsync ({DateTime.Now}) ===\n" +
                                $"SKU: {sku}\n" +
                                $"Error: {ex.Message}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                Console.WriteLine("Error in LoadClaimDataAsync: " + ex.Message);
                return false;
            }
        }

        public async Task<int> ProcessOnhandClaimAsync(string sku, DateTime onhandDate, decimal priceBefore, decimal priceAfter, string user)
        {
            try
            {
                var serials = await GetOnhandSerialsFromSpireAsync(sku, onhandDate);
                string description = await GetSkuDescriptionAsync(sku);

                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                // Clear current batch
                string clearSql = "DELETE FROM tblPriceProtectionBatch";
                using (var cmd = new SqlCommand(clearSql, conn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                int processedCount = 0;

                foreach (var s in serials)
                {
                    decimal previousClaim = await GetPreviousClaimAmountAsync(s.Number, s.LatestPOReceiptNo, conn);
                    decimal claimAmountPaid = await GetPreviousClaimPaidAsync(s.Number, s.LatestPOReceiptNo, conn);

                    // claim = ReceiptCost - PreviousClaim - PriceAfter
                    decimal claim = s.LatestPOReceiptCost - previousClaim - priceAfter;

                    string memo = "";
                    if (string.IsNullOrEmpty(s.LatestPOReceiptNo)) memo += "SN record does not show receipt. ";
                    if (s.LatestPOReceiptDate > onhandDate) memo += "Receipt date after price drop date. ";

                    string insertSql = @"
                    INSERT INTO tblPriceProtectionBatch (
                        ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, SKU, Description, IMEI, ClaimDate, ClaimAmount,
                        PriceBeforeDrop, PriceAfterDrop, PreviousClaim, Memo, PONumber, ClaimAmountPaid, CreatedBy, ModifiedBy
                    ) VALUES (
                        @ReceiptNo, @ReceiptDate, @ReceiptCost, @PriceDropDate, @SKU, @Description, @IMEI, GETDATE(), @ClaimAmount,
                        @PriceBefore, @PriceAfter, @PreviousClaim, @Memo, @PONumber, @ClaimAmountPaid, @User, @User
                    )";

                    using var cmd = new SqlCommand(insertSql, conn);
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@ReceiptNo", SafeSub(s.LatestPOReceiptNo, 50));
                    cmd.Parameters.AddWithValue("@ReceiptDate", (object)s.LatestPOReceiptDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReceiptCost", s.LatestPOReceiptCost);
                    cmd.Parameters.AddWithValue("@PriceDropDate", onhandDate);
                    cmd.Parameters.AddWithValue("@SKU", SafeSub(sku, 50));
                    cmd.Parameters.AddWithValue("@Description", SafeSub(description, 255));
                    cmd.Parameters.AddWithValue("@IMEI", SafeSub(s.Number, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmount", claim);
                    cmd.Parameters.AddWithValue("@PriceBefore", priceBefore);
                    cmd.Parameters.AddWithValue("@PriceAfter", priceAfter);
                    cmd.Parameters.AddWithValue("@PreviousClaim", previousClaim);
                    cmd.Parameters.AddWithValue("@Memo", SafeSub(memo, 1000));
                    cmd.Parameters.AddWithValue("@PONumber", SafeSub(s.LatestPONumber, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmountPaid", claimAmountPaid);
                    cmd.Parameters.AddWithValue("@User", SafeSub(user, 100));

                    await cmd.ExecuteNonQueryAsync();
                    processedCount++;
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                var errorText = $"=== Error in ProcessOnhandClaimAsync ({DateTime.Now}) ===\n" +
                                $"SKU: {sku}\n" +
                                $"Error: {ex.Message}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                Console.WriteLine("Error in ProcessOnhandClaimAsync: " + ex.Message);
                return 0;
            }
        }

        #endregion

        #region Receipt Claim Methods

        public async Task<ReceiptInfoBO?> FindReceiptAsync(string receiptNo)
        {
            try
            {
                int rId = 0;
                int.TryParse(receiptNo.Trim(), out rId);

                using var conn = new NpgsqlConnection(_pgConn);
                await conn.OpenAsync();

                string spireSql = @"
                SELECT r.qty, i.part_no, r.cost, r.receive_date, r.link_no, i.description 
                FROM inventory_receipts r 
                INNER JOIN inventory i ON r.inventory_id = i.id 
                WHERE r.id = @ReceiptNo LIMIT 1;";

                using var cmd = new NpgsqlCommand(spireSql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@ReceiptNo", rId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new ReceiptInfoBO
                    {
                        PartNo = reader["part_no"].ToString(),
                        Cost = reader["cost"] != DBNull.Value ? Convert.ToDecimal(reader["cost"]) : 0,
                        Description = reader["description"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]) : 0,
                        PONumber = reader["link_no"].ToString()
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in FindReceiptAsync: " + ex.Message);
                return null;
            }
        }

        public async Task<int> ProcessReceiptClaimAsync(string receiptNo, DateTime dropDate, decimal priceBefore, decimal priceAfter, string user)
        {
            try
            {
                int rId = 0;
                int.TryParse(receiptNo.Trim(), out rId);

                var receiptInfo = await FindReceiptAsync(receiptNo);
                if (receiptInfo == null) return 0;

                // DIAGNOSTIC LOGS
                Console.WriteLine("=== ProcessReceiptClaimAsync DIAGNOSTICS ===");
                Console.WriteLine($"Original receiptNo input: '{receiptNo}'");
                Console.WriteLine($"Parsed rId: {rId}");
                Console.WriteLine($"Receipt Info SKU: {receiptInfo.PartNo}, Cost: {receiptInfo.Cost}, Qty: {receiptInfo.Qty}");

                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();

                    // 1. Get first 5 receipt_no values
                    using (var cmd = new NpgsqlCommand("SELECT receipt_no, link_type, recvd_qty FROM inventory_serial_transactions WHERE receipt_no IS NOT NULL LIMIT 5;", conn))
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        Console.WriteLine("Sample receipt_no from inventory_serial_transactions:");
                        while (await r.ReadAsync())
                        {
                            Console.WriteLine($"  receipt_no: {r["receipt_no"]} (Type: {r["receipt_no"]?.GetType()}), link_type: {r["link_type"]}, recvd_qty: {r["recvd_qty"]}");
                        }
                    }

                    // 2. Count matching serials with receiptNo (numeric and string versions)
                    string clean = receiptNo.Trim().TrimStart('0');
                    if (string.IsNullOrEmpty(clean)) clean = "0";

                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory_serial_transactions WHERE receipt_no::text = @r1 OR receipt_no::text = @r2;", conn))
                    {
                        cmd.Parameters.AddWithValue("@r1", clean);
                        cmd.Parameters.AddWithValue("@r2", receiptNo.Trim());
                        var count = await cmd.ExecuteScalarAsync();
                        Console.WriteLine($"Matching serial count in database: {count}");
                    }
                }

                // Load Serials associated with this Receipt No from Spire
                var serials = new List<string>();

                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();

                    // Cast receipt_no to text to avoid type mismatch error if column is bigint
                    string sql = @"
                    SELECT number 
                    FROM inventory_serial_transactions 
                    WHERE receipt_no::text = @ReceiptNo 
                       OR receipt_no::text = @ReceiptNoPadded 
                       OR receipt_no::text = @ReceiptNoPadded8;";

                    using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.CommandTimeout = 600;

                    // Trim leading zeros for raw match (e.g. '0000010025' -> '10025')
                    string cleanedReceiptNo = receiptNo.Trim().TrimStart('0');
                    if (string.IsNullOrEmpty(cleanedReceiptNo))
                    {
                        cleanedReceiptNo = "0";
                    }

                    cmd.Parameters.AddWithValue("@ReceiptNo", cleanedReceiptNo);
                    cmd.Parameters.AddWithValue("@ReceiptNoPadded", cleanedReceiptNo.PadLeft(10, '0'));
                    cmd.Parameters.AddWithValue("@ReceiptNoPadded8", cleanedReceiptNo.PadLeft(8, '0'));

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        serials.Add(reader["number"]?.ToString() ?? string.Empty);
                    }
                }

                using var sqlConn = new SqlConnection(_sqlConn);
                await sqlConn.OpenAsync();

                // Clear current batch
                string clearSql = "DELETE FROM tblPriceProtectionBatch";
                using (var cmd = new SqlCommand(clearSql, sqlConn))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                int processedCount = 0;

                foreach (var imei in serials)
                {
                    decimal previousClaim = await GetPreviousClaimAmountAsync(imei, receiptNo, sqlConn);
                    decimal claimAmountPaid = await GetPreviousClaimPaidAsync(imei, receiptNo, sqlConn);

                    // claim = ReceiptCost - PreviousClaimPaid - PriceAfter
                    decimal claim = receiptInfo.Cost - claimAmountPaid - priceAfter;

                    string insertSql = @"
                    INSERT INTO tblPriceProtectionBatch (
                        ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, SKU, Description, IMEI, ClaimDate, ClaimAmount,
                        PriceBeforeDrop, PriceAfterDrop, PreviousClaim, Memo, PONumber, ClaimAmountPaid, CreatedBy, ModifiedBy
                    ) VALUES (
                        @ReceiptNo, @ReceiptDate, @ReceiptCost, @PriceDropDate, @SKU, @Description, @IMEI, GETDATE(), @ClaimAmount,
                        @PriceBefore, @PriceAfter, @PreviousClaim, '', @PONumber, @ClaimAmountPaid, @User, @User
                    )";

                    using var cmd = new SqlCommand(insertSql, sqlConn);
                    cmd.CommandTimeout = 600;

                    cmd.Parameters.AddWithValue("@ReceiptNo", SafeSub(receiptNo, 50));
                    cmd.Parameters.AddWithValue("@ReceiptDate", dropDate);
                    cmd.Parameters.AddWithValue("@ReceiptCost", receiptInfo.Cost);
                    cmd.Parameters.AddWithValue("@PriceDropDate", dropDate);
                    cmd.Parameters.AddWithValue("@SKU", SafeSub(receiptInfo.PartNo, 50));
                    cmd.Parameters.AddWithValue("@Description", SafeSub(receiptInfo.Description, 255));
                    cmd.Parameters.AddWithValue("@IMEI", SafeSub(imei, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmount", claim);
                    cmd.Parameters.AddWithValue("@PriceBefore", priceBefore);
                    cmd.Parameters.AddWithValue("@PriceAfter", priceAfter);
                    cmd.Parameters.AddWithValue("@PreviousClaim", previousClaim);
                    cmd.Parameters.AddWithValue("@PONumber", SafeSub(receiptInfo.PONumber, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmountPaid", claimAmountPaid);
                    cmd.Parameters.AddWithValue("@User", SafeSub(user, 100));

                    await cmd.ExecuteNonQueryAsync();
                    processedCount++;
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                var errorText = $"=== Error in ProcessReceiptClaimAsync ({DateTime.Now}) ===\n" +
                                $"ReceiptNo: {receiptNo}\n" +
                                $"Error: {ex.Message}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                Console.WriteLine("Error in ProcessReceiptClaimAsync: " + ex.Message);
                return 0;
            }
        }

        #endregion

        #region Manual IMEI Methods

        public async Task<bool> ManualAddImeiAsync(string imei, decimal priceBefore, decimal priceAfter, DateTime onhandDate, string sku, string description, string user)
        {
            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                // Check duplicate IMEI
                string checkSql = "SELECT COUNT(*) FROM tblPriceProtectionBatch WHERE IMEI = @Imei";
                using (var cmd = new SqlCommand(checkSql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@Imei", imei);
                    int exists = (int)await cmd.ExecuteScalarAsync();
                    if (exists > 0)
                        throw new InvalidOperationException("This IMEI already exists in this batch.");
                }

                // Check SKU match in batch
                string matchSql = "SELECT TOP 1 SKU FROM tblPriceProtectionBatch";
                using (var cmd = new SqlCommand(matchSql, conn))
                {
                    cmd.CommandTimeout = 600;
                    var currentSku = await cmd.ExecuteScalarAsync();
                    if (currentSku != null && currentSku.ToString() != sku)
                        throw new InvalidOperationException("Part number for this IMEI is not the same part as the current batch.");
                }

                // Lookup latest receipt details from Spire
                string spireSql = @"
                SELECT receipt_no, receipt_date, unit_cost, link_no, whse 
                FROM inventory_serial_transactions 
                WHERE number = @Imei AND part_no = @Sku AND link_type = 'PORD' 
                ORDER BY id DESC LIMIT 1;";

                string receiptNo = "";
                DateTime? receiptDate = null;
                decimal receiptCost = 0;
                string poNumber = "";

                using (var pgConn = new NpgsqlConnection(_pgConn))
                {
                    await pgConn.OpenAsync();
                    using var cmd = new NpgsqlCommand(spireSql, pgConn);
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@Imei", imei);
                    cmd.Parameters.AddWithValue("@Sku", sku);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        receiptNo = reader["receipt_no"]?.ToString() ?? "";
                        receiptDate = reader["receipt_date"] != DBNull.Value ? Convert.ToDateTime(reader["receipt_date"]) : (DateTime?)null;
                        receiptCost = reader["unit_cost"] != DBNull.Value ? Convert.ToDecimal(reader["unit_cost"]) : 0;
                        poNumber = reader["link_no"]?.ToString() ?? "";
                    }
                    else
                    {
                        throw new InvalidOperationException("Either no data has been loaded, or there are no units onhand on that date.");
                    }
                }

                decimal previousClaim = await GetPreviousClaimAmountAsync(imei, receiptNo, conn);
                decimal claimAmountPaid = await GetPreviousClaimPaidAsync(imei, receiptNo, conn);

                decimal claim = receiptCost - previousClaim - priceAfter;

                string insertSql = @"
                INSERT INTO tblPriceProtectionBatch (
                    ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, SKU, Description, IMEI, ClaimDate, ClaimAmount,
                    PriceBeforeDrop, PriceAfterDrop, PreviousClaim, Memo, PONumber, ClaimAmountPaid, CreatedBy, ModifiedBy
                ) VALUES (
                    @ReceiptNo, @ReceiptDate, @ReceiptCost, @PriceDropDate, @SKU, @Description, @IMEI, GETDATE(), @ClaimAmount,
                    @PriceBefore, @PriceAfter, @PreviousClaim, '', @PONumber, @ClaimAmountPaid, @User, @User
                )";

                using (var cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.CommandTimeout = 600;

                    cmd.Parameters.AddWithValue("@ReceiptNo", SafeSub(receiptNo, 50));
                    cmd.Parameters.AddWithValue("@ReceiptDate", (object)receiptDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReceiptCost", receiptCost);
                    cmd.Parameters.AddWithValue("@PriceDropDate", onhandDate);
                    cmd.Parameters.AddWithValue("@SKU", SafeSub(sku, 50));
                    cmd.Parameters.AddWithValue("@Description", SafeSub(description, 255));
                    cmd.Parameters.AddWithValue("@IMEI", SafeSub(imei, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmount", claim);
                    cmd.Parameters.AddWithValue("@PriceBefore", priceBefore);
                    cmd.Parameters.AddWithValue("@PriceAfter", priceAfter);
                    cmd.Parameters.AddWithValue("@PreviousClaim", previousClaim);
                    cmd.Parameters.AddWithValue("@PONumber", SafeSub(poNumber, 50));
                    cmd.Parameters.AddWithValue("@ClaimAmountPaid", claimAmountPaid);
                    cmd.Parameters.AddWithValue("@User", SafeSub(user, 100));

                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                var errorText = $"=== Error in ManualAddImeiAsync ({DateTime.Now}) ===\n" +
                                $"IMEI: {imei}\n" +
                                $"Error: {ex.Message}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task<bool> ManualRemoveImeiAsync(string imei)
        {
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = "DELETE FROM tblPriceProtectionBatch WHERE IMEI = @Imei";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Imei", imei);
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        #endregion

        #region Claims Operations

        public async Task<List<PriceProtectionBatchRow>> GetBatchDataAsync()
        {
            var list = new List<PriceProtectionBatchRow>();
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = @"
            SELECT ID, ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, SKU, Description, IMEI, ClaimDate, 
                   ClaimAmount, PriceBeforeDrop, PriceAfterDrop, PreviousClaim, Memo, PONumber, ClaimAmountPaid 
            FROM tblPriceProtectionBatch ORDER BY ID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PriceProtectionBatchRow
                {
                    ID = Convert.ToInt32(reader["ID"]),
                    ReceiptNo = reader["ReceiptNo"].ToString(),
                    ReceiptDate = reader["ReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReceiptDate"]) : (DateTime?)null,
                    ReceiptCost = Convert.ToDecimal(reader["ReceiptCost"]),
                    PriceDropDate = reader["PriceDropDate"] != DBNull.Value ? Convert.ToDateTime(reader["PriceDropDate"]) : (DateTime?)null,
                    SKU = reader["SKU"].ToString(),
                    Description = reader["Description"].ToString(),
                    IMEI = reader["IMEI"].ToString(),
                    ClaimDate = reader["ClaimDate"] != DBNull.Value ? Convert.ToDateTime(reader["ClaimDate"]) : (DateTime?)null,
                    ClaimAmount = Convert.ToDecimal(reader["ClaimAmount"]),
                    PriceBeforeDrop = Convert.ToDecimal(reader["PriceBeforeDrop"]),
                    PriceAfterDrop = Convert.ToDecimal(reader["PriceAfterDrop"]),
                    PreviousClaim = Convert.ToDecimal(reader["PreviousClaim"]),
                    Memo = reader["Memo"].ToString(),
                    PONumber = reader["PONumber"].ToString(),
                    ClaimAmountPaid = Convert.ToDecimal(reader["ClaimAmountPaid"])
                });
            }
            return list;
        }

        public async Task<bool> AppendClaimAsync(string password, string user)
        {
            if (password != "subaru")
                throw new UnauthorizedAccessException("Password incorrect.");

            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            int nextBatchID = await GetNextBatchIDAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                // 1. Copy batch to tblPriceProtection
                string insertSql = @"
                INSERT INTO tblPriceProtection (
                    ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, PriceBeforeDrop, PriceAfterDrop, SKU, Description,
                    IMEI, ClaimAmount, PreviousClaim, PONumber, ClaimDate, ClaimBatchID, ClaimAmountPaid, CreatedBy, ModifiedBy
                )
                SELECT 
                    ReceiptNo, ReceiptDate, ReceiptCost, PriceDropDate, PriceBeforeDrop, PriceAfterDrop, SKU, Description,
                    IMEI, ClaimAmount, PreviousClaim, PONumber, ClaimDate, @BatchID, ClaimAmountPaid, @User, @User
                FROM tblPriceProtectionBatch";

                using (var cmd = new SqlCommand(insertSql, conn, transaction))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@BatchID", nextBatchID);
                    cmd.Parameters.AddWithValue("@User", user);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Clear tblPriceProtectionBatch
                string clearSql = "DELETE FROM tblPriceProtectionBatch";
                using (var cmd = new SqlCommand(clearSql, conn, transaction))
                {
                    cmd.CommandTimeout = 600;
                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> RemoveBatchAsync(int batchNo)
        {
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();
            string sql = "DELETE FROM tblPriceProtection WHERE ClaimBatchID = @BatchNo";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@BatchNo", batchNo);
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<List<PostedClaimSummaryBO>> GetPostedClaimsSummaryAsync()
        {
            var list = new List<PostedClaimSummaryBO>();
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string sql = @"
            SELECT ClaimBatchID, SKU, Description, MAX(ClaimDate) as ClaimDate, COUNT(*) as UnitCount, SUM(ClaimAmount) as TotalClaimAmount
            FROM tblPriceProtection
            GROUP BY ClaimBatchID, SKU, Description
            ORDER BY ClaimBatchID DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PostedClaimSummaryBO
                {
                    ClaimBatchID = Convert.ToInt32(reader["ClaimBatchID"]),
                    SKU = reader["SKU"].ToString(),
                    Description = reader["Description"].ToString(),
                    ClaimDate = reader["ClaimDate"] != DBNull.Value ? Convert.ToDateTime(reader["ClaimDate"]) : (DateTime?)null,
                    UnitCount = Convert.ToInt32(reader["UnitCount"]),
                    TotalClaimAmount = Convert.ToDecimal(reader["TotalClaimAmount"])
                });
            }
            return list;
        }

        public async Task<byte[]> GetRawClaimDataExcelAsync(DateTime start, DateTime end)
        {
            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            string sql = @"
            WITH qryRogersInvoiceNet AS (
                SELECT 
                    BVReceiptNo, 
                    SUM(CASE WHEN TransType = 'C' THEN PerUnitAmount ELSE 0.0 END) AS UnitCredit,
                    SUM(CASE WHEN TransType IN ('I', 'D') THEN PerUnitAmount ELSE 0.0 END) AS UnitDebit,
                    SUM(PerUnitAmount) AS NetUnitCost,
                    MAX(CASE WHEN TransType IN ('I', 'D') THEN RefNo ELSE '' END) AS LastInvoice,
                    MAX(CASE WHEN TransType IN ('I', 'D') THEN TransDate ELSE NULL END) AS LastInvoiceDate,
                    MAX(CASE WHEN TransType = 'C' THEN RefNo ELSE '' END) AS LastCredit,
                    MAX(CASE WHEN TransType = 'C' THEN TransDate ELSE NULL END) AS LastCreditDate
                FROM tblRogersInvoice
                GROUP BY BVReceiptNo
            )
            SELECT 
                p.ID, 
                p.ReceiptNo, 
                p.ReceiptDate, 
                p.ReceiptCost, 
                p.PriceDropDate, 
                p.PriceBeforeDrop, 
                p.PriceAfterDrop, 
                p.SKU, 
                p.Description, 
                p.IMEI, 
                p.ClaimAmount, 
                p.PreviousClaim, 
                p.PONumber, 
                p.ClaimDate, 
                p.ClaimBatchID, 
                p.ClaimAmountPaid, 
                p.Flag,
                p.Memo,
                c.UnitCreditAmount AS ClaimPaid, 
                r.UnitCredit, 
                r.UnitDebit, 
                r.NetUnitCost, 
                r.LastInvoice, 
                r.LastInvoiceDate, 
                r.LastCredit, 
                r.LastCreditDate
            FROM tblPriceProtection p
            LEFT JOIN tblPPCredits c ON p.ID = c.PPClaimID
            LEFT JOIN qryRogersInvoiceNet r ON p.ReceiptNo = r.BVReceiptNo
            WHERE p.PriceDropDate BETWEEN @Start AND @End
            ORDER BY p.ClaimBatchID DESC, p.SKU, p.IMEI";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var adapter = new SqlDataAdapter(cmd);
            using var table = new DataTable();
            adapter.Fill(table);

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("PriceProtectionClaims");
                ws.Cells["A1"].LoadFromDataTable(table, true);

                // Formatting headers
                using (var range = ws.Cells[1, 1, 1, table.Columns.Count])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Enable Excel Auto-Filter up/down arrows
                if (table.Rows.Count > 0)
                {
                    ws.Cells[1, 1, table.Rows.Count + 1, table.Columns.Count].AutoFilter = true;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
        }

        #endregion

        #region Helper Methods

        private async Task<decimal> GetPreviousClaimAmountAsync(string imei, string receiptNo, SqlConnection conn)
        {
            string sql = @"
            SELECT COALESCE(SUM(ClaimAmount), 0) 
            FROM tblPriceProtection 
            WHERE IMEI = @Imei AND ReceiptNo = @ReceiptNo";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Imei", imei);
            cmd.Parameters.AddWithValue("@ReceiptNo", receiptNo);
            var val = await cmd.ExecuteScalarAsync();
            return val != null ? Convert.ToDecimal(val) : 0;
        }

        private async Task<decimal> GetPreviousClaimPaidAsync(string imei, string receiptNo, SqlConnection conn)
        {
            string sql = @"
            SELECT COALESCE(SUM(UnitCreditAmount), 0) 
            FROM tblPPCredits 
            WHERE IMEI = @Imei AND ReceiptNo = @ReceiptNo";

            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@Imei", imei);
            cmd.Parameters.AddWithValue("@ReceiptNo", receiptNo);
            var val = await cmd.ExecuteScalarAsync();
            return val != null ? Convert.ToDecimal(val) : 0;
        }

        private string SafeSub(string? val, int maxLen)
        {
            if (val == null) return "";
            return val.Length > maxLen ? val.Substring(0, maxLen) : val;
        }

        #endregion
    }

    internal class OnhandSerialTemp
    {
        public string? Warehouse { get; set; }
        public string? PartNo { get; set; }
        public string? Number { get; set; }
        public DateTime? LatestPOReceiptDate { get; set; }
        public string? LatestPOReceiptNo { get; set; }
        public decimal LatestPOReceiptCost { get; set; }
        public string? LatestPONumber { get; set; }
    }
}
