
using DAL.Inventory.OutputInvoice;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace DAL.Sales.ARCollections
{
    public class ARCollectionsDA : IARCollectionsDA
    {
        private readonly string _pgConn;
        private readonly AppDBContext _dbContext;
        private static bool _triggersFixed = false;
        private static readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

        public ARCollectionsDA(IConfiguration config, AppDBContext dbContext)
        {
            _pgConn = config.GetConnectionString("spire_Connection") ?? "";
            _dbContext = dbContext;
        }

        private async Task EnsureTriggersFixedAsync()
        {
            if (_triggersFixed) return;
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'T_tblEventTrans_ITrig')
                    BEGIN
                        EXEC('
                        ALTER TRIGGER T_tblEventTrans_ITrig ON [tblEventTrans] FOR INSERT AS  
                        BEGIN
                            SET NOCOUNT ON;
                            IF (SELECT COUNT(*) FROM inserted) !=  
                               (SELECT COUNT(*) FROM tblEvents, inserted WHERE (tblEvents.ID = inserted.EventID))  
                            BEGIN  
                                RAISERROR (''The record can''''t be added or changed. Referential integrity rules require a related record in table ''''tblEvents''''.'', 16, 1);  
                                ROLLBACK TRANSACTION;  
                            END  
                        END
                        ');
                    END
                ");

                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'T_tblEventTrans_UTrig')
                    BEGIN
                        EXEC('
                        ALTER TRIGGER T_tblEventTrans_UTrig ON [tblEventTrans] FOR UPDATE AS  
                        BEGIN
                            SET NOCOUNT ON;
                            IF UPDATE(EventID)  
                            BEGIN  
                                IF (SELECT COUNT(*) FROM inserted) !=  
                                   (SELECT COUNT(*) FROM tblEvents, inserted WHERE (tblEvents.ID = inserted.EventID))  
                                BEGIN  
                                    RAISERROR (''The record can''''t be added or changed. Referential integrity rules require a related record in table ''''tblEvents''''.'', 16, 1);  
                                    ROLLBACK TRANSACTION;  
                                END  
                            END  
                        END
                        ');
                    END
                ");
                _triggersFixed = true;
            }
            catch (Exception)
            {
                // Ignore errors if the current user doesn't have permissions to ALTER trigger.
            }
        }

        public async Task<List<TerritoryGroup>> GetTerritoryGroupsAsync()
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await _dbContext.tblTerritoryGroups
                .OrderBy(g => g.SortOrder)
                .Select(g => new TerritoryGroup
                {
                    ID = g.ID,
                    GroupName = g.GroupName,
                    GroupCriteria = g.GroupCriteria,
                    SortOrder = g.SortOrder,
                    Phone1 = g.Phone1,
                    Phone2 = g.Phone2,
                    RogersReporting = g.RogersReporting,
                    RogersReportingName = g.RogersReportingName
                })
                .ToListAsync();
        }

        private async Task UpdateCustomerCacheAsync(DateTime agingDate, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            // 1. Delete existing cached customers for this user session
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblCustomersOpen WHERE UserId = {0}", userId);

            // 2. Fetch open balances from Postgres
            var pgCustomers = new List<ARCustomerRow>();
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                // Select open customers
                string sql = @"
                    WITH open_custs AS (
                        SELECT DISTINCT cust_no
                        FROM ar_transaction_balances_at_date(@agingDate::date)
                        WHERE balance <> 0
                    ),
                    contacts_agg AS (
                        SELECT 
                            address_id,
                            MAX(CASE WHEN contact_type_id = 0 OR contact_type_id IS NULL THEN name END) AS main_name,
                            MAX(CASE WHEN contact_type_id = 0 OR contact_type_id IS NULL THEN phone END) AS main_phone,
                            MAX(CASE WHEN contact_type_id = 0 OR contact_type_id IS NULL THEN email END) AS main_email,
                            MAX(CASE WHEN contact_type_id = 2 THEN name END) AS contact1_name,
                            MAX(CASE WHEN contact_type_id = 2 THEN phone END) AS contact1_phone,
                            MAX(CASE WHEN contact_type_id = 2 THEN email END) AS contact1_email,
                            MAX(CASE WHEN contact_type_id = 5 THEN name END) AS contact2_name,
                            MAX(CASE WHEN contact_type_id = 5 THEN phone END) AS contact2_phone,
                            MAX(CASE WHEN contact_type_id = 5 THEN email END) AS contact2_email,
                            MAX(CASE WHEN contact_type_id = 9 THEN name END) AS contact3_name,
                            MAX(CASE WHEN contact_type_id = 9 THEN phone END) AS contact3_phone,
                            MAX(CASE WHEN contact_type_id = 9 THEN email END) AS contact3_email
                        FROM address_contacts
                        WHERE contact_type_id IN (0, 2, 5, 9) OR contact_type_id IS NULL
                        GROUP BY address_id
                    )
                    SELECT 
                        c.cust_no, 
                        c.name, 
                        a.sales_terr, 
                        a.postal_zip,
                        COALESCE(con.main_phone, '') AS main_phone,
                        COALESCE(con.main_email, '') AS main_email,
                        con.contact1_name, con.contact1_phone, con.contact1_email,
                        con.contact2_name, con.contact2_phone, con.contact2_email,
                        con.contact3_name, con.contact3_phone, con.contact3_email,
                        CASE WHEN c.spec_handling = 'F' THEN 'French' ELSE 'English' END AS language,
                        a.id AS address_id
                    FROM open_custs oc
                    INNER JOIN customers c ON oc.cust_no = c.cust_no
                    INNER JOIN addresses a ON c.cust_no = a.link_no AND a.link_table = 'CUST' AND a.addr_type = 'B'
                    LEFT JOIN contacts_agg con ON a.id = con.address_id";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("agingDate", NpgsqlTypes.NpgsqlDbType.Date, agingDate.Date);
                    cmd.CommandTimeout = 600;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            pgCustomers.Add(new ARCustomerRow
                            {
                                CUST = reader.GetString(0),
                                CustName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                SALES_TERR = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PostalCode = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                BVADDRTELNO1 = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                BVADDREMAIL = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                BVCOCONTACT1NAME = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                BVCOCONTACT1TEL1 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                BVCOCONTACT1EMAIL = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                BVCOCONTACT2NAME = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                BVCOCONTACT2TEL1 = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                BVCOCONTACT2EMAIL = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                BVCOCONTACT3NAME = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                BVCOCONTACT3TEL1 = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                BVCOCONTACT3EMAIL = reader.IsDBNull(14) ? "" : reader.GetString(14),
                                Language = reader.IsDBNull(15) ? "English" : reader.GetString(15),
                                AddressID = reader.IsDBNull(16) ? (int?)null : reader.GetInt32(16)
                            });
                        }
                    }
                }
            }

            // Save to SQL Server tblCustomersOpen
            var sqlRecords = pgCustomers.Select(c => new TblCustomersOpen
            {
                CUST = c.CUST,
                CustName = c.CustName,
                SALES_TERR = c.SALES_TERR,
                PostalCode = c.PostalCode,
                BVADDRTELNO1 = c.BVADDRTELNO1,
                BVADDREMAIL = c.BVADDREMAIL,
                BVCOCONTACT1NAME = c.BVCOCONTACT1NAME,
                BVCOCONTACT1TEL1 = c.BVCOCONTACT1TEL1,
                BVCOCONTACT1EMAIL = c.BVCOCONTACT1EMAIL,
                BVCOCONTACT2NAME = c.BVCOCONTACT2NAME,
                BVCOCONTACT2TEL1 = c.BVCOCONTACT2TEL1,
                BVCOCONTACT2EMAIL = c.BVCOCONTACT2EMAIL,
                BVCOCONTACT3NAME = c.BVCOCONTACT3NAME,
                BVCOCONTACT3TEL1 = c.BVCOCONTACT3TEL1,
                BVCOCONTACT3EMAIL = c.BVCOCONTACT3EMAIL,
                Language = c.Language,
                AddressID = c.AddressID,
                UserId = userId,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            }).ToList();

            await _dbContext.tblCustomersOpen.AddRangeAsync(sqlRecords);
            await _dbContext.SaveChangesAsync();

            // Update group associations based on tblCustomerGroups (VBA Parity)
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                UPDATE tblCustomersOpen
                SET CustGroup = g.CustGroup
                FROM tblCustomerGroups g
                WHERE tblCustomersOpen.CUST = g.BVCustNo AND tblCustomersOpen.UserId = {0}", userId);

            // Update GroupAndSingle flag based on tblARDetailExtra BillToCust settings
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                UPDATE tblCustomersOpen
                SET GroupAndSingle = 1
                WHERE CUST IN (SELECT BillToCust FROM tblARDetailExtra WHERE IgnoreGroup = 1) AND UserId = {0}", userId);
        }

        public async Task<List<ARCustomerRow>> LoadOpenCustomersAsync(int selectBy, string groupCriteria, DateTime agingDate, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            // Refresh the cache first
            await UpdateCustomerCacheAsync(agingDate, userId);

            // Handle empty/Other criteria
            if (string.IsNullOrEmpty(groupCriteria))
            {
                var otherCriteriaList = await _dbContext.tblTerritoryGroups
                    .Where(g => g.GroupName != "Other" && !string.IsNullOrEmpty(g.GroupCriteria))
                    .Select(g => g.GroupCriteria)
                    .ToListAsync();
                if (otherCriteriaList.Any())
                {
                    groupCriteria = "NOT (" + string.Join(" OR ", otherCriteriaList.Select(c => $"({c})")) + ")";
                }
                else
                {
                    groupCriteria = "1=1";
                }
            }

            if (selectBy == 1) // Single Customer mode
            {
                // SQL Server raw query to respect complex dynamic groupCriteria strings (VBA Parity)
                string sql = $@"
                    SELECT o.*, CAST(CASE WHEN b.CustNo IS NOT NULL THEN 1 ELSE 0 END AS BIT) as SendBulk
                    FROM tblCustomersOpen o
                    LEFT JOIN tblBulkCustomers b ON o.CUST = b.CustNo
                    WHERE o.UserId = {{0}} AND (o.CustGroup IS NULL OR o.GroupAndSingle = 1) AND ({groupCriteria})
                    ORDER BY o.CustName";

                var queryResult = await _dbContext.tblCustomersOpen
                    .FromSqlRaw(sql, userId)
                    .ToListAsync();

                // Map to BO rows
                return queryResult.Select(r => new ARCustomerRow
                {
                    CUST = r.CUST,
                    CustName = r.CustName,
                    CustGroup = r.CustGroup,
                    GroupAndSingle = r.GroupAndSingle,
                    SALES_TERR = r.SALES_TERR,
                    PostalCode = r.PostalCode,
                    BVADDRTELNO1 = r.BVADDRTELNO1,
                    BVADDREMAIL = r.BVADDREMAIL,
                    BVCOCONTACT1NAME = r.BVCOCONTACT1NAME,
                    BVCOCONTACT1TEL1 = r.BVCOCONTACT1TEL1,
                    BVCOCONTACT1EMAIL = r.BVCOCONTACT1EMAIL,
                    BVCOCONTACT2NAME = r.BVCOCONTACT2NAME,
                    BVCOCONTACT2TEL1 = r.BVCOCONTACT2TEL1,
                    BVCOCONTACT2EMAIL = r.BVCOCONTACT2EMAIL,
                    BVCOCONTACT3NAME = r.BVCOCONTACT3NAME,
                    BVCOCONTACT3TEL1 = r.BVCOCONTACT3TEL1,
                    BVCOCONTACT3EMAIL = r.BVCOCONTACT3EMAIL,
                    Language = r.Language,
                    AddressID = r.AddressID,
                    SendBulk = _dbContext.tblBulkCustomers.Any(b => b.CustNo == r.CUST)
                }).ToList();
            }
            else // Group mode
            {
                // Fetch group summaries aggregating customer rows (VBA Parity)
                string sql = $@"
                    SELECT 
                        MIN(o.Id) as Id,
                        o.CustGroup as CUST,
                        MAX(g.GroupName) as CustName,
                        o.CustGroup as CustGroup,
                        CAST(0 AS BIT) as GroupAndSingle,
                        MAX(o.SALES_TERR) as SALES_TERR,
                        MAX(o.PostalCode) as PostalCode,
                        MAX(o.BVADDRTELNO1) as BVADDRTELNO1,
                        MAX(o.BVADDREMAIL) as BVADDREMAIL,
                        MAX(o.BVCOCONTACT1NAME) as BVCOCONTACT1NAME,
                        MAX(o.BVCOCONTACT1TEL1) as BVCOCONTACT1TEL1,
                        MAX(o.BVCOCONTACT1EMAIL) as BVCOCONTACT1EMAIL,
                        MAX(o.BVCOCONTACT2NAME) as BVCOCONTACT2NAME,
                        MAX(o.BVCOCONTACT2TEL1) as BVCOCONTACT2TEL1,
                        MAX(o.BVCOCONTACT2EMAIL) as BVCOCONTACT2EMAIL,
                        MAX(o.BVCOCONTACT3NAME) as BVCOCONTACT3NAME,
                        MAX(o.BVCOCONTACT3TEL1) as BVCOCONTACT3TEL1,
                        MAX(o.BVCOCONTACT3EMAIL) as BVCOCONTACT3EMAIL,
                        MAX(o.Language) as Language,
                        MAX(o.ChannelID) as ChannelID,
                        MAX(o.AddressID) as AddressID,
                        o.UserId,
                        MAX(o.CreatedBy) as CreatedBy,
                        MAX(o.CreatedDate) as CreatedDate,
                        MAX(o.ModifiedBy) as ModifiedBy,
                        MAX(o.ModifiedDate) as ModifiedDate
                    FROM tblCustomersOpen o
                    INNER JOIN tblCustomerGroups g ON o.CustGroup = g.CustGroup
                    WHERE o.UserId = {{0}} AND ({groupCriteria})
                    GROUP BY o.CustGroup, o.UserId
                    HAVING o.CustGroup IS NOT NULL AND o.CustGroup <> ''
                    ORDER BY o.CustGroup";

                var queryResult = await _dbContext.tblCustomersOpen
                    .FromSqlRaw(sql, userId)
                    .ToListAsync();

                return queryResult.Select(r => new ARCustomerRow
                {
                    CUST = r.CUST,
                    CustName = r.CustName,
                    CustGroup = r.CustGroup ?? r.CUST,
                    GroupAndSingle = false,
                    SALES_TERR = r.SALES_TERR,
                    PostalCode = r.PostalCode,
                    BVADDRTELNO1 = r.BVADDRTELNO1,
                    BVADDREMAIL = r.BVADDREMAIL,
                    BVCOCONTACT1NAME = r.BVCOCONTACT1NAME,
                    BVCOCONTACT1TEL1 = r.BVCOCONTACT1TEL1,
                    BVCOCONTACT1EMAIL = r.BVCOCONTACT1EMAIL,
                    BVCOCONTACT2NAME = r.BVCOCONTACT2NAME,
                    BVCOCONTACT2TEL1 = r.BVCOCONTACT2TEL1,
                    BVCOCONTACT2EMAIL = r.BVCOCONTACT2EMAIL,
                    BVCOCONTACT3NAME = r.BVCOCONTACT3NAME,
                    BVCOCONTACT3TEL1 = r.BVCOCONTACT3TEL1,
                    BVCOCONTACT3EMAIL = r.BVCOCONTACT3EMAIL,
                    Language = r.Language,
                    AddressID = r.AddressID,
                    SendBulk = _dbContext.tblBulkCustomers.Any(b => b.CustNo == r.CUST)
                }).ToList();
            }
        }

        public async Task<List<ARTransactionRow>> RefreshARGridAsync(string custNo, int selectBy, string groupCriteria, DateTime agingDate, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            // 1. Clear session grid cache
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM ARDetailView WHERE UserId = {0}", userId);

            // 2. Resolve target customer accounts
            var customerNos = await _dbContext.tblCustomersOpen
                .Where(c => c.UserId == userId && (selectBy == 1 ? c.CUST == custNo : c.CustGroup == custNo))
                .Select(c => c.CUST)
                .ToListAsync();

            if (!customerNos.Any()) return new List<ARTransactionRow>();

            var openTransactions = new List<TblARDetailView>();

            // 3. Fetch open items from Postgres
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT cust_no, code, trans_no, ref_no, date, debit_amt, credit_amt, balance, id
                    FROM ar_transactions
                    WHERE cust_no = ANY(@custNos) AND open_close_flag = 'O'";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("custNos", customerNos);
                    cmd.CommandTimeout = 600;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tCust = reader.GetString(0);
                            var tCode = reader.GetString(1);
                            var tTrans = reader.GetString(2);
                            var tRef = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            var tDate = reader.GetDateTime(4);
                            var dAmt = reader.GetDecimal(5);
                            var cAmt = reader.GetDecimal(6);
                            var bal = reader.GetDecimal(7);
                            var pgId = reader.GetInt32(8);

                            var grp = await _dbContext.tblCustomersOpen
                                .Where(c => c.UserId == userId && c.CUST == tCust)
                                .Select(c => c.CustGroup)
                                .FirstOrDefaultAsync();

                            openTransactions.Add(new TblARDetailView
                            {
                                CustGroup = grp,
                                CUST = tCust,
                                FOLIO = tTrans,
                                TopItem = "*",
                                Type = tCode,
                                TRANS_NO = tTrans,
                                REF_NO = tRef,
                                TranDate = tDate,
                                D_AMOUNT = dAmt,
                                C_AMOUNT = cAmt,
                                BALANCE = dAmt > 0 ? dAmt : cAmt * -1, // VBA Balance formula parity
                                DaysOld = (agingDate.Date - tDate.Date).Days,
                                ARID = pgId,
                                UserId = userId,
                                CreatedBy = userId,
                                CreatedDate = DateTime.Now
                            });
                        }
                    }
                }
            }

            if (openTransactions.Any())
            {
                await _dbContext.ARDetailView.AddRangeAsync(openTransactions);
                await _dbContext.SaveChangesAsync();

                // 4. Fetch linked payments/credits from Postgres (VBA qryAppendARDetailViewLinked parity)
                var debitIds = openTransactions.Select(t => t.ARID ?? 0).Where(id => id > 0).Distinct().ToList();
                var linkedTransactions = new List<TblARDetailView>();

                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();

                    string linkSql = @"
                        SELECT l.debit_id, l.credit_id, SUM(l.applied_amt) as sum_applied,
                               c.cust_no, c.code, c.trans_no, c.ref_no, c.date
                        FROM ar_transaction_links l
                        INNER JOIN ar_transactions c ON l.credit_id = c.id
                        WHERE l.debit_id = ANY(@debitIds)
                        GROUP BY l.debit_id, l.credit_id, c.cust_no, c.code, c.trans_no, c.ref_no, c.date
                        HAVING SUM(l.applied_amt) <> 0";

                    using (var cmd = new NpgsqlCommand(linkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("debitIds", debitIds);
                        cmd.CommandTimeout = 600;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var debitId = reader.GetInt32(0);
                                var creditId = reader.GetInt32(1);
                                var appliedAmt = reader.GetDecimal(2);
                                var credCust = reader.GetString(3);
                                var credCode = reader.GetString(4);
                                var credTrans = reader.GetString(5);
                                var credRef = reader.IsDBNull(6) ? "" : reader.GetString(6);
                                var credDate = reader.GetDateTime(7);

                                // Find matching parent details from open transactions
                                var parent = openTransactions.First(p => p.ARID == debitId);

                                linkedTransactions.Add(new TblARDetailView
                                {
                                    CustGroup = parent.CustGroup,
                                    CUST = parent.CUST,
                                    FOLIO = parent.TRANS_NO, // Insert parent invoice number under FOLIO (VBA parity)
                                    TopItem = "",
                                    Type = credCode,
                                    TRANS_NO = credTrans,
                                    REF_NO = credRef,
                                    TranDate = credDate,
                                    D_AMOUNT = appliedAmt > 0 ? appliedAmt * -1 : 0,
                                    C_AMOUNT = appliedAmt > 0 ? appliedAmt : 0,
                                    BALANCE = appliedAmt * -1,
                                    DaysOld = (agingDate.Date - credDate.Date).Days,
                                    ARID = creditId,
                                    UserId = userId,
                                    CreatedBy = userId,
                                    CreatedDate = DateTime.Now
                                });
                            }
                        }
                    }
                }

                if (linkedTransactions.Any())
                {
                    await _dbContext.ARDetailView.AddRangeAsync(linkedTransactions);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // 5. Apply Rogers RBC new flow filter logic (VBA parity)
            if (custNo == "806334" || customerNos.Contains("806334"))
            {
                var actTransIds = await _dbContext.SalesActivations
                    .Where(sa => sa.POLine != null && sa.POLine.StartsWith("POL"))
                    .Select(sa => sa.TransactionNo)
                    .Where(t => t != null)
                    .ToListAsync();

                var rowsToDelete1 = await _dbContext.ARDetailView
                    .Where(a => a.UserId == userId && actTransIds.Contains(a.TRANS_NO))
                    .ToListAsync();
                _dbContext.ARDetailView.RemoveRange(rowsToDelete1);

                var actInvoiceNos = await _dbContext.SalesActivationsDetail
                    .Where(sad => sad.POLine != null && sad.POLine.StartsWith("POL"))
                    .Select(sad => sad.Invoice)
                    .Where(i => i != null)
                    .ToListAsync();

                var rowsToDelete2 = await _dbContext.ARDetailView
                    .Where(a => a.UserId == userId && actInvoiceNos.Contains(a.REF_NO))
                    .ToListAsync();
                _dbContext.ARDetailView.RemoveRange(rowsToDelete2);

                await _dbContext.SaveChangesAsync();
            }

            // 6. Delete ignore group transactions based on settings (VBA parity)
            if (selectBy == 2)
            {
                var ignoreTrans = await _dbContext.tblARDetailExtra
                    .Where(e => e.IgnoreGroup)
                    .Select(e => e.TransNo)
                    .ToListAsync();

                var rowsToRemove = await _dbContext.ARDetailView
                    .Where(d => d.UserId == userId && ignoreTrans.Contains(d.TRANS_NO))
                    .ToListAsync();
                _dbContext.ARDetailView.RemoveRange(rowsToRemove);
                await _dbContext.SaveChangesAsync();
            }
            else // Single mode
            {
                var rowsToRemove = await (from o in _dbContext.tblCustomersOpen
                                          join d in _dbContext.ARDetailView on o.CUST equals d.CUST
                                          join e in _dbContext.tblARDetailExtra on d.TRANS_NO equals e.TransNo
                                          where o.UserId == userId && d.UserId == userId && o.CustGroup != null && o.CustGroup != "" && !e.IgnoreGroup
                                          select d).ToListAsync();
                _dbContext.ARDetailView.RemoveRange(rowsToRemove);
                await _dbContext.SaveChangesAsync();
            }

            // 7. Ensure ARDetailExtra placeholders exist for all loaded transactions
            var existingExtras = await _dbContext.tblARDetailExtra.Select(e => e.TransNo).ToListAsync();
            var newTrans = await _dbContext.ARDetailView
                .Where(d => d.UserId == userId && !_dbContext.tblARDetailExtra.Any(e => e.TransNo == d.TRANS_NO))
                .Select(d => d.TRANS_NO)
                .Distinct()
                .ToListAsync();

            if (newTrans.Any())
            {
                var newExtras = newTrans.Select(t => new TblARDetailExtra
                {
                    TransNo = t,
                    OPCResolved = false,
                    IgnoreGroup = false,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                }).ToList();

                await _dbContext.tblARDetailExtra.AddRangeAsync(newExtras);
                await _dbContext.SaveChangesAsync();
            }

            // 8. Fetch BulkID from Postgres sales_history (VBA parity)
            var uncheckedTrans = await (from d in _dbContext.ARDetailView
                                        join e in _dbContext.tblARDetailExtra on d.TRANS_NO equals e.TransNo
                                        where d.UserId == userId && !e.BulkIDChecked
                                        select e.TransNo)
                                        .Distinct()
                                        .ToListAsync();

            if (uncheckedTrans.Any())
            {
                var fobs = new Dictionary<string, string>();
                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT invoice_no, fob FROM sales_history WHERE invoice_no = ANY(@transNos)", conn))
                    {
                        cmd.Parameters.AddWithValue("transNos", uncheckedTrans);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var inv = reader.GetString(0);
                                var fob = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                fobs[inv] = fob;
                            }
                        }
                    }
                }

                var extrasToUpdate = await _dbContext.tblARDetailExtra
                    .Where(e => uncheckedTrans.Contains(e.TransNo))
                    .ToListAsync();

                foreach (var extra in extrasToUpdate)
                {
                    if (fobs.TryGetValue(extra.TransNo, out var fob))
                    {
                        extra.BulkID = (!string.IsNullOrEmpty(fob) && fob.Trim() != "Your Dock") ? fob.Trim() : null;
                    }
                    extra.BulkIDChecked = true;
                    extra.ModifiedBy = userId;
                    extra.ModifiedDate = DateTime.Now;
                }
                await _dbContext.SaveChangesAsync();
            }

            // 9. Cache Activations details (VBA parity)
            var cachedInvoices = await _dbContext.tblActivationsLookup
                .Where(a => a.UserId == userId)
                .Select(a => a.Invoice)
                .ToListAsync();

            var requiredInvoices = await _dbContext.ARDetailView
                .Where(d => d.UserId == userId && (d.Type == "I" || d.Type == "C") && !cachedInvoices.Contains(d.REF_NO))
                .Select(d => d.REF_NO)
                .Distinct()
                .Where(r => r != null)
                .ToListAsync();

            if (requiredInvoices.Any())
            {
                var activationsData = await _dbContext.SalesActivations
                    .Where(sa => requiredInvoices.Contains(sa.Invoice))
                    .GroupBy(sa => new { sa.Invoice, sa.InvoiceDate })
                    .Select(g => new TblActivationsLookup
                    {
                        Invoice = g.Key.Invoice,
                        InvoiceDate = g.Key.InvoiceDate,
                        MaxOfID = g.Max(sa => sa.Id),
                        Customer = g.Max(sa => sa.Customer),
                        ActivationsTerritory = g.Max(sa => sa.CustTerritory),
                        MSD = g.Max(sa => sa.MSD),
                        WebOrderID = g.Max(sa => sa.WebOrderID),
                        CustomerPostal = g.Max(sa => sa.CustomerPostal),
                        ShipToPostal = g.Max(sa => sa.ShipToPostal),
                        CostBudgetCode = g.Max(sa => sa.CostBudgetCode),
                        CustomerPONo = g.Max(sa => sa.CustomerPONo),
                        UserName = g.Max(sa => sa.UserName),
                        CellPhoneNo = g.Max(sa => sa.CellPhoneNo),
                        CountGovChannel = g.Sum(sa => sa.Channel == "Government" ? 1m : 0m),
                        CountGovFee = g.Sum(sa => (sa.FeeType != null && sa.FeeType.Contains("GOV")) ? 1m : 0m),
                        UserId = userId,
                        CreatedBy = userId,
                        CreatedDate = DateTime.Now
                    })
                    .ToListAsync();

                if (activationsData.Any())
                {
                    await _dbContext.tblActivationsLookup.AddRangeAsync(activationsData);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // 10. Assemble continuous grid view joining cache, metadata and activations
            var gridQuery = from d in _dbContext.ARDetailView
                            join e in _dbContext.tblARDetailExtra on d.TRANS_NO equals e.TransNo
                            join aOpt in _dbContext.tblActivationsLookup.Where(al => al.UserId == userId) on d.REF_NO equals aOpt.Invoice into aJoin
                            from al in aJoin.DefaultIfEmpty()
                            where d.UserId == userId
                            orderby d.TranDate descending, d.TRANS_NO
                            select new ARTransactionRow
                            {
                                Id = d.Id,
                                Checked = d.Checked,
                                CUST = d.CUST,
                                FOLIO = d.FOLIO,
                                TopItem = d.TopItem,
                                Type = d.Type,
                                TRANS_NO = d.TRANS_NO,
                                REF_NO = d.REF_NO,
                                TranDate = d.TranDate,
                                D_AMOUNT = d.D_AMOUNT,
                                C_AMOUNT = d.C_AMOUNT,
                                BALANCE = d.BALANCE,
                                DaysOld = d.DaysOld,
                                Amount = d.D_AMOUNT != 0 ? d.D_AMOUNT : d.C_AMOUNT * -1,

                                // Activations
                                ActivationsTerritory = al != null ? al.ActivationsTerritory : null,
                                MSD = al != null ? al.MSD : null,
                                WebOrderID = al != null ? al.WebOrderID : null,
                                CostBudgetCode = al != null ? al.CostBudgetCode : null,
                                CustomerPONo = al != null ? al.CustomerPONo : null,
                                UserName = al != null ? al.UserName : null,
                                CellPhoneNo = al != null ? al.CellPhoneNo : null,
                                CountGovChannel = al != null ? al.CountGovChannel : null,
                                CountGovFee = al != null ? al.CountGovFee : null,

                                // Metadata
                                BAN = e.BAN,
                                FirstNoticeDate = e.FirstNoticeDate,
                                FirstNoticeBalance = e.FirstNoticeBalance,
                                SecondNoticeDate = e.SecondNoticeDate,
                                SecondNoticeBalance = e.SecondNoticeBalance,
                                RootCauseID = e.RootCauseID,
                                OPCResolved = e.OPCResolved,
                                OPCDescription = e.OPCDescription,
                                BulkID = e.BulkID,
                                IgnoreGroup = e.IgnoreGroup,
                                BillToCust = e.BillToCust
                            };

            var finalGrid = await gridQuery.ToListAsync();

            // Populate RootCauseDescription in memory
            var causes = await _dbContext.tblRootCauses.ToDictionaryAsync(r => r.Code, r => r.Description);
            foreach (var row in finalGrid)
            {
                if (row.RootCauseID.HasValue && causes.TryGetValue(row.RootCauseID.Value, out var desc))
                {
                    row.RootCauseDescription = desc;
                }

                // Set point-in-time aging buckets (VBA Parity)
                row.Current = (row.DaysOld < 30) ? row.BALANCE : 0m;
                row.ThirtyDays = (row.DaysOld >= 30 && row.DaysOld < 60) ? row.BALANCE : 0m;
                row.SixtyDays = (row.DaysOld >= 60 && row.DaysOld < 90) ? row.BALANCE : 0m;
                row.NinetyDays = (row.DaysOld >= 90 && row.DaysOld < 120) ? row.BALANCE : 0m;
                row.OneTwentyPlusDays = (row.DaysOld >= 120) ? row.BALANCE : 0m;
            }

            return finalGrid;
        }

        public async Task<bool> UpdateARDetailRowAsync(UpdateARDetailRequest request, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var extra = await _dbContext.tblARDetailExtra
                .FirstOrDefaultAsync(e => e.TransNo == request.TransNo);

            if (extra == null)
            {
                extra = new TblARDetailExtra
                {
                    TransNo = request.TransNo,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                };
                await _dbContext.tblARDetailExtra.AddAsync(extra);
            }

            extra.BAN = request.BAN;
            extra.RootCauseID = (byte?)request.RootCauseID;
            extra.OPCResolved = request.OPCResolved;
            extra.OPCDescription = request.OPCDescription;
            extra.IgnoreGroup = request.IgnoreGroup;
            extra.BillToCust = request.BillToCust;
            extra.ModifiedBy = userId;
            extra.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<ARCommentEvent>> GetEventsAsync(string custNo, int selectBy)
        {
            _dbContext.Database.SetCommandTimeout(600);

            // Respect complex VBA query logic filter (qryEventsDetailView parity)
            var eventQuery = from e in _dbContext.tblEvents
                             join t in _dbContext.tblEventTrans on e.ID equals t.EventID into tJoin
                             from et in tJoin.DefaultIfEmpty()
                             join ty in _dbContext.tblEventTypes on e.EventType equals ty.EventType
                             where e.CustNo == custNo && e.CustType == (selectBy == 1 ? "Single" : "Group")
                             orderby e.AddDate descending
                             select new ARCommentEvent
                             {
                                 ID = e.ID,
                                 EventType = e.EventType,
                                 EventDescription = ty.EventDescription,
                                 CustNo = e.CustNo,
                                 CustType = e.CustType,
                                 EventText = e.EventText,
                                 EventAmount = (decimal?)e.EventAmount,
                                 CommentKey = e.CommentKey,
                                 AddDate = e.AddDate,
                                 AddUser = e.AddUser,
                                 ModDate = e.ModDate,
                                 ModUser = e.ModUser,
                                 TransNo = et != null ? et.TransNo : null,
                                 EventTransID = et != null ? (int?)et.ID : null
                             };

            return await eventQuery.ToListAsync();
        }

        public async Task<int> AddCommentAsync(AddCommentRequest request, string initials, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);
            await EnsureTriggersFixedAsync();

            // Diagnostics to inspect database schema of tblEvents and tblEventTrans
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== DB Diagnostics Started at {DateTime.Now} ===");

                var conn = _dbContext.Database.GetDbConnection();
                var wasOpen = conn.State == System.Data.ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    // Check tblEvents identity
                    cmd.CommandText = "SELECT name, is_identity FROM sys.identity_columns WHERE object_id = object_id('tblEvents')";
                    using (var reader = cmd.ExecuteReader())
                    {
                        sb.AppendLine("tblEvents Identity Columns:");
                        while (reader.Read())
                        {
                            sb.AppendLine($"- Column: {reader[0]} | IsIdentity: {reader[1]}");
                        }
                    }

                    // Check tblEventTrans identity
                    cmd.CommandText = "SELECT name, is_identity FROM sys.identity_columns WHERE object_id = object_id('tblEventTrans')";
                    using (var reader = cmd.ExecuteReader())
                    {
                        sb.AppendLine("tblEventTrans Identity Columns:");
                        while (reader.Read())
                        {
                            sb.AppendLine($"- Column: {reader[0]} | IsIdentity: {reader[1]}");
                        }
                    }

                    // Check triggers on both
                    cmd.CommandText = "SELECT OBJECT_NAME(parent_id) as TableName, name, is_disabled FROM sys.triggers WHERE parent_id IN (object_id('tblEvents'), object_id('tblEventTrans'))";
                    using (var reader = cmd.ExecuteReader())
                    {
                        sb.AppendLine("Triggers:");
                        while (reader.Read())
                        {
                            sb.AppendLine($"- Table: {reader[0]} | Trigger: {reader[1]} | Disabled: {reader[2]}");
                        }
                    }
                }

                if (!wasOpen) conn.Close();

                sb.AppendLine("===============================================\n");
                var path = @"C:\Logs\backend_error.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(@"c:\Users\DELL\Downloads\My Code\backend_error.txt", $"Diagnostics Failed: {ex.Message}\n"); } catch { }
            }

            var newEvent = new TblEvents
            {
                EventType = request.EventType > 0 ? request.EventType : 1,
                CustNo = request.CustNo,
                CustType = request.CustType,
                EventText = request.CommentText,
                AddDate = DateTime.Now,
                AddUser = initials,
            };

            int eventIdToUse = 0;
            try
            {
                await _dbContext.tblEvents.AddAsync(newEvent);
                await _dbContext.SaveChangesAsync();

                eventIdToUse = newEvent.ID;
                if (eventIdToUse <= 0)
                {
                    var fetchedId = await _dbContext.tblEvents
                        .Where(e => e.CustNo == newEvent.CustNo
                                 && e.AddUser == newEvent.AddUser
                                 && e.EventText == newEvent.EventText)
                        .OrderByDescending(e => e.ID)
                        .Select(e => e.ID)
                        .FirstOrDefaultAsync();

                    if (fetchedId > 0)
                    {
                        eventIdToUse = fetchedId;
                    }
                    else
                    {
                        throw new Exception("Fallback failed: Could not retrieve generated Event ID after insert. The event record might not have been created correctly.");
                    }
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== tblEvents Save Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"c:\Users\DELL\Downloads\My Code\backend_error.txt", errorText); } catch { }
                throw new Exception($"tblEvents save failed: {innerMsg}", ex);
            }

            if (request.CheckedTransNos != null && request.CheckedTransNos.Any())
            {
                var transMappings = request.CheckedTransNos.Distinct().Select(t => new TblEventTrans
                {
                    EventID = eventIdToUse,
                    TransNo = t
                }).ToList();

                try
                {
                    await _dbContext.tblEventTrans.AddRangeAsync(transMappings);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    var innerMsg = ex.InnerException?.Message ?? ex.Message;
                    var errorText = $"=== tblEventTrans Save Error ({DateTime.Now}) ===\n" +
                                    $"EventID attempting to insert: {eventIdToUse}\n" +
                                    $"Error: {ex.Message}\n" +
                                    $"Inner Exception: {innerMsg}\n" +
                                    $"Stack Trace:\n{ex.StackTrace}\n\n";
                    try { System.IO.File.AppendAllText(@"c:\Users\DELL\Downloads\My Code\backend_error.txt", errorText); } catch { }
                    throw new Exception($"tblEventTrans save failed (EventID: {eventIdToUse}): {innerMsg}", ex);
                }
            }

            return eventIdToUse;
        }
        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var ev = await _dbContext.tblEvents.FindAsync(commentId);
            if (ev == null) return false;

            _dbContext.tblEvents.Remove(ev);

            var links = await _dbContext.tblEventTrans.Where(l => l.EventID == commentId).ToListAsync();
            _dbContext.tblEventTrans.RemoveRange(links);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditCommentAsync(int commentId, string text, string initials)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var ev = await _dbContext.tblEvents.FindAsync(commentId);
            if (ev == null) return false;

            ev.EventText = text;
            ev.ModDate = DateTime.Now;
            ev.ModUser = initials;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCommentFromTransAsync(int eventTransId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var link = await _dbContext.tblEventTrans.FindAsync(eventTransId);
            if (link == null) return false;

            _dbContext.tblEventTrans.Remove(link);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckOpenPaymentsAsync(string custNo)
        {
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT EXISTS (
                        SELECT 1 
                        FROM ar_transactions 
                        WHERE cust_no = @custNo 
                          AND open_close_flag = 'O' 
                          AND (code = 'P' OR code = 'C')
                          AND balance != 0
                    )";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("custNo", custNo);
                    cmd.CommandTimeout = 600;
                    return (bool)(await cmd.ExecuteScalarAsync() ?? false);
                }
            }
        }

        public async Task<byte[]> GenerateOverdueNoticeAsync(CreateNoticeRequest request, string templatesPath, string initials, int userId)
        {
            await _dbLock.WaitAsync();
            try
            {
                _dbContext.Database.SetCommandTimeout(600);
                await EnsureTriggersFixedAsync();
            }
            finally
            {
                _dbLock.Release();
            }

            EnsureNoticeTemplatesExist(templatesPath);

            string fileName = request.Language == "French"
                ? (request.NoticeType == 1 ? "1er Avis.docx" : "2ieme Avis.docx")
                : (request.NoticeType == 1 ? "1st Notice.docx" : "2nd Notice.docx");

            string templatePath = Path.Combine(templatesPath, fileName);
            byte[] templateBytes = await File.ReadAllBytesAsync(templatePath);

            byte[] docxBytes;
            using (var ms = new MemoryStream())
            {
                await ms.WriteAsync(templateBytes, 0, templateBytes.Length);
                ms.Position = 0;

                using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, true))
                {
                    var entry = archive.GetEntry("word/document.xml");
                    if (entry != null)
                    {
                        string content;
                        using (var sr = new StreamReader(entry.Open()))
                        {
                            content = await sr.ReadToEndAsync();
                        }

                        // Search and replace placeholders
                        content = content.Replace("{NoticeDate}", DateTime.Now.ToString("yyyy-MM-dd"));
                        content = content.Replace("{AcctNo}", request.CustNo);
                        content = content.Replace("{CustName}", request.CustName);
                        content = content.Replace("{Amount}", request.Amount.ToString("N2"));

                        entry.Delete();
                        var newEntry = archive.CreateEntry("word/document.xml");
                        using (var sw = new StreamWriter(newEntry.Open()))
                        {
                            await sw.WriteAsync(content);
                        }
                    }
                }
                docxBytes = ms.ToArray();
            }

            await _dbLock.WaitAsync();
            try
            {
                // Add notice event to tblEvents
                var newEvent = new TblEvents
                {
                    EventType = request.NoticeType == 1 ? 2 : 3, // 2 for First Notice, 3 for Second Notice
                    CustNo = request.CustNo,
                    CustType = "Single",
                    EventText = $"Generated {(request.NoticeType == 1 ? "1st" : "2nd")} notice for ${request.Amount:N2}",
                    EventAmount = (double?)request.Amount,
                    AddDate = DateTime.Now,
                    AddUser = initials,
                };
                await _dbContext.tblEvents.AddAsync(newEvent);
                await _dbContext.SaveChangesAsync();

                int eventIdToUse = newEvent.ID;
                if (eventIdToUse <= 0)
                {
                    var fetchedId = await _dbContext.tblEvents
                        .Where(e => e.CustNo == newEvent.CustNo
                                 && e.AddUser == newEvent.AddUser
                                 && e.EventText == newEvent.EventText)
                        .OrderByDescending(e => e.ID)
                        .Select(e => e.ID)
                        .FirstOrDefaultAsync();

                    if (fetchedId > 0)
                    {
                        eventIdToUse = fetchedId;
                    }
                    else
                    {
                        throw new Exception("Fallback failed: Could not retrieve generated Event ID after insert. The event record might not have been created correctly.");
                    }
                }

                if (request.CheckedTransNos != null && request.CheckedTransNos.Any())
                {
                    var transMappings = request.CheckedTransNos.Select(t => new TblEventTrans
                    {
                        EventID = eventIdToUse,
                        TransNo = t,
                    }).ToList();
                    await _dbContext.tblEventTrans.AddRangeAsync(transMappings);

                    // Update Notice dates in tblARDetailExtra metadata
                    var extras = await _dbContext.tblARDetailExtra
                        .Where(e => request.CheckedTransNos.Contains(e.TransNo))
                        .ToListAsync();

                    var transNos = extras.Select(e => e.TransNo).ToList();
                    var transDetails = await _dbContext.ARDetailView
                        .Where(d => d.UserId == userId && transNos.Contains(d.TRANS_NO))
                        .ToDictionaryAsync(d => d.TRANS_NO);

                    foreach (var extra in extras)
                    {
                        decimal bal = transDetails.TryGetValue(extra.TransNo, out var transDetail) ? transDetail.BALANCE : 0m;

                        if (request.NoticeType == 1)
                        {
                            extra.FirstNoticeDate = DateTime.Now;
                            extra.FirstNoticeBalance = bal;
                        }
                        else
                        {
                            extra.SecondNoticeDate = DateTime.Now;
                            extra.SecondNoticeBalance = bal;
                        }
                        extra.ModifiedBy = userId;
                        extra.ModifiedDate = DateTime.Now;
                    }
                    await _dbContext.SaveChangesAsync();
                }
            }
            finally
            {
                _dbLock.Release();
            }

            return docxBytes;
        }

        private void EnsureNoticeTemplatesExist(string templatesPath)
        {
            if (!Directory.Exists(templatesPath))
            {
                Directory.CreateDirectory(templatesPath);
            }

            var files = new[] { "1st Notice.docx", "2nd Notice.docx", "1er Avis.docx", "2ieme Avis.docx" };
            foreach (var file in files)
            {
                string fullPath = Path.Combine(templatesPath, file);
                if (!File.Exists(fullPath))
                {
                    string title = file.Contains("1st") ? "First Overdue Notice" :
                                   file.Contains("2nd") ? "Second Overdue Notice" :
                                   file.Contains("1er") ? "Premier Avis de Retard" : "Deuxième Avis de Retard";

                    string bodyText = file.Contains("Avis") ?
                        "Ceci est un avis formel concernant votre solde impayé. Veuillez envoyer votre paiement immédiatement." :
                        "This is a formal notice regarding your outstanding balance. Please remit payment immediately.";

                    byte[] docxBytes = CreateMinimalDocx(title, bodyText);
                    File.WriteAllBytes(fullPath, docxBytes);
                }
            }
        }

        private byte[] CreateMinimalDocx(string title, string bodyText)
        {
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var contentTypesEntry = archive.CreateEntry("[Content_Types].xml");
                    using (var sw = new StreamWriter(contentTypesEntry.Open()))
                    {
                        sw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>
</Types>");
                    }

                    var relsEntry = archive.CreateEntry("_rels/.rels");
                    using (var sw = new StreamWriter(relsEntry.Open()))
                    {
                        sw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>
</Relationships>");
                    }

                    var docEntry = archive.CreateEntry("word/document.xml");
                    using (var sw = new StreamWriter(docEntry.Open()))
                    {
                        sw.Write($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
  <w:body>
    <w:p>
      <w:r><w:t>{title}</w:t></w:r>
    </w:p>
    <w:p>
      <w:r><w:t>Date: {{NoticeDate}}</w:t></w:r>
    </w:p>
    <w:p>
      <w:r><w:t>Account Number: {{AcctNo}}</w:t></w:r>
    </w:p>
    <w:p>
      <w:r><w:t>Customer Name: {{CustName}}</w:t></w:r>
    </w:p>
    <w:p>
      <w:r><w:t>Amount Overdue: ${{Amount}}</w:t></w:r>
    </w:p>
    <w:p>
      <w:r><w:t>{bodyText}</w:t></w:r>
    </w:p>
  </w:body>
</w:document>");
                    }
                }
                return ms.ToArray();
            }
        }

        public async Task<byte[]> OutputInvoicePdfAsync(ExportInvoiceRequest request, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            if (request.InvoiceType == "Bulk")
            {
                // Consolidate bulk invoices having the matching BulkID / FOB
                var invoiceNos = await _dbContext.ARDetailView
                    .Where(d => d.UserId == userId && d.CUST == request.CustNo)
                    .Join(_dbContext.tblARDetailExtra, d => d.TRANS_NO, e => e.TransNo, (d, e) => new { d.REF_NO, e.BulkID })
                    .Where(x => x.BulkID == request.InvoiceRef)
                    .Select(x => x.REF_NO)
                    .Distinct()
                    .ToListAsync();

                if (!invoiceNos.Any()) return Array.Empty<byte>();

                // Create a zipped output containing individual PDF invoices
                using (var ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        foreach (var invNo in invoiceNos)
                        {
                            var data = await GetInvoiceDataFromSpire(invNo);
                            if (data != null && data.Lines.Count > 0)
                            {
                                using (PdfDocument singlePdf = new PdfDocument())
                                {
                                    GeneratePdfLayout(singlePdf, data, invNo.PadLeft(10, '0'));
                                    var entry = archive.CreateEntry($"Invoice-{invNo}.pdf", System.IO.Compression.CompressionLevel.Optimal);
                                    using (var entryStream = entry.Open())
                                    {
                                        singlePdf.SaveToStream(entryStream);
                                    }
                                }
                            }
                        }
                    }
                    return ms.ToArray();
                }
            }
            else // Normal invoice
            {
                using (PdfDocument pdf = new PdfDocument())
                {
                    var data = await GetInvoiceDataFromSpire(request.InvoiceRef);
                    if (data == null || data.Lines.Count == 0) return Array.Empty<byte>();

                    GeneratePdfLayout(pdf, data, request.InvoiceRef.PadLeft(10, '0'));

                    using (MemoryStream ms = new MemoryStream())
                    {
                        pdf.SaveToStream(ms);
                        return ms.ToArray();
                    }
                }
            }
        }

        private async Task<InvoiceDetail> GetInvoiceDataFromSpire(string invoiceNo)
        {
            var detail = new InvoiceDetail();

            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                string headerSql = @"
                    SELECT cust_name, cust_no, invoice_date, order_no, 
                           '' as ship_name, '' as ship_address1, '' as ship_address2, '' as ship_city,
                           0 as tax_amount
                    FROM sales_history
                    WHERE invoice_no=@inv
                    LIMIT 1";

                using (var cmd = new NpgsqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@inv", invoiceNo);
                    cmd.CommandTimeout = 600;

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
                                if (dateVal is DateOnly dateOnly)
                                {
                                    detail.InvoiceDate = dateOnly.ToString("MMM dd, yyyy");
                                }
                                else if (dateVal is DateTime dateTime)
                                {
                                    detail.InvoiceDate = dateTime.ToString("MMM dd, yyyy");
                                }
                                else
                                {
                                    detail.InvoiceDate = dateVal.ToString();
                                }
                            }
                            else
                            {
                                detail.InvoiceDate = "N/A";
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                string lineSql = @"
                    SELECT shi.part_no, shi.description, shi.order_qty, shi.unit_price
                    FROM sales_history_items shi
                    WHERE shi.invoice_no=@inv";

                string serialSql = @"
                    SELECT part_no, number
                    FROM inventory_serial_transactions
                    WHERE link_no=@inv AND sales_qty > 0";

                var serials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new NpgsqlCommand(serialSql, conn))
                {
                    cmd.Parameters.AddWithValue("@inv", invoiceNo);
                    cmd.CommandTimeout = 600;
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            var partNo = r["part_no"]?.ToString() ?? "";
                            var serialNo = r["number"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(partNo) && !serials.ContainsKey(partNo))
                            {
                                serials[partNo] = serialNo;
                            }
                        }
                    }
                }

                using (var cmd = new NpgsqlCommand(lineSql, conn))
                {
                    cmd.Parameters.AddWithValue("@inv", invoiceNo);
                    cmd.CommandTimeout = 600;

                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            var partNo = r["part_no"]?.ToString() ?? "";
                            detail.Lines.Add(new InvoiceItemLine
                            {
                                PartNo = partNo,
                                Description = r["description"]?.ToString() ?? "",
                                Qty = r["order_qty"] != DBNull.Value ? Convert.ToDecimal(r["order_qty"]) : 0,
                                Price = r["unit_price"] != DBNull.Value ? Convert.ToDecimal(r["unit_price"]) : 0,
                                SerialNo = serials.TryGetValue(partNo, out var sn) ? sn : ""
                            });
                        }
                    }
                }
            }

            return detail;
        }

        private void GeneratePdfLayout(PdfDocument pdf, InvoiceDetail data, string formattedInv)
        {
            pdf.PageSettings.Size = PdfPageSize.A4;
            pdf.PageSettings.Margins.All = 0;
            PdfPageBase page = pdf.Pages.Add();

            PdfBrush blackBrush = PdfBrushes.Black;
            PdfBrush redBrush = new PdfSolidBrush(new PdfRGBColor(234, 18, 35));
            PdfBrush grayBrush = new PdfSolidBrush(new PdfRGBColor(245, 245, 245));
            PdfBrush darkGrayBrush = new PdfSolidBrush(new PdfRGBColor(80, 80, 80));
            PdfPen thinPen = new PdfPen(PdfBrushes.Black, 0.5f);
            PdfPen thickPen = new PdfPen(PdfBrushes.Black, 1.0f);

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

            page.Canvas.DrawString("Your Rogers Bill", titleFont, blackBrush, leftMargin, y + 5);

            float logoX = pageWidth / 2 - 50;
            float logoY = y;
            page.Canvas.DrawPie(redBrush, logoX, logoY, 20, 20, 0, 360);
            page.Canvas.DrawPie(redBrush, logoX + 12, logoY, 20, 20, 0, 360);
            page.Canvas.DrawString("ROGERS", logoFont, redBrush, logoX + 38, logoY - 2);
            page.Canvas.DrawString("™", smallFont, redBrush, logoX + 115, logoY);

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

            page.Canvas.DrawString("Remit Payment To: / Payer à:", smallBoldFont, blackBrush, leftMargin, y);
            y += 12;
            page.Canvas.DrawString("Rogers Communications Canada Inc.", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("30 Victoria Crescent", smallFont, blackBrush, leftMargin, y);
            y += 10;
            page.Canvas.DrawString("Brampton, ON L6T 1E4", smallFont, blackBrush, leftMargin, y);

            y += 35;

            float billToX = leftMargin;
            float shipToX = pageWidth / 2 + 10;
            float sectionY = y;

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

            float barHeight = 35;
            float barWidth = pageWidth - leftMargin - rightMargin;
            page.Canvas.DrawRectangle(thinPen, leftMargin, y, barWidth, barHeight);

            float colWidth = barWidth / 4;
            page.Canvas.DrawLine(thinPen, leftMargin + colWidth, y, leftMargin + colWidth, y + barHeight);
            page.Canvas.DrawLine(thinPen, leftMargin + colWidth * 2, y, leftMargin + colWidth * 2, y + barHeight);
            page.Canvas.DrawLine(thinPen, leftMargin + colWidth * 3, y, leftMargin + colWidth * 3, y + barHeight);

            float textY = y + 5;
            page.Canvas.DrawString("Ship Via / Expédier Via", smallBoldFont, blackBrush, leftMargin + 5, textY);
            page.Canvas.DrawString("Best way", normalFont, blackBrush, leftMargin + 5, textY + 12);

            page.Canvas.DrawString("Salesperson / Représentant", smallBoldFont, blackBrush, leftMargin + colWidth + 5, textY);
            page.Canvas.DrawString("CCO", normalFont, blackBrush, leftMargin + colWidth + 5, textY + 12);

            page.Canvas.DrawString("Terms / Termes", smallBoldFont, blackBrush, leftMargin + colWidth * 2 + 5, textY);
            page.Canvas.DrawString("V21 Account", normalFont, blackBrush, leftMargin + colWidth * 2 + 5, textY + 12);

            page.Canvas.DrawString("Order No.", smallBoldFont, blackBrush, leftMargin + colWidth * 3 + 5, textY);
            page.Canvas.DrawString("No. Commande", smallBoldFont, blackBrush, leftMargin + colWidth * 3 + colWidth - smallBoldFont.MeasureString("No. Commande").Width - 5, textY);
            string orderNo = data.OrderNo ?? "";
            page.Canvas.DrawString(orderNo, normalFont, blackBrush, leftMargin + colWidth * 3 + colWidth - normalFont.MeasureString(orderNo).Width - 5, textY + 12);

            y += barHeight + 20;

            float tableStart = y;
            float col1X = leftMargin;
            float col2X = leftMargin + 85;
            float col3X = pageWidth - rightMargin - 150;
            float col4X = pageWidth - rightMargin - 95;
            float col5X = pageWidth - rightMargin - 45;
            float tableEnd = pageWidth - rightMargin;

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
                }

                float rowY = y + 5;
                page.Canvas.DrawString(line.PartNo ?? "", normalFont, blackBrush, col1X + 5, rowY);

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

            page.Canvas.DrawLine(thinPen, leftMargin, contentStartY, leftMargin, y);
            page.Canvas.DrawLine(thinPen, col2X, contentStartY, col2X, y);
            page.Canvas.DrawLine(thinPen, col3X, contentStartY, col3X, y);
            page.Canvas.DrawLine(thinPen, col4X, contentStartY, col4X, y);
            page.Canvas.DrawLine(thinPen, col5X, contentStartY, col5X, y);
            page.Canvas.DrawLine(thinPen, tableEnd, contentStartY, tableEnd, y);
            page.Canvas.DrawLine(thinPen, leftMargin, y, tableEnd, y);

            y += 20;

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

        public async Task<byte[]> OutputPaymentAdvicePdfAsync(string transNo, int userId)
        {
            // Fetch payment transaction
            PaymentAdviceDetail payment = null;
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                string paySql = @"
                    SELECT t.id, t.cust_no, c.name, t.date, t.ref_no, t.credit_amt
                    FROM ar_transactions t
                    INNER JOIN customers c ON t.cust_no = c.cust_no
                    WHERE t.trans_no = @transNo
                    LIMIT 1";

                using (var cmd = new NpgsqlCommand(paySql, conn))
                {
                    cmd.Parameters.AddWithValue("@transNo", transNo);
                    cmd.CommandTimeout = 600;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            payment = new PaymentAdviceDetail
                            {
                                Id = reader.GetInt32(0),
                                CustNo = reader.GetString(1),
                                CustName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PaymentDate = reader.GetDateTime(3),
                                ReferenceNo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                TotalAmount = reader.GetDecimal(5),
                                Invoices = new List<PaymentAppliedInvoice>()
                            };
                        }
                    }
                }

                if (payment == null) return Array.Empty<byte>();

                // Fetch invoices linked to this payment
                string linkSql = @"
                    SELECT d.ref_no, d.date, l.applied_amt
                    FROM ar_transaction_links l
                    INNER JOIN ar_transactions d ON l.debit_id = d.id
                    WHERE l.credit_id = @creditId";

                using (var cmd = new NpgsqlCommand(linkSql, conn))
                {
                    cmd.Parameters.AddWithValue("creditId", payment.Id);
                    cmd.CommandTimeout = 600;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            payment.Invoices.Add(new PaymentAppliedInvoice
                            {
                                InvoiceNo = reader.GetString(0),
                                InvoiceDate = reader.GetDateTime(1),
                                AppliedAmount = reader.GetDecimal(2)
                            });
                        }
                    }
                }
            }

            // Draw Payment Advice PDF
            using (PdfDocument pdf = new PdfDocument())
            {
                pdf.PageSettings.Size = PdfPageSize.A4;
                pdf.PageSettings.Margins.All = 0;
                PdfPageBase page = pdf.Pages.Add();

                PdfBrush blackBrush = PdfBrushes.Black;
                PdfBrush redBrush = new PdfSolidBrush(new PdfRGBColor(234, 18, 35));
                PdfPen thinPen = new PdfPen(PdfBrushes.Black, 0.5f);
                PdfPen thickPen = new PdfPen(PdfBrushes.Black, 1.0f);

                PdfFont titleFont = new PdfFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                PdfFont headerFont = new PdfFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                PdfFont normalFont = new PdfFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Regular);
                PdfFont normalBoldFont = new PdfFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
                PdfFont smallFont = new PdfFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);

                float pageWidth = page.Canvas.ClientSize.Width;
                float leftMargin = 40;
                float rightMargin = 40;
                float y = 40;

                // Header
                page.Canvas.DrawString("PAYMENT ADVICE / AVIS DE PAIEMENT", titleFont, redBrush, leftMargin, y);
                y += 35;

                // Customer and Payment details
                page.Canvas.DrawString("Customer Code:", normalBoldFont, blackBrush, leftMargin, y);
                page.Canvas.DrawString(payment.CustNo, normalFont, blackBrush, leftMargin + 110, y);
                y += 15;

                page.Canvas.DrawString("Customer Name:", normalBoldFont, blackBrush, leftMargin, y);
                page.Canvas.DrawString(payment.CustName, normalFont, blackBrush, leftMargin + 110, y);
                y += 15;

                page.Canvas.DrawString("Payment Date:", normalBoldFont, blackBrush, leftMargin, y);
                page.Canvas.DrawString(payment.PaymentDate.ToString("MMM dd, yyyy"), normalFont, blackBrush, leftMargin + 110, y);
                y += 15;

                page.Canvas.DrawString("Reference No:", normalBoldFont, blackBrush, leftMargin, y);
                page.Canvas.DrawString(payment.ReferenceNo, normalFont, blackBrush, leftMargin + 110, y);
                y += 15;

                page.Canvas.DrawString("Total Paid:", normalBoldFont, blackBrush, leftMargin, y);
                page.Canvas.DrawString("$" + payment.TotalAmount.ToString("N2"), normalBoldFont, redBrush, leftMargin + 110, y);
                y += 30;

                // Applied Invoices Table Header
                float tableWidth = pageWidth - leftMargin - rightMargin;
                page.Canvas.DrawRectangle(thinPen, leftMargin, y, tableWidth, 20);

                float col1Width = 150;
                float col2Width = 150;
                float col1X = leftMargin;
                float col2X = leftMargin + col1Width;
                float col3X = leftMargin + col1Width + col2Width;
                float tableEnd = pageWidth - rightMargin;

                page.Canvas.DrawLine(thinPen, col2X, y, col2X, y + 20);
                page.Canvas.DrawLine(thinPen, col3X, y, col3X, y + 20);

                page.Canvas.DrawString("Invoice No. / Facture", headerFont, blackBrush, col1X + 5, y + 4);
                page.Canvas.DrawString("Invoice Date / Date", headerFont, blackBrush, col2X + 5, y + 4);
                page.Canvas.DrawString("Amount Applied / Appliqué", headerFont, blackBrush, col3X + 5, y + 4);

                y += 20;
                float contentStartY = y;

                decimal totalApplied = 0;
                foreach (var inv in payment.Invoices)
                {
                    page.Canvas.DrawString(inv.InvoiceNo, normalFont, blackBrush, col1X + 5, y + 4);
                    page.Canvas.DrawString(inv.InvoiceDate.ToString("yyyy-MM-dd"), normalFont, blackBrush, col2X + 5, y + 4);
                    page.Canvas.DrawString("$" + inv.AppliedAmount.ToString("N2"), normalFont, blackBrush, tableEnd - 5 - normalFont.MeasureString("$" + inv.AppliedAmount.ToString("N2")).Width, y + 4);

                    totalApplied += inv.AppliedAmount;
                    y += 18;

                    page.Canvas.DrawLine(thinPen, leftMargin, y, tableEnd, y);
                }

                // Table vertical lines
                page.Canvas.DrawLine(thinPen, leftMargin, contentStartY, leftMargin, y);
                page.Canvas.DrawLine(thinPen, col2X, contentStartY, col2X, y);
                page.Canvas.DrawLine(thinPen, col3X, contentStartY, col3X, y);
                page.Canvas.DrawLine(thinPen, tableEnd, contentStartY, tableEnd, y);

                y += 15;

                // Totals
                page.Canvas.DrawString("Total Applied:", normalBoldFont, blackBrush, col2X, y);
                page.Canvas.DrawString("$" + totalApplied.ToString("N2"), normalBoldFont, blackBrush, tableEnd - 5 - normalBoldFont.MeasureString("$" + totalApplied.ToString("N2")).Width, y);
                y += 15;

                decimal unapplied = payment.TotalAmount - totalApplied;
                page.Canvas.DrawString("Unapplied Balance:", normalBoldFont, blackBrush, col2X, y);
                page.Canvas.DrawString("$" + unapplied.ToString("N2"), normalBoldFont, redBrush, tableEnd - 5 - normalBoldFont.MeasureString("$" + unapplied.ToString("N2")).Width, y);

                using (MemoryStream ms = new MemoryStream())
                {
                    pdf.SaveToStream(ms);
                    return ms.ToArray();
                }
            }
        }

        public async Task<byte[]> OutputCheckedDocumentsAsync(string custNo, bool chkSendBulk, List<string> checkedTransNos, int userId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            if (checkedTransNos == null || !checkedTransNos.Any())
            {
                return Array.Empty<byte>();
            }

            // Fetch checked transactions detail from ARDetailView/tblARDetailExtra
            var checkedItems = await (from d in _dbContext.ARDetailView
                                      join e in _dbContext.tblARDetailExtra on d.TRANS_NO equals e.TransNo
                                      where d.UserId == userId && checkedTransNos.Contains(d.TRANS_NO)
                                      select new
                                      {
                                          d.TRANS_NO,
                                          d.REF_NO,
                                          d.Type,
                                          e.BulkID,
                                          d.CUST
                                      }).ToListAsync();

            if (!checkedItems.Any()) return Array.Empty<byte>();

            // For counting Bulk ID matches in Spire:
            var checkedBulkIds = checkedItems
                .Where(x => !string.IsNullOrEmpty(x.BulkID))
                .Select(x => x.BulkID!)
                .Distinct()
                .ToList();

            var spireBulkCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (checkedBulkIds.Any() && chkSendBulk)
            {
                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();
                    string countSql = "SELECT fob, COUNT(*) FROM sales_history WHERE fob = ANY(@bulkIds) GROUP BY fob";
                    using (var cmd = new NpgsqlCommand(countSql, conn))
                    {
                        cmd.Parameters.AddWithValue("bulkIds", checkedBulkIds);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fob = reader.GetString(0);
                                var count = reader.GetInt32(1);
                                spireBulkCounts[fob] = count;
                            }
                        }
                    }
                }
            }

            var outputtedRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var item in checkedItems)
                    {
                        if (item.Type == "I" || item.Type == "C")
                        {
                            string strInvoiceRef;
                            string strInvoiceType;

                            if (chkSendBulk && !string.IsNullOrEmpty(item.BulkID))
                            {
                                int bulkCountBatch = checkedItems.Count(x => string.Equals(x.BulkID, item.BulkID, StringComparison.OrdinalIgnoreCase));
                                spireBulkCounts.TryGetValue(item.BulkID, out int bulkCountBV);

                                if (bulkCountBV > 0 && bulkCountBV == bulkCountBatch)
                                {
                                    strInvoiceRef = item.BulkID;
                                    strInvoiceType = "Bulk";
                                }
                                else
                                {
                                    strInvoiceRef = !string.IsNullOrEmpty(item.REF_NO) ? item.REF_NO : item.TRANS_NO;
                                    strInvoiceType = "Normal";
                                }
                            }
                            else
                            {
                                strInvoiceRef = !string.IsNullOrEmpty(item.REF_NO) ? item.REF_NO : item.TRANS_NO;
                                strInvoiceType = "Normal";
                            }

                            if (!outputtedRefs.Contains(strInvoiceRef))
                            {
                                outputtedRefs.Add(strInvoiceRef);

                                if (strInvoiceType == "Bulk")
                                {
                                    var bulkInvoiceNos = await (from d in _dbContext.ARDetailView
                                                                join e in _dbContext.tblARDetailExtra on d.TRANS_NO equals e.TransNo
                                                                where d.UserId == userId && d.CUST == item.CUST && e.BulkID == strInvoiceRef
                                                                select d.REF_NO)
                                                                .Distinct()
                                                                .ToListAsync();

                                    if (bulkInvoiceNos.Any())
                                    {
                                        using (PdfDocument bulkPdf = new PdfDocument())
                                        {
                                            foreach (var invNo in bulkInvoiceNos)
                                            {
                                                if (string.IsNullOrEmpty(invNo)) continue;
                                                var data = await GetInvoiceDataFromSpire(invNo);
                                                if (data != null && data.Lines.Count > 0)
                                                {
                                                    GeneratePdfLayout(bulkPdf, data, invNo.PadLeft(10, '0'));
                                                }
                                            }

                                            var entry = archive.CreateEntry($"BulkInvoice-{strInvoiceRef}.pdf", System.IO.Compression.CompressionLevel.Optimal);
                                            using (var entryStream = entry.Open())
                                            {
                                                bulkPdf.SaveToStream(entryStream);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var data = await GetInvoiceDataFromSpire(strInvoiceRef);
                                    if (data != null && data.Lines.Count > 0)
                                    {
                                        using (PdfDocument pdf = new PdfDocument())
                                        {
                                            GeneratePdfLayout(pdf, data, strInvoiceRef.PadLeft(10, '0'));
                                            var entry = archive.CreateEntry($"Invoice-{strInvoiceRef}.pdf", System.IO.Compression.CompressionLevel.Optimal);
                                            using (var entryStream = entry.Open())
                                            {
                                                pdf.SaveToStream(entryStream);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (item.Type == "P")
                        {
                            if (!outputtedRefs.Contains(item.TRANS_NO))
                            {
                                outputtedRefs.Add(item.TRANS_NO);

                                byte[] pdfBytes = await OutputPaymentAdvicePdfAsync(item.TRANS_NO, userId);
                                if (pdfBytes != null && pdfBytes.Length > 0)
                                {
                                    var entry = archive.CreateEntry($"Payment-{item.TRANS_NO}.pdf", System.IO.Compression.CompressionLevel.Optimal);
                                    using (var entryStream = entry.Open())
                                    {
                                        await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                                    }
                                }
                            }
                        }
                    }
                }

                return memoryStream.ToArray();
            }
        }

        public async Task<List<ARCollectionUser>> GetARUsersAsync(int page, int pageSize)
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await (from u in _dbContext.tblUsers
                          join g in _dbContext.tblTerritoryGroups on u.DefaultChannel equals g.ID into gJoin
                          from g in gJoin.DefaultIfEmpty()
                          select new ARCollectionUser
                          {
                              ID = u.ID,
                              DomainUser = u.DomainUser,
                              Initials = u.Initials,
                              DefaultChannel = u.DefaultChannel,
                              ChannelName = g != null ? g.GroupName : null,
                              CreatedBy = u.CreatedBy,
                              CreatedDate = u.CreatedDate,
                              ModifiedBy = u.ModifiedBy,
                              ModifiedDate = u.ModifiedDate
                          })
                          .OrderBy(u => u.DomainUser)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync();
        }

        public async Task<int> GetARUsersCountAsync()
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await _dbContext.tblUsers.CountAsync();
        }

        public async Task<bool> CreateARUserAsync(ARCollectionUser user, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var newUser = new TblUsers
            {
                DomainUser = user.DomainUser,
                Initials = user.Initials,
                DefaultChannel = user.DefaultChannel,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.Now
            };
            await _dbContext.tblUsers.AddAsync(newUser);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateARUserAsync(ARCollectionUser user, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var dbUser = await _dbContext.tblUsers.FirstOrDefaultAsync(u => u.ID == user.ID);
            if (dbUser == null) return false;

            dbUser.DomainUser = user.DomainUser;
            dbUser.Initials = user.Initials;
            dbUser.DefaultChannel = user.DefaultChannel;
            dbUser.ModifiedBy = currentUserId;
            dbUser.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteARUserAsync(int id)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var dbUser = await _dbContext.tblUsers.FirstOrDefaultAsync(u => u.ID == id);
            if (dbUser == null) return false;

            _dbContext.tblUsers.Remove(dbUser);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // --- Customer Groups Management ---
        public async Task<List<TblCustomerGroups>> GetCustomerGroupsAsync(int page, int pageSize)
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await _dbContext.tblCustomerGroups
                .OrderBy(g => g.CustGroup)
                .ThenBy(g => g.BVCustNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCustomerGroupsCountAsync()
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await _dbContext.tblCustomerGroups.CountAsync();
        }

        public async Task<bool> CreateCustomerGroupAsync(TblCustomerGroups group, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            // Check if group-customer mapping already exists
            var exists = await _dbContext.tblCustomerGroups
                .AnyAsync(g => g.CustGroup == group.CustGroup && g.BVCustNo == group.BVCustNo);
            if (exists) return false;

            group.CreatedBy = currentUserId;
            group.CreatedDate = DateTime.Now;

            await _dbContext.tblCustomerGroups.AddAsync(group);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCustomerGroupAsync(TblCustomerGroups group, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var dbGroup = await _dbContext.tblCustomerGroups.FirstOrDefaultAsync(g => g.Id == group.Id);
            if (dbGroup == null) return false;

            dbGroup.CustGroup = group.CustGroup;
            dbGroup.BVCustNo = group.BVCustNo;
            dbGroup.GroupName = group.GroupName;
            dbGroup.BVName = group.BVName;
            dbGroup.ModifiedBy = currentUserId;
            dbGroup.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCustomerGroupAsync(int id)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var dbGroup = await _dbContext.tblCustomerGroups.FirstOrDefaultAsync(g => g.Id == id);
            if (dbGroup == null) return false;

            _dbContext.tblCustomerGroups.Remove(dbGroup);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // --- Bulk Customers Management ---
        public async Task<List<TblBulkCustomers>> GetBulkCustomersAsync(int page, int pageSize)
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await _dbContext.tblBulkCustomers
                .OrderBy(b => b.CustNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetBulkCustomersCountAsync()
        {
            _dbContext.Database.SetCommandTimeout(600);
            return await _dbContext.tblBulkCustomers.CountAsync();
        }

        public async Task<bool> CreateBulkCustomerAsync(TblBulkCustomers bulk, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var exists = await _dbContext.tblBulkCustomers.AnyAsync(b => b.CustNo == bulk.CustNo);
            if (exists) return false;

            bulk.CreatedBy = currentUserId;
            bulk.CreatedDate = DateTime.Now;

            await _dbContext.tblBulkCustomers.AddAsync(bulk);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBulkCustomerAsync(int id)
        {
            _dbContext.Database.SetCommandTimeout(600);

            var dbBulk = await _dbContext.tblBulkCustomers.FirstOrDefaultAsync(b => b.ID == id);
            if (dbBulk == null) return false;

            _dbContext.tblBulkCustomers.Remove(dbBulk);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // --- Parity with Access Form frmCustGroupMaintain ---
        public async Task<List<CustomerGroupSummary>> GetARGroupsSummaryAsync(string groupType)
        {
            _dbContext.Database.SetCommandTimeout(600);
            if (groupType == "Collections Groups")
            {
                return await _dbContext.tblCustomerGroups
                    .GroupBy(g => g.CustGroup)
                    .Select(g => new CustomerGroupSummary
                    {
                        CustGroup = g.Key,
                        MaxOfGroupName = g.Max(x => x.GroupName) ?? "",
                        CountOfCustGroup = g.Count()
                    })
                    .OrderBy(g => g.MaxOfGroupName)
                    .ToListAsync();
            }
            else
            {
                return await _dbContext.tblCustomerGroupsRR
                    .GroupBy(g => g.CustGroup)
                    .Select(g => new CustomerGroupSummary
                    {
                        CustGroup = g.Key,
                        MaxOfGroupName = g.Max(x => x.GroupName) ?? "",
                        CountOfCustGroup = g.Count()
                    })
                    .OrderBy(g => g.MaxOfGroupName)
                    .ToListAsync();
            }
        }

        public async Task<List<GroupCustomerRow>> GetARGroupCustomersAsync(string groupType, string custGroup)
        {
            _dbContext.Database.SetCommandTimeout(600);
            if (groupType == "Collections Groups")
            {
                return await _dbContext.tblCustomerGroups
                    .Where(g => g.CustGroup == custGroup)
                    .Select(g => new GroupCustomerRow
                    {
                        Id = g.Id,
                        CustGroup = g.CustGroup,
                        BVCustNo = g.BVCustNo,
                        GroupName = g.GroupName,
                        BVName = g.BVName
                    })
                    .ToListAsync();
            }
            else
            {
                return await _dbContext.tblCustomerGroupsRR
                    .Where(g => g.CustGroup == custGroup)
                    .Select(g => new GroupCustomerRow
                    {
                        //Id = g.Id,z
                        CustGroup = g.CustGroup,
                        BVCustNo = g.BVCustNo,
                        GroupName = g.GroupName,
                        BVName = g.BVName
                    })
                    .ToListAsync();
            }
        }

        public async Task<(bool exists, string name)> LookupSpireCustomerNameAsync(string custNo)
        {
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                string sql = "SELECT name FROM customers WHERE cust_no = @custNo LIMIT 1";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("custNo", custNo);
                    cmd.CommandTimeout = 600;
                    var nameObj = await cmd.ExecuteScalarAsync();
                    if (nameObj != null && nameObj != DBNull.Value)
                    {
                        return (true, nameObj.ToString() ?? "");
                    }
                }
            }
            return (false, "");
        }

        public async Task<string> AddCustomerToGroupAsync(string groupType, string custNo, bool isNewGroup, string newGroupName, string selectedCustGroup, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var userInitials = await _dbContext.tblUsers
                .Where(u => u.ID == currentUserId)
                .Select(u => u.Initials)
                .FirstOrDefaultAsync() ?? "SA";

            // 1. Verify customer exists in Spire
            var (exists, custName) = await LookupSpireCustomerNameAsync(custNo);
            if (!exists) return $"Spire Customer Number {custNo} not found.";

            // 2. Verify customer is not already in a group in this table
            if (groupType == "Collections Groups")
            {
                var existingGroup = await _dbContext.tblCustomerGroups
                    .Where(g => g.BVCustNo == custNo)
                    .Select(g => g.CustGroup)
                    .FirstOrDefaultAsync();
                if (existingGroup != null) return $"Customer {custNo} already exists in group {existingGroup}";
            }
            else
            {
                var existingGroup = await _dbContext.tblCustomerGroupsRR
                    .Where(g => g.BVCustNo == custNo)
                    .Select(g => g.CustGroup)
                    .FirstOrDefaultAsync();
                if (existingGroup != null) return $"Customer {custNo} already exists in group {existingGroup}";
            }

            string groupId = "";
            string groupName = "";

            if (isNewGroup)
            {
                groupName = newGroupName.Trim();
                if (string.IsNullOrEmpty(groupName)) return "New Group Name is required.";

                // Verify group name doesn't exist
                if (groupType == "Collections Groups")
                {
                    if (await _dbContext.tblCustomerGroups.AnyAsync(g => g.GroupName.Trim().ToLower() == groupName.ToLower()))
                        return "A group with this name already exists.";
                }
                else
                {
                    if (await _dbContext.tblCustomerGroupsRR.AnyAsync(g => g.GroupName.Trim().ToLower() == groupName.ToLower()))
                        return "A group with this name already exists.";
                }

                // Compute next group ID
                string maxGroup = "";
                if (groupType == "Collections Groups")
                {
                    maxGroup = await _dbContext.tblCustomerGroups.MaxAsync(g => g.CustGroup) ?? "";
                    int nextNum = 1;
                    if (!string.IsNullOrEmpty(maxGroup) && maxGroup.Length >= 6 && maxGroup.StartsWith("ARC"))
                    {
                        if (int.TryParse(maxGroup.Substring(3), out int num))
                        {
                            nextNum = num + 1;
                        }
                    }
                    groupId = "ARC" + nextNum.ToString("D3");
                }
                else
                {
                    maxGroup = await _dbContext.tblCustomerGroupsRR.MaxAsync(g => g.CustGroup) ?? "";
                    int nextNum = 1;
                    if (!string.IsNullOrEmpty(maxGroup) && maxGroup.Length >= 5 && maxGroup.StartsWith("RR"))
                    {
                        if (int.TryParse(maxGroup.Substring(2), out int num))
                        {
                            nextNum = num + 1;
                        }
                    }
                    groupId = "RR" + nextNum.ToString("D3");
                }
            }
            else
            {
                groupId = selectedCustGroup;
                if (string.IsNullOrEmpty(groupId)) return "You must select a group from the left panel.";

                if (groupType == "Collections Groups")
                {
                    groupName = await _dbContext.tblCustomerGroups
                        .Where(g => g.CustGroup == groupId)
                        .Select(g => g.GroupName)
                        .FirstOrDefaultAsync() ?? "";
                }
                else
                {
                    groupName = await _dbContext.tblCustomerGroupsRR
                        .Where(g => g.CustGroup == groupId)
                        .Select(g => g.GroupName)
                        .FirstOrDefaultAsync() ?? "";
                }
            }

            // 3. Insert mapping
            if (groupType == "Collections Groups")
            {
                var newMap = new TblCustomerGroups
                {
                    CustGroup = groupId,
                    GroupName = groupName,
                    BVCustNo = custNo,
                    BVName = custName,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.Now
                };
                await _dbContext.tblCustomerGroups.AddAsync(newMap);
            }
            else
            {
                var newMap = new TblCustomerGroupsRR
                {
                    CustGroup = groupId,
                    GroupName = groupName,
                    BVCustNo = custNo,
                    BVName = custName,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.Now
                };
                await _dbContext.tblCustomerGroupsRR.AddAsync(newMap);
            }

            // 4. If Collections Groups, update cached open customers
            if (groupType == "Collections Groups")
            {
                var openCusts = await _dbContext.tblCustomersOpen
                    .Where(c => c.CUST == custNo)
                    .ToListAsync();
                foreach (var oc in openCusts)
                {
                    oc.CustGroup = groupId;
                    oc.ModifiedBy = currentUserId;
                    oc.ModifiedDate = DateTime.Now;
                }
            }

            // 5. Update events in tblEvents
            var events = await _dbContext.tblEvents
                .Where(e => e.CustType == "Single" && e.CustNo == custNo)
                .ToListAsync();
            foreach (var ev in events)
            {
                ev.CustType = "Group";
                ev.CustNo = groupId;
                ev.ModUser = userInitials;
                ev.ModDate = DateTime.Now;

            }

            await _dbContext.SaveChangesAsync();
            return "SUCCESS";
        }

        public async Task<bool> RemoveCustomerFromGroupAsync(string groupType, string custNo)
        {
            _dbContext.Database.SetCommandTimeout(600);
            if (groupType == "Collections Groups")
            {
                var mappings = await _dbContext.tblCustomerGroups
                    .Where(g => g.BVCustNo == custNo)
                    .ToListAsync();
                if (!mappings.Any()) return false;

                _dbContext.tblCustomerGroups.RemoveRange(mappings);

                var openCusts = await _dbContext.tblCustomersOpen
                    .Where(c => c.CUST == custNo)
                    .ToListAsync();
                foreach (var oc in openCusts)
                {
                    oc.CustGroup = null;
                }
            }
            else
            {
                var mappings = await _dbContext.tblCustomerGroupsRR
                    .Where(g => g.BVCustNo == custNo)
                    .ToListAsync();
                if (!mappings.Any()) return false;

                _dbContext.tblCustomerGroupsRR.RemoveRange(mappings);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModifyGroupNameAsync(string groupType, string custGroup, string newGroupName)
        {
            _dbContext.Database.SetCommandTimeout(600);
            string trimmedName = newGroupName.Trim();
            if (string.IsNullOrEmpty(trimmedName)) return false;

            if (groupType == "Collections Groups")
            {
                var mappings = await _dbContext.tblCustomerGroups
                    .Where(g => g.CustGroup == custGroup)
                    .ToListAsync();
                if (!mappings.Any()) return false;

                foreach (var m in mappings)
                {
                    m.GroupName = trimmedName;
                }
            }
            else
            {
                var mappings = await _dbContext.tblCustomerGroupsRR
                    .Where(g => g.CustGroup == custGroup)
                    .ToListAsync();
                if (!mappings.Any()) return false;

                foreach (var m in mappings)
                {
                    m.GroupName = trimmedName;
                }
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<BulkCustomerRow>> GetBulkCustomersWithNameAsync()
        {
            _dbContext.Database.SetCommandTimeout(600);
            var bulks = await _dbContext.tblBulkCustomers.ToListAsync();
            var result = new List<BulkCustomerRow>();

            if (bulks.Any())
            {
                var bulkCustNos = bulks.Select(b => b.CustNo).Distinct().ToList();
                var namesMap = new Dictionary<string, string>();
                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();
                    string sql = "SELECT cust_no, name FROM customers WHERE cust_no = ANY(@custNos)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("custNos", bulkCustNos);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                namesMap[reader.GetString(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            }
                        }
                    }
                }
                foreach (var b in bulks)
                {
                    namesMap.TryGetValue(b.CustNo, out string? name);
                    result.Add(new BulkCustomerRow
                    {
                        ID = b.ID,
                        CustNo = b.CustNo,
                        Name = name ?? ""
                    });
                }
            }
            return result;
        }

        public async Task<bool> AddBulkCustomerAsync(string custNo, int currentUserId)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var (exists, name) = await LookupSpireCustomerNameAsync(custNo);
            if (!exists) return false;

            var alreadyBulk = await _dbContext.tblBulkCustomers.AnyAsync(b => b.CustNo == custNo);
            if (alreadyBulk) return false;

            var bulk = new TblBulkCustomers
            {
                CustNo = custNo,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.Now
            };
            await _dbContext.tblBulkCustomers.AddAsync(bulk);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveBulkCustomerAsync(int id)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var bulk = await _dbContext.tblBulkCustomers.FirstOrDefaultAsync(b => b.ID == id);
            if (bulk == null) return false;

            _dbContext.tblBulkCustomers.Remove(bulk);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<GLAllowedAccountDto>> GetGLAllowedAccountsAsync()
        {
            _dbContext.Database.SetCommandTimeout(600);
            var allowedAccounts = await _dbContext.tblAllowedAccounts
                .Select(a => a.Account)
                .ToListAsync();

            var accountNames = new Dictionary<string, string>();
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                string sql = "SELECT DISTINCT account_no, name FROM gl_accounts WHERE account_no = ANY(@accounts)";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("accounts", allowedAccounts);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var acc = reader.GetString(0);
                            var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            if (!accountNames.ContainsKey(acc))
                            {
                                accountNames[acc] = name;
                            }
                        }
                    }
                }
            }

            var result = allowedAccounts
                .Select(acc => new GLAllowedAccountDto
                {
                    Account = acc,
                    Name = accountNames.TryGetValue(acc, out var name) ? name : ""
                })
                .OrderBy(x => x.Account)
                .ToList();

            return result;
        }

        public async Task<List<GLActivityRow>> GetGLActivityAsync(string accountNo, DateTime startDate, DateTime endDate)
        {
            _dbContext.Database.SetCommandTimeout(600);
            var result = new List<GLActivityRow>();

            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT 
                        t.account_no, 
                        a.name AS AccountName, 
                        t.date, 
                        t.trans_no, 
                        t.where_from AS Source, 
                        t.gl_user AS User, 
                        t.gl_memo, 
                        t.mf_who AS Type, 
                        t.mf_key AS Entity, 
                        t.mf_tran AS Document,  
                        t.debit_amt, 
                        t.credit_amt, 
                        t.post_date
                    FROM gl_transactions t
                    INNER JOIN gl_accounts a 
                        ON a.division = t.division 
                        AND a.account_no = t.account_no 
                        AND a.currency = t.currency
                    WHERE t.account_no = @accountNo
                      AND t.date BETWEEN @startDate AND @endDate";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("accountNo", accountNo);
                    cmd.Parameters.AddWithValue("startDate", NpgsqlTypes.NpgsqlDbType.Date, startDate.Date);
                    cmd.Parameters.AddWithValue("endDate", NpgsqlTypes.NpgsqlDbType.Date, endDate.Date);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new GLActivityRow
                            {
                                AccountNo = reader.GetString(0),
                                AccountName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Date = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                TransNo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Source = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                User = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                GLMemo = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Type = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                Entity = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                Document = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                DebitAmt = reader.IsDBNull(10) ? 0m : reader.GetDecimal(10),
                                CreditAmt = reader.IsDBNull(11) ? 0m : reader.GetDecimal(11),
                                PostDate = reader.IsDBNull(12) ? (DateTime?)null : reader.GetDateTime(12)
                            };
                            row.Balance = row.DebitAmt - row.CreditAmt;
                            result.Add(row);
                        }
                    }
                }
            }

            if (result.Any())
            {
                var docNos = result.Select(r => r.Document).Where(doc => !string.IsNullOrEmpty(doc)).Distinct().ToList();
                if (docNos.Any())
                {
                    var webOrders = new Dictionary<string, string?>();
                    const int chunkSize = 200;
                    for (int i = 0; i < docNos.Count; i += chunkSize)
                    {
                        var chunk = docNos.Skip(i).Take(chunkSize).ToList();

                        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(SalesActivations), "sa");
                        var member = System.Linq.Expressions.Expression.Property(parameter, nameof(SalesActivations.Invoice10));
                        System.Linq.Expressions.Expression body = null;
                        foreach (var docNo in chunk)
                        {
                            var constant = System.Linq.Expressions.Expression.Constant(docNo);
                            var equality = System.Linq.Expressions.Expression.Equal(member, constant);
                            body = body == null ? equality : System.Linq.Expressions.Expression.OrElse(body, equality);
                        }
                        var lambda = System.Linq.Expressions.Expression.Lambda<Func<SalesActivations, bool>>(body!, parameter);

                        var chunkResult = await _dbContext.SalesActivations
                            .Where(lambda)
                            .Select(sa => new { sa.Invoice10, sa.WebOrderID })
                            .ToListAsync();

                        foreach (var item in chunkResult)
                        {
                            if (!string.IsNullOrEmpty(item.Invoice10) && !webOrders.ContainsKey(item.Invoice10))
                            {
                                webOrders[item.Invoice10] = item.WebOrderID;
                            }
                        }
                    }

                    foreach (var row in result)
                    {
                        if (!string.IsNullOrEmpty(row.Document) && webOrders.TryGetValue(row.Document, out var webOrderId))
                        {
                            row.WebOrderID = webOrderId;
                        }
                    }
                }
            }

            return result;
        }

        public async Task<byte[]> ExportGLActivityAsync(string accountNo, DateTime startDate, DateTime endDate)
        {
            var data = await GetGLActivityAsync(accountNo, startDate, endDate);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("GL Activity");

                string[] headers = new string[] {
                    "account_no", "AccountName", "Date", "trans_no", "Source", "User",
                    "gl_memo", "Type", "Entity", "Document", "debit_amt", "credit_amt",
                    "balance", "WebOrderID", "post_date"
                };

                // --- ROW 1: HEADERS ---
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                }

                using (var range = ws.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // --- ROW 2+: DATA ---
                int currentRow = 2;
                foreach (var row in data)
                {
                    ws.Cells[currentRow, 1].Value = row.AccountNo;
                    ws.Cells[currentRow, 2].Value = row.AccountName;
                    ws.Cells[currentRow, 3].Value = row.Date?.ToString("MM/dd/yyyy");
                    ws.Cells[currentRow, 4].Value = row.TransNo;
                    ws.Cells[currentRow, 5].Value = row.Source;
                    ws.Cells[currentRow, 6].Value = row.User;
                    ws.Cells[currentRow, 7].Value = row.GLMemo;
                    ws.Cells[currentRow, 8].Value = row.Type;
                    ws.Cells[currentRow, 9].Value = row.Entity;
                    ws.Cells[currentRow, 10].Value = row.Document;
                    ws.Cells[currentRow, 11].Value = row.DebitAmt;
                    ws.Cells[currentRow, 12].Value = row.CreditAmt;
                    ws.Cells[currentRow, 13].Value = row.Balance;
                    ws.Cells[currentRow, 14].Value = row.WebOrderID;
                    ws.Cells[currentRow, 15].Value = row.PostDate?.ToString("MM/dd/yyyy");

                    currentRow++;
                }

                // Enable AutoFilter on column headers
                ws.Cells[1, 1, currentRow - 1, headers.Length].AutoFilter = true;

                // Format number columns
                if (currentRow > 2)
                {
                    using (var range = ws.Cells[2, 11, currentRow - 1, 13])
                    {
                        range.Style.Numberformat.Format = "$#,##0.00";
                    }
                }

                ws.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        #region Comment Review

        public async Task<bool> GenerateCommentReviewDataAsync(DateTime agingDate, int userId)
        {
            try
            {
                _dbContext.Database.SetCommandTimeout(600);

                // 1. Delete existing cached AR details for this user session
                await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblARDetailViewFull WHERE UserId = {0}", userId);

                // 2. Fetch all open transactions from PG spire database
                var pgTransactions = new List<TblARDetailViewFull>();
                using (var conn = new NpgsqlConnection(_pgConn))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT cust_no, code, trans_no, ref_no, date, debit_amt, credit_amt, balance, id
                        FROM ar_transactions
                        WHERE open_close_flag = 'O'";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 600;
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var tCust = reader.GetString(0);
                                var tCode = reader.GetString(1);
                                var tTrans = reader.GetString(2);
                                var tRef = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                var tDate = reader.GetDateTime(4);
                                var dAmt = reader.GetDecimal(5);
                                var cAmt = reader.GetDecimal(6);
                                var bal = reader.GetDecimal(7);
                                var pgId = reader.GetInt32(8);

                                pgTransactions.Add(new TblARDetailViewFull
                                {
                                    CUST = tCust,
                                    FOLIO = tTrans,
                                    TopItem = "*",
                                    Type = tCode,
                                    TRANS_NO = tTrans,
                                    REF_NO = tRef,
                                    TranDate = tDate,
                                    D_AMOUNT = dAmt,
                                    C_AMOUNT = cAmt,
                                    BALANCE = dAmt > 0 ? dAmt : cAmt * -1,
                                    DaysOld = (agingDate.Date - tDate.Date).Days,
                                    ARID = pgId,
                                    UserId = userId,
                                    CreatedBy = userId,
                                    CreatedDate = DateTime.Now
                                });
                            }
                        }
                    }
                }

                // 3. Batch save in SQL Server
                if (pgTransactions.Any())
                {
                    const int batchSize = 1000;
                    for (int i = 0; i < pgTransactions.Count; i += batchSize)
                    {
                        var batch = pgTransactions.Skip(i).Take(batchSize).ToList();
                        await _dbContext.tblARDetailViewFull.AddRangeAsync(batch);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                // 4. Update Group info matching tblCustomerGroups
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    UPDATE tblARDetailViewFull
                    SET CustGroup = g.CustGroup
                    FROM tblCustomerGroups g
                    WHERE tblARDetailViewFull.CUST = g.BVCustNo AND tblARDetailViewFull.UserId = {0}", userId);

                // 5. Update Group info matching tblCustomerGroupsRR
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    UPDATE tblARDetailViewFull
                    SET CustGroup = g.CustGroup
                    FROM tblCustomerGroupsRR g
                    WHERE tblARDetailViewFull.CUST = g.BVCustNo AND tblARDetailViewFull.UserId = {0}", userId);

                // 6. Ensure tblARDetailExtra placeholders exist
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO tblARDetailExtra (TransNo, OPCResolved, IgnoreGroup, BulkIDChecked, CreatedBy, CreatedDate)
                    SELECT DISTINCT f.TRANS_NO, 0, 0, 0, {0}, GETDATE()
                    FROM tblARDetailViewFull f
                    LEFT JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                    WHERE e.TransNo IS NULL AND f.UserId = {0}", userId);

                // 7. Update Customer open cache list as well (VBA parity)
                await UpdateCustomerCacheAsync(agingDate, userId);

                return true;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GenerateCommentReviewData Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"C:\Logs\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<List<CommentReviewSummaryRow>> GetCommentReviewSummaryAsync(int minDays, string groupCriteria, int userId)
        {
            try
            {
                _dbContext.Database.SetCommandTimeout(600);

                if (string.IsNullOrEmpty(groupCriteria))
                {
                    var otherCriteriaList = await _dbContext.tblTerritoryGroups
                        .Where(g => g.GroupName != "Other" && !string.IsNullOrEmpty(g.GroupCriteria))
                        .Select(g => g.GroupCriteria)
                        .ToListAsync();
                    if (otherCriteriaList.Any())
                    {
                        groupCriteria = "NOT (" + string.Join(" OR ", otherCriteriaList.Select(c => $"({c})")) + ")";
                    }
                    else
                    {
                        groupCriteria = "1=1";
                    }
                }

                // Formulate raw SQL statement executing dynamic groupCriteria filter
                string sql = $@"
                    SELECT 
                        COALESCE(f.CustGroup, f.CUST) AS GroupID,
                        MAX(o.SALES_TERR) AS MaxOfSALES_TERR,
                        MAX(CASE WHEN g.GroupName IS NOT NULL THEN g.GroupName ELSE o.CustName END) AS CustomerName,
                        MAX(CASE WHEN f.CustGroup IS NOT NULL THEN 'Group' ELSE 'Single' END) AS ARType,
                        COUNT(f.TRANS_NO) AS TransCount,
                        SUM(CASE WHEN f.Type IN ('I', 'C') THEN 1 ELSE 0 END) AS SumOfInvoiceCount,
                        SUM(CASE WHEN f.Type = 'P' THEN 1 ELSE 0 END) AS SumOfPaymentCount,
                        SUM(CASE WHEN e.FirstNoticeDate IS NOT NULL THEN 1 ELSE 0 END) AS SumOfFirstNoticeCount,
                        SUM(CASE WHEN e.SecondNoticeDate IS NOT NULL THEN 1 ELSE 0 END) AS SumOfSecondNoticeCount,
                        SUM(f.BALANCE) AS SumOfBALANCE,
                        CAST(MAX(CASE WHEN b.CustNo IS NOT NULL THEN 1 ELSE 0 END) AS BIT) AS BulkInvoice
                    FROM tblARDetailViewFull f
                    INNER JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                    INNER JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                    LEFT JOIN tblBulkCustomers b ON o.CUST = b.CustNo
                    LEFT JOIN tblCustomerGroups g ON o.CUST = g.BVCustNo AND f.CustGroup = g.CustGroup
                    WHERE f.UserId = @p0 
                      AND f.DaysOld >= @p1 
                      AND f.Type = 'I'
                      AND ({groupCriteria})
                    GROUP BY COALESCE(f.CustGroup, f.CUST)
                    HAVING SUM(f.BALANCE) > 0
                    ORDER BY CustomerName";

                var result = new List<CommentReviewSummaryRow>();
                var conn = _dbContext.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 600;

                    var pUserId = cmd.CreateParameter();
                    pUserId.ParameterName = "@p0";
                    pUserId.Value = userId;
                    cmd.Parameters.Add(pUserId);

                    var pMinDays = cmd.CreateParameter();
                    pMinDays.ParameterName = "@p1";
                    pMinDays.Value = minDays;
                    cmd.Parameters.Add(pMinDays);

                    try
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new CommentReviewSummaryRow
                                {
                                    GroupID = reader.GetString(0),
                                    MaxOfSALES_TERR = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    CustomerName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    ARType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    TransCount = reader.GetInt32(4),
                                    SumOfInvoiceCount = reader.GetInt32(5),
                                    SumOfPaymentCount = reader.GetInt32(6),
                                    SumOfFirstNoticeCount = reader.GetInt32(7),
                                    SumOfSecondNoticeCount = reader.GetInt32(8),
                                    SumOfBALANCE = reader.GetDecimal(9),
                                    BulkInvoice = reader.GetBoolean(10)
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"SQL Error in GetCommentReviewSummaryAsync: {ex.Message}. Query: {sql}");
                    }
                }
                if (!wasOpen) conn.Close();
                return result;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GetCommentReviewSummary Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"C:\Logs\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<ARCommentEvent?> GetSummaryCommentAsync(string custNo)
        {
            try
            {
                _dbContext.Database.SetCommandTimeout(600);

                var comment = await _dbContext.tblEvents
                    .Where(e => e.CustNo == custNo && e.EventType == 10)
                    .OrderByDescending(e => e.ModDate)
                    .Select(e => new ARCommentEvent
                    {
                        ID = e.ID,
                        EventType = e.EventType,
                        CustNo = e.CustNo,
                        CustType = e.CustType,
                        EventText = e.EventText,
                        AddDate = e.AddDate,
                        AddUser = e.AddUser,
                        ModDate = e.ModDate,
                        ModUser = e.ModUser
                    })
                    .FirstOrDefaultAsync();

                return comment;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GetSummaryComment Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"C:\Logs\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<bool> SaveSummaryCommentAsync(string custNo, string custType, string commentText, string initials, int userId)
        {
            try
            {
                _dbContext.Database.SetCommandTimeout(600);

                var existing = await _dbContext.tblEvents
                    .FirstOrDefaultAsync(e => e.CustNo == custNo && e.EventType == 10);

                if (existing != null)
                {
                    existing.EventText = commentText;
                    existing.ModDate = DateTime.Now;
                    existing.ModUser = initials;
                }
                else
                {
                    var newEvent = new TblEvents
                    {
                        EventType = 10,
                        CustNo = custNo,
                        CustType = custType,
                        EventText = commentText,
                        AddDate = DateTime.Now,
                        AddUser = initials,
                        ModDate = DateTime.Now,
                        ModUser = initials
                    };
                    await _dbContext.tblEvents.AddAsync(newEvent);
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== SaveSummaryComment Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"C:\Logs\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<byte[]> ExportSummaryCommentsAsync(int minDays, string groupCriteria, int userId)
        {
            try
            {
                _dbContext.Database.SetCommandTimeout(600);

                if (string.IsNullOrEmpty(groupCriteria))
                {
                    var otherCriteriaList = await _dbContext.tblTerritoryGroups
                        .Where(g => g.GroupName != "Other" && !string.IsNullOrEmpty(g.GroupCriteria))
                        .Select(g => g.GroupCriteria)
                        .ToListAsync();
                    if (otherCriteriaList.Any())
                    {
                        groupCriteria = "NOT (" + string.Join(" OR ", otherCriteriaList.Select(c => $"({c})")) + ")";
                    }
                    else
                    {
                        groupCriteria = "1=1";
                    }
                }

                string summarySql = $@"
                    SELECT 
                        MAX(o.SALES_TERR) AS MaxOfSALES_TERR,
                        MAX(CASE WHEN f.CustGroup IS NOT NULL THEN 'Group' ELSE 'Single' END) AS ARType,
                        COALESCE(f.CustGroup, f.CUST) AS GroupID,
                        MAX(CASE WHEN g.GroupName IS NOT NULL THEN g.GroupName ELSE o.CustName END) AS CustomerName,
                        MAX(ev.EventType) AS EventType,
                        MAX(ev.EventText) AS EventText,
                        MAX(ev.ModDate) AS ModDate,
                        MAX(ev.ModUser) AS ModUser
                    FROM tblARDetailViewFull f
                    INNER JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                    INNER JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                    LEFT JOIN tblBulkCustomers b ON o.CUST = b.CustNo
                    LEFT JOIN tblCustomerGroups g ON o.CUST = g.BVCustNo AND f.CustGroup = g.CustGroup
                    INNER JOIN tblEvents ev ON COALESCE(f.CustGroup, f.CUST) = ev.CustNo AND ev.EventType = 10
                    WHERE f.UserId = @userId 
                      AND f.DaysOld >= @minDays 
                      AND f.Type = 'I'
                      AND ({groupCriteria})
                    GROUP BY COALESCE(f.CustGroup, f.CUST)
                    HAVING SUM(f.BALANCE) > 0
                    ORDER BY CustomerName";

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Summary Comments");
                    string[] headers = new string[] {
                        "Territory", "ARType", "GroupID", "CustomerName", "EventType", "EventText", "ModDate", "ModUser"
                    };

                    // Headers
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[1, i + 1].Value = headers[i];
                    }
                    using (var range = ws.Cells[1, 1, 1, headers.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    int currentRow = 2;
                    var conn = _dbContext.Database.GetDbConnection();
                    var wasOpen = conn.State == ConnectionState.Open;
                    if (!wasOpen) await conn.OpenAsync();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = summarySql;
                        cmd.CommandTimeout = 600;

                        var pUserId = cmd.CreateParameter();
                        pUserId.ParameterName = "@userId";
                        pUserId.Value = userId;
                        cmd.Parameters.Add(pUserId);

                        var pMinDays = cmd.CreateParameter();
                        pMinDays.ParameterName = "@minDays";
                        pMinDays.Value = minDays;
                        cmd.Parameters.Add(pMinDays);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ws.Cells[currentRow, 1].Value = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                ws.Cells[currentRow, 2].Value = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                ws.Cells[currentRow, 3].Value = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                ws.Cells[currentRow, 4].Value = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                ws.Cells[currentRow, 5].Value = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                                ws.Cells[currentRow, 6].Value = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                ws.Cells[currentRow, 7].Value = reader.IsDBNull(6) ? "" : reader.GetDateTime(6).ToString("MM/dd/yyyy");
                                ws.Cells[currentRow, 8].Value = reader.IsDBNull(7) ? "" : reader.GetString(7);
                                currentRow++;
                            }
                        }
                    }
                    if (!wasOpen) conn.Close();

                    // Auto Filter
                    if (currentRow > 2)
                    {
                        ws.Cells[1, 1, currentRow - 1, headers.Length].AutoFilter = true;
                    }

                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== ExportSummaryComments Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"C:\Logs\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        #endregion

        #region AR Reporting

        public async Task<bool> GenerateAgingDataAsync(DateTime lastReportDate, DateTime startDate, DateTime endDate, int userId)
        {
            try
            {
                var sqlConn = (Microsoft.Data.SqlClient.SqlConnection)_dbContext.Database.GetDbConnection();
                if (sqlConn.State != System.Data.ConnectionState.Open) await sqlConn.OpenAsync();

                // 1. Sync gl_transactions to dbo_WWGL_TRANSARPMTS
                string strLastTrans = "0";
                using (var cmd = sqlConn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "SELECT ISNULL(MAX(trans_no), '0') FROM dbo_WWGL_TRANSARPMTS";
                    var res = await cmd.ExecuteScalarAsync();
                    if (res != null && res != DBNull.Value) strLastTrans = res.ToString();
                }

                var dtGL = new DataTable();
                using (var pgConn = new NpgsqlConnection(_pgConn))
                {
                    await pgConn.OpenAsync();
                    string pgSql = @"SELECT division, account_no AS acct_no, currency AS gl_currency, to_char(date, 'YYYYMMDD') AS tran_date, 
                                     trans_no, recno, where_from, gl_user, mf_who, mf_key, mf_key AS bvglmemokey, mf_tran AS bvglmemotran, mf_tran, reconcile_flag, 
                                     to_char(post_date, 'YYYYMMDD') AS postdate, debit_amt, credit_amt, source_dept 
                                     FROM gl_transactions 
                                     WHERE trans_no > @lastTrans AND where_from = 'AR' AND (mf_who = 'Cust.' OR mf_who = 'Void Pmt.')";
                    using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                    {
                        pgCmd.CommandTimeout = 600;
                        pgCmd.Parameters.AddWithValue("lastTrans", strLastTrans);
                        using (var reader = await pgCmd.ExecuteReaderAsync())
                            dtGL.Load(reader);
                    }
                }

                if (dtGL.Rows.Count > 0)
                {
                    dtGL.Columns.Add("CreatedBy", typeof(int)).DefaultValue = userId;
                    dtGL.Columns.Add("CreatedDate", typeof(DateTime)).DefaultValue = DateTime.Now;
                    dtGL.Columns.Add("ModifiedBy", typeof(int)).DefaultValue = userId;
                    dtGL.Columns.Add("ModifiedDate", typeof(DateTime)).DefaultValue = DateTime.Now;

                    using (var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(sqlConn))
                    {
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = "dbo_WWGL_TRANSARPMTS";
                        foreach (DataColumn col in dtGL.Columns) bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        await bulkCopy.WriteToServerAsync(dtGL);
                    }
                }

                // 2. Clear tblPaymentsTEMPOnly and tblPaymentsTEMP
                using (var cmd = sqlConn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "TRUNCATE TABLE tblPaymentsTEMPOnly; TRUNCATE TABLE tblPaymentsTEMP;";
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Extract MakeTblPaymentsTempOnly logic directly
                var dtTemp = new DataTable();
                using (var pgConn = new NpgsqlConnection(_pgConn))
                {
                    if (pgConn.State != System.Data.ConnectionState.Open) await pgConn.OpenAsync();
                    string pgSqlTemp = @"
                        WITH LowestLink AS (
                            SELECT t.id, MIN(t1.id) AS lowest_link_id
                            FROM ar_transactions t
                            INNER JOIN ar_transaction_links l ON t.id = l.credit_id
                            INNER JOIN ar_transactions t1 ON l.debit_id = t1.id
                            WHERE t1.code = 'I'
                            GROUP BY t.id
                        ),
                        TempOnly AS (
                            SELECT t.id AS ""ARID"", t.cust_no AS ""CUST"", t.code AS ""code"", t.date::timestamp AS ""CreditDate"", to_char(t.date, 'YYYYMMDD') AS ""arf_date"",
                                   t.trans_no AS ""trans_no"", t.ref_no AS ""ref_no"", t.open_close_flag AS ""open_close_flag"",
                                   CASE WHEN t.credit_amt <> 0 THEN t.credit_amt ELSE t.debit_amt * -1 END AS ""CreditAmt"",
                                   a.sales_terr AS ""sales_terr"", c.name AS ""CustomerName"", t._created_by AS ""BVUser"",
                                   ll.lowest_link_id AS ""lowestlink"", t.date::timestamp AS ""j_date""
                            FROM ar_transactions t
                            INNER JOIN customers c ON t.cust_no = c.cust_no
                            INNER JOIN addresses a ON c.cust_no = a.link_no
                            LEFT JOIN LowestLink ll ON t.id = ll.id
                            WHERE t.code = 'P' AND t.date > @startDate AND t.date <= @endDate AND a.addr_type = 'B' AND a.link_table = 'CUST'
                        ),
                        ARLinksAgg AS (
                            SELECT credit_id, debit_id, SUM(applied_amt) AS ""SumOfapplied_amt""
                            FROM ar_transaction_links
                            GROUP BY credit_id, debit_id
                        )
                        SELECT 
                            to1.""ARID"", to1.""CUST"", to1.""trans_no"" AS ""FOLIO"", to1.""code"" AS ""CODE"", to1.""arf_date"" AS ""ARF_DATE"",
                            to1.""trans_no"" AS ""TRANS_NO"", to1.""ref_no"" AS ""REF_NO"", to1.""open_close_flag"" AS ""OPEN_CLOSE_FLAG"",
                            to1.""j_date"" AS ""J_DATE"", al.""SumOfapplied_amt"" AS ""CreditAmt"", to1.""CreditDate"",
                            pt.date::timestamp AS ""LinkDate"", 
                            EXTRACT(DAY FROM (@lastReportDate - COALESCE(pt.date::timestamp, to1.""CreditDate""))) AS ""DaysLastReport"",
                            to1.""sales_terr"" AS ""SALES_TERR"", pt.ref_no AS ""Invoice"", pt.debit_amt AS ""InvoiceAmount"",
                            to1.""CustomerName"", to1.""BVUser"", to1.""lowestlink"" AS ""LowestLink"", pt.trans_no AS ""LinkFolio""
                        FROM TempOnly to1
                        LEFT JOIN ARLinksAgg al ON to1.""ARID"" = al.credit_id
                        LEFT JOIN ar_transactions pt ON al.debit_id = pt.id
                        ORDER BY to1.""ARID""";

                    using (var pgCmd = new NpgsqlCommand(pgSqlTemp, pgConn))
                    {
                        pgCmd.CommandTimeout = 600;
                        pgCmd.Parameters.AddWithValue("startDate", startDate.AddDays(-1));
                        pgCmd.Parameters.AddWithValue("endDate", endDate);
                        pgCmd.Parameters.AddWithValue("lastReportDate", lastReportDate);
                        using (var reader = await pgCmd.ExecuteReaderAsync())
                            dtTemp.Load(reader);
                    }
                }

                // 4. Update GL, CTN, IMEI, FFF from SQL Server Tables
                if (dtTemp.Rows.Count > 0)
                {
                    dtTemp.Columns.Add("DRAccount", typeof(string));
                    dtTemp.Columns.Add("DRAmount", typeof(decimal));
                    dtTemp.Columns.Add("CRAccount", typeof(string));
                    dtTemp.Columns.Add("CRAmount", typeof(decimal));
                    dtTemp.Columns.Add("IMEI", typeof(string));
                    dtTemp.Columns.Add("CTN", typeof(string));
                    dtTemp.Columns.Add("IMEICount", typeof(int));
                    dtTemp.Columns.Add("FFFIMEI", typeof(string));
                    dtTemp.Columns.Add("FFFCTN", typeof(string));
                    dtTemp.Columns.Add("FFFWebID", typeof(string));
                    dtTemp.Columns.Add("FFFARTotal", typeof(decimal));
                    dtTemp.Columns.Add("CreatedBy", typeof(int));
                    dtTemp.Columns.Add("CreatedDate", typeof(DateTime));
                    dtTemp.Columns.Add("ModifiedBy", typeof(int));
                    dtTemp.Columns.Add("ModifiedDate", typeof(DateTime));
                    dtTemp.Columns.Add("Current", typeof(decimal));
                    dtTemp.Columns.Add("30Days", typeof(decimal));
                    dtTemp.Columns.Add("60Days", typeof(decimal));
                    dtTemp.Columns.Add("90Days", typeof(decimal));
                    dtTemp.Columns.Add("120Days", typeof(decimal));

                    DateTime now = DateTime.Now;
                    foreach (DataRow row in dtTemp.Rows)
                    {
                        row["CreatedBy"] = userId;
                        row["CreatedDate"] = now;
                        row["ModifiedBy"] = userId;
                        row["ModifiedDate"] = now;
                        row["Current"] = 0m;
                        row["30Days"] = 0m;
                        row["60Days"] = 0m;
                        row["90Days"] = 0m;
                        row["120Days"] = 0m;
                    }

                    var invoices = dtTemp.AsEnumerable().Select(r => r.Field<string>("Invoice")).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
                    var transNos = dtTemp.AsEnumerable().Select(r => r.Field<string>("TRANS_NO")).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();

                    var glMap = new Dictionary<string, dynamic>();
                    for (int i = 0; i < transNos.Count; i += 1000)
                    {
                        var chunk = transNos.Skip(i).Take(1000).ToList();
                        if (!chunk.Any()) break;
                        string inClause = string.Join(",", chunk.Select(t => $"'{t.Replace("'", "''")}'"));
                        using (var cmd = sqlConn.CreateCommand())
                        {
                            cmd.CommandTimeout = 600;
                            cmd.CommandText = $"SELECT trans_no, MAX(debit_amt) as MaxOfDEBIT_AMT, MAX(credit_amt) as MaxOfCREDIT_AMT, MAX(CASE WHEN debit_amt <> 0 THEN acct_no END) as Debit_account, MAX(CASE WHEN credit_amt <> 0 THEN acct_no END) as Credit_account FROM dbo_WWGL_TRANSARPMTS WHERE trans_no IN ({inClause}) AND division >= '000' GROUP BY trans_no";
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    glMap[reader.GetString(0)] = new
                                    {
                                        MaxOfDEBIT_AMT = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                                        MaxOfCREDIT_AMT = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2),
                                        Debit_account = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        Credit_account = reader.IsDBNull(4) ? null : reader.GetString(4)
                                    };
                                }
                            }
                        }
                    }

                    var saMap = new Dictionary<string, dynamic>();
                    for (int i = 0; i < invoices.Count; i += 1000)
                    {
                        var chunk = invoices.Skip(i).Take(1000).ToList();
                        if (!chunk.Any()) break;
                        string inClause = string.Join(",", chunk.Select(t => $"'{t.Replace("'", "''")}'"));
                        using (var cmd = sqlConn.CreateCommand())
                        {
                            cmd.CommandTimeout = 600;
                            cmd.CommandText = $"SELECT Invoice, COUNT(1) as InvCount, SUM(CASE WHEN IMEIESN IS NOT NULL THEN 1 ELSE 0 END) as IMEICount, MAX(IMEIESN) as FirstIMEI, MAX(CellPhoneNo) as FirstCTN FROM SalesActivations WHERE Invoice IN ({inClause}) GROUP BY Invoice";
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    saMap[reader.GetString(0)] = new
                                    {
                                        InvCount = reader.GetInt32(1),
                                        IMEICount = reader.GetInt32(2),
                                        FirstIMEI = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        FirstCTN = reader.IsDBNull(4) ? null : reader.GetString(4)
                                    };
                                }
                            }
                        }
                    }

                    var fffMap = new Dictionary<string, dynamic>();
                    for (int i = 0; i < invoices.Count; i += 1000)
                    {
                        var chunk = invoices.Skip(i).Take(1000).ToList();
                        if (!chunk.Any()) break;
                        string inClause = string.Join(",", chunk.Select(t => $"'{t.Replace("'", "''")}'"));
                        using (var cmd = sqlConn.CreateCommand())
                        {
                            cmd.CommandTimeout = 600;
                            cmd.CommandText = $"SELECT Invoice, SUM(ARAmount) as SumOfARAmount, MAX(IMEIESN) as MaxOfIMEIESN, MAX(WebOrderID) as MaxOfWebOrderID, MAX(CellPhoneNo) as MaxOfCellPhoneNo FROM dbo.FFFClaimsMaster2 WHERE Invoice IN ({inClause}) GROUP BY Invoice";
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    fffMap[reader.GetString(0)] = new
                                    {
                                        SumOfARAmount = reader.IsDBNull(1)
    ? 0m
    : Convert.ToDecimal(reader.GetValue(1)),
                                        MaxOfIMEIESN = reader.IsDBNull(2) ? null : reader.GetString(2),
                                        MaxOfWebOrderID = reader.IsDBNull(3) ? null : reader.GetString(3),
                                        MaxOfCellPhoneNo = reader.IsDBNull(4) ? null : reader.GetString(4)
                                    };
                                }
                            }
                        }
                    }

                    foreach (DataRow row in dtTemp.Rows)
                    {
                        var tNo = row["TRANS_NO"]?.ToString();
                        if (!string.IsNullOrEmpty(tNo) && glMap.TryGetValue(tNo, out var gl))
                        {
                            row["DRAccount"] = gl.Debit_account;
                            row["CRAccount"] = gl.Credit_account;
                            row["DRAmount"] = gl.MaxOfDEBIT_AMT;
                            row["CRAmount"] = gl.MaxOfCREDIT_AMT;
                        }

                        var inv = row["Invoice"]?.ToString();
                        if (!string.IsNullOrEmpty(inv))
                        {
                            if (saMap.TryGetValue(inv, out var sa))
                            {
                                row["IMEI"] = sa.FirstIMEI;
                                row["CTN"] = sa.FirstCTN;
                                row["IMEICount"] = sa.IMEICount;
                            }
                            if (fffMap.TryGetValue(inv, out var fff))
                            {
                                row["FFFIMEI"] = fff.MaxOfIMEIESN;
                                row["FFFCTN"] = fff.MaxOfCellPhoneNo;
                                row["FFFWebID"] = fff.MaxOfWebOrderID;
                                row["FFFARTotal"] = fff.SumOfARAmount;
                            }
                        }
                    }

                    using (var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(sqlConn))
                    {
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = "tblPaymentsTEMP";
                        foreach (DataColumn col in dtTemp.Columns) bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        await bulkCopy.WriteToServerAsync(dtTemp);
                    }
                }

                // 5. UpdateCustomerList and Rebuild tblChannelIDLink
                await UpdateCustomerCacheAsync(lastReportDate, userId);

                using (var cmd = sqlConn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "DELETE FROM tblChannelIDLink";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = @"
                        INSERT INTO tblChannelIDLink (Territory, ChannelID)
                        SELECT c.SALES_TERR, t.ID 
                        FROM tblCustomersOpen c
                        CROSS JOIN tblTerritoryGroups t
                        WHERE t.GroupCriteria IS NOT NULL 
                          AND (
                             (t.GroupCriteria IS NULL AND c.SALES_TERR = '') OR 
                             (t.GroupCriteria IS NOT NULL) 
                          )
                        GROUP BY c.SALES_TERR, t.ID";
                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GenerateAgingData Error ({DateTime.Now}) ===\nError: {ex.Message}\nInner Exception: {innerMsg}\nStack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<byte[]> ExportAgedSummaryAsync(int userId)
        {
            try
            {
                var data = await GetAgedSummaryDataAsync(userId);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Aged Summary");
                    int row = 1;
                    int colCount = 6;
                    string[] headers = { "AccountType", "SumOfCurrent", "SumOf30Days", "SumOf60Days", "SumOf90Days", "SumOf120Days" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[row, i + 1].Value = headers[i];
                        ws.Cells[row, i + 1].Style.Font.Bold = true;
                    }
                    row++;

                    foreach (var r in data)
                    {
                        var dict = (Dictionary<string, object>)r;
                        int c = 1;
                        foreach (var key in headers)
                        {
                            ws.Cells[row, c++].Value = dict.ContainsKey(key) ? dict[key] : null;
                        }
                        row++;
                    }
                    if (row > 2 && colCount > 0)
                    {
                        ws.Cells[1, 1, row - 1, colCount].AutoFilter = true;
                    }
                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== ExportAgedSummary Error ({DateTime.Now}) ===\nError: {ex.Message}\nInner Exception: {innerMsg}\nStack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<bool> GenerateARMasterDataAsync(DateTime agingDate, int userId)
        {
            try
            {
                var sqlConn = (Microsoft.Data.SqlClient.SqlConnection)_dbContext.Database.GetDbConnection();
                if (sqlConn.State != System.Data.ConnectionState.Open) await sqlConn.OpenAsync();

                // 1. Clear and Load tblARDetailViewFull from Postgres
                using (var cmd = sqlConn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "DELETE FROM tblARDetailViewFull WHERE UserId = @userId";
                    cmd.Parameters.AddWithValue("@userId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                var dtMaster = new DataTable();
                using (var pgConn = new NpgsqlConnection(_pgConn))
                {
                    await pgConn.OpenAsync();
                    string pgSql = @"SELECT b.id AS id_bal, b.cust_no AS cust_no_bal, b.age AS age_bal, b.balance AS balance_bal, 
                                            t.code AS ""CODE"", t.trans_no AS ""TRANS_NO"", t.ref_no AS ""REF_NO"", t.date::timestamp AS ""TranDate"", 
                                            t.cust_no AS ""CUST"", b.balance AS ""BALANCE"",
                                            EXTRACT(DAY FROM (@agingDate - t.date)) AS ""DaysOld""
                                     FROM ar_transaction_balances_at_date(@agingDate::date) b
                                     LEFT JOIN ar_transactions t ON b.id = t.id
                                     WHERE b.balance != 0";
                    using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                    {
                        pgCmd.CommandTimeout = 600;
                        pgCmd.Parameters.AddWithValue("agingDateStr", agingDate.ToString("yyyyMMdd"));
                        pgCmd.Parameters.AddWithValue("agingDate", agingDate);
                        using (var reader = await pgCmd.ExecuteReaderAsync())
                            dtMaster.Load(reader);
                    }
                }

                if (dtMaster.Rows.Count > 0)
                {
                    dtMaster.Columns.Add("UserId", typeof(int));
                    dtMaster.Columns.Add("CreatedBy", typeof(int));
                    dtMaster.Columns.Add("CreatedDate", typeof(DateTime));
                    dtMaster.Columns.Add("ModifiedBy", typeof(int));
                    dtMaster.Columns.Add("ModifiedDate", typeof(DateTime));

                    DateTime now = DateTime.Now;
                    foreach (DataRow row in dtMaster.Rows)
                    {
                        row["UserId"] = userId;
                        row["CreatedBy"] = userId;
                        row["CreatedDate"] = now;
                        row["ModifiedBy"] = userId;
                        row["ModifiedDate"] = now;
                    }
                    using (var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(sqlConn))
                    {
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = "tblARDetailViewFull";
                        foreach (DataColumn col in dtMaster.Columns) bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        await bulkCopy.WriteToServerAsync(dtMaster);
                    }
                }

                // 2. Clear and Load tblActivationsLookupFULL
                using (var cmd = sqlConn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "TRUNCATE TABLE tblActivationsLookupFULL";
                    await cmd.ExecuteNonQueryAsync();
                }

                var activeInvoices = dtMaster.AsEnumerable().Select(r => r.Field<string>("REF_NO")).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
                if (activeInvoices.Any())
                {
                    var dtLookup = new DataTable();
                    dtLookup.Columns.Add("Invoice", typeof(string));
                    dtLookup.Columns.Add("InvoiceDate", typeof(DateTime));
                    dtLookup.Columns.Add("MaxOfID", typeof(int));
                    dtLookup.Columns.Add("Customer", typeof(string));
                    dtLookup.Columns.Add("ActivationsTerritory", typeof(string));
                    dtLookup.Columns.Add("MSD", typeof(string));
                    dtLookup.Columns.Add("WebOrderID", typeof(string));
                    dtLookup.Columns.Add("CustomerPostal", typeof(string));
                    dtLookup.Columns.Add("ShipToPostal", typeof(string));
                    dtLookup.Columns.Add("CostBudgetCode", typeof(string));
                    dtLookup.Columns.Add("CustomerPONo", typeof(string));
                    dtLookup.Columns.Add("UserName", typeof(string));
                    dtLookup.Columns.Add("CellPhoneNo", typeof(string));
                    dtLookup.Columns.Add("CountGovChannel", typeof(int));
                    dtLookup.Columns.Add("CountGovFee", typeof(int));
                    dtLookup.Columns.Add("CreatedBy", typeof(int));
                    dtLookup.Columns.Add("CreatedDate", typeof(DateTime));
                    dtLookup.Columns.Add("ModifiedBy", typeof(int));
                    dtLookup.Columns.Add("ModifiedDate", typeof(DateTime));

                    for (int i = 0; i < activeInvoices.Count; i += 1000)
                    {
                        var chunk = activeInvoices.Skip(i).Take(1000).ToList();
                        if (!chunk.Any()) break;
                        string inClause = string.Join(",", chunk.Select(t => $"'{t.Replace("'", "''")}'"));

                        using (var cmd = sqlConn.CreateCommand())
                        {
                            cmd.CommandTimeout = 600;
                            cmd.CommandText = $@"
                                SELECT Invoice, InvoiceDate, MAX(Id) as MaxOfID, MAX(Customer) as Customer, 
                                       MAX(CustTerritory) as ActivationsTerritory, MAX(MSD) as MSD, MAX(WebOrderID) as WebOrderID, 
                                       MAX(CustomerPostal) as CustomerPostal, MAX(ShipToPostal) as ShipToPostal, 
                                       MAX(CostBudgetCode) as CostBudgetCode, MAX(CustomerPONo) as CustomerPONo, 
                                       MAX(UserName) as UserName, MAX(CellPhoneNo) as CellPhoneNo, 
                                       SUM(CASE WHEN Channel = 'Government' THEN 1 ELSE 0 END) as CountGovChannel, 
                                       SUM(CASE WHEN FeeType LIKE '%GOV%' THEN 1 ELSE 0 END) as CountGovFee
                                FROM SalesActivations 
                                WHERE Invoice IN ({inClause}) 
                                GROUP BY Invoice, InvoiceDate";

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    dtLookup.Rows.Add(
                                        reader.IsDBNull(0) ? (object)DBNull.Value : reader.GetString(0),
                                        reader.IsDBNull(1) ? (object)DBNull.Value : reader.GetDateTime(1),
                                        reader.IsDBNull(2) ? (object)0 : reader.GetInt32(2),
                                        reader.IsDBNull(3) ? (object)DBNull.Value : reader.GetString(3),
                                        reader.IsDBNull(4) ? (object)DBNull.Value : reader.GetString(4),
                                        reader.IsDBNull(5) ? (object)DBNull.Value : reader.GetString(5),
                                        reader.IsDBNull(6) ? (object)DBNull.Value : reader.GetString(6),
                                        reader.IsDBNull(7) ? (object)DBNull.Value : reader.GetString(7),
                                        reader.IsDBNull(8) ? (object)DBNull.Value : reader.GetString(8),
                                        reader.IsDBNull(9) ? (object)DBNull.Value : reader.GetString(9),
                                        reader.IsDBNull(10) ? (object)DBNull.Value : reader.GetString(10),
                                        reader.IsDBNull(11) ? (object)DBNull.Value : reader.GetString(11),
                                        reader.IsDBNull(12) ? (object)DBNull.Value : reader.GetString(12),
                                        reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                                        reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                                        userId,
                                        DateTime.Now,
                                        userId,
                                        DateTime.Now
                                    );
                                }
                            }
                        }
                    }

                    if (dtLookup.Rows.Count > 0)
                    {
                        using (var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(sqlConn))
                        {
                            bulkCopy.BulkCopyTimeout = 600;
                            bulkCopy.DestinationTableName = "tblActivationsLookupFULL";
                            foreach (DataColumn col in dtLookup.Columns) bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            await bulkCopy.WriteToServerAsync(dtLookup);
                        }
                    }
                }

                // 3. UpdateCustomerList and Rebuild tblChannelIDLink
                await UpdateCustomerCacheAsync(agingDate, userId);

                using (var cmd = sqlConn.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "DELETE FROM tblChannelIDLink";
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = @"
                        INSERT INTO tblChannelIDLink (Territory, ChannelID)
                        SELECT c.SALES_TERR, t.ID 
                        FROM tblCustomersOpen c
                        CROSS JOIN tblTerritoryGroups t
                        WHERE t.GroupCriteria IS NOT NULL 
                          AND (
                             (t.GroupCriteria IS NULL AND c.SALES_TERR = '') OR 
                             (t.GroupCriteria IS NOT NULL) 
                          )
                        GROUP BY c.SALES_TERR, t.ID";
                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GenerateARMasterData Error ({DateTime.Now}) ===\nError: {ex.Message}\nInner Exception: {innerMsg}\nStack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<byte[]> ExportARMasterAsync(int userId)
        {
            try
            {
                var data = await GetARMasterExportDataAsync(userId, false);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("AR Master");
                    int row = 1;
                    int colCount = 0;
                    foreach (var r in data)
                    {
                        var dict = (Dictionary<string, object>)r;
                        if (row == 1)
                        {
                            int col = 1;
                            colCount = dict.Keys.Count;
                            foreach (var key in dict.Keys) ws.Cells[1, col++].Value = key;
                            row++;
                        }
                        int c = 1;
                        foreach (var val in dict.Values)
                        {
                            if (val != null && val.ToString() == "EMPTY_MARKER") { c++; continue; }
                            ws.Cells[row, c++].Value = val;
                        }
                        row++;
                    }
                    if (row > 2 && colCount > 0)
                    {
                        ws.Cells[1, 1, row - 1, colCount].AutoFilter = true;
                    }
                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== ExportARMaster Error ({DateTime.Now}) ===\nError: {ex.Message}\nInner Exception: {innerMsg}\nStack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<byte[]> ExportARMasterAllAsync(int userId)
        {
            try
            {
                // In VBA this exports all records
                var data = await GetARMasterExportDataAsync(userId, true);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("AR Master ALL");
                    int row = 1;
                    int colCount = 0;
                    foreach (var r in data)
                    {
                        var dict = (Dictionary<string, object>)r;
                        if (row == 1)
                        {
                            int col = 1;
                            colCount = dict.Keys.Count;
                            foreach (var key in dict.Keys) ws.Cells[1, col++].Value = key;
                            row++;
                        }
                        int c = 1;
                        foreach (var val in dict.Values)
                        {
                            if (val != null && val.ToString() == "EMPTY_MARKER") { c++; continue; }
                            ws.Cells[row, c++].Value = val;
                        }
                        row++;
                    }
                    if (row > 2 && colCount > 0)
                    {
                        ws.Cells[1, 1, row - 1, colCount].AutoFilter = true;
                    }
                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== ExportARMasterAll Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<byte[]> ExportARMasterSummaryAsync(int userId)
        {
            try
            {
                // Summary group by Code, Cat
                var data = await GetARMasterSummaryExportDataAsync(userId);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("AR Master Summary");
                    int row = 1;
                    int colCount = 0;
                    foreach (var r in data)
                    {
                        var dict = (Dictionary<string, object>)r;
                        if (row == 1)
                        {
                            int col = 1;
                            colCount = dict.Keys.Count;
                            foreach (var key in dict.Keys) ws.Cells[1, col++].Value = key;
                            row++;
                        }
                        int c = 1;
                        foreach (var val in dict.Values)
                        {
                            if (val != null && val.ToString() == "EMPTY_MARKER") { c++; continue; }
                            ws.Cells[row, c++].Value = val;
                        }
                        row++;
                    }
                    if (row > 2 && colCount > 0)
                    {
                        ws.Cells[1, 1, row - 1, colCount].AutoFilter = true;
                    }
                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== ExportARMasterSummary Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetAgedSummaryDataAsync(int userId)
        {
            try
            {
                string sql = @"
                    SELECT 
                        COALESCE(r2.AcctType, r1.AcctType, t.RogersReportingName) AS AccountType, 
                        SUM(p.[Current]) AS SumOfCurrent, 
                        SUM(p.[30Days]) AS SumOf30Days, 
                        SUM(p.[60Days]) AS SumOf60Days, 
                        SUM(p.[90Days]) AS SumOf90Days, 
                        SUM(p.[120Days]) AS SumOf120Days
                    FROM tblPaymentsTEMP p
                    INNER JOIN tblChannelIDLink c ON p.SALES_TERR = c.Territory
                    INNER JOIN tblTerritoryGroups t ON c.ChannelID = t.ID
                    LEFT JOIN tblCustomerGroupsRR cg ON p.CUST = cg.BVCustNo
                    LEFT JOIN tblRRAcctTypeExceptions r1 ON cg.CustGroup = r1.RRGroupNo
                    LEFT JOIN tblRRAcctTypeExceptions r2 ON p.CUST = r2.BVCustNo
                    WHERE t.RogersReporting = 1
                    GROUP BY COALESCE(r2.AcctType, r1.AcctType, t.RogersReportingName)
                ";
                var result = new List<Dictionary<string, object>>();
                var conn = _dbContext.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 600;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            result.Add(row);
                        }
                    }
                }
                if (!wasOpen) conn.Close();
                return result;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GetAgedSummaryData Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<byte[]> ExportPaymentDetailsAsync(int userId)
        {
            try
            {
                var data = await GetPaymentDetailsDataAsync(userId);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Payment Details");
                    int row = 1;
                    int colCount = 0;
                    foreach (var r in data)
                    {
                        var dict = (Dictionary<string, object>)r;
                        if (row == 1)
                        {
                            int col = 1;
                            colCount = dict.Keys.Count;
                            foreach (var key in dict.Keys) ws.Cells[1, col++].Value = key;
                            row++;
                        }
                        int c = 1;
                        foreach (var val in dict.Values) ws.Cells[row, c++].Value = val;
                        row++;
                    }
                    if (row > 2 && colCount > 0)
                    {
                        ws.Cells[1, 1, row - 1, colCount].AutoFilter = true;
                    }
                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== ExportPaymentDetails Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetPaymentDetailsDataAsync(int userId)
        {
            try
            {
                var glAccounts = new Dictionary<string, string>();
                using (var pgConn = new NpgsqlConnection(_pgConn))
                {
                    await pgConn.OpenAsync();
                    using (var pgCmd = new NpgsqlCommand("SELECT account_no, name FROM gl_accounts", pgConn))
                    using (var pgReader = await pgCmd.ExecuteReaderAsync())
                    {
                        while (await pgReader.ReadAsync())
                            glAccounts[pgReader.GetString(0)] = pgReader.GetString(1);
                    }
                }

                string sql = @"
                    SELECT p.SALES_TERR, p.CUST, p.FOLIO, p.CODE, p.ARF_DATE, p.TRANS_NO, p.Invoice, p.InvoiceAmount, p.REF_NO, p.OPEN_CLOSE_FLAG, p.CreditDate, p.LinkDate, p.DaysLastReport, p.CreditAmt, p.[Current], p.[30Days], p.[60Days], p.[90Days], p.[120Days], p.DRAccount, '' AS DRAcctName, p.DRAmount, p.CRAccount, '' AS CRAcctName, p.CRAmount, p.CustomerName, p.BVUser, p.CTN, p.IMEI, p.IMEICount, p.FFFIMEI, p.FFFCTN, p.FFFWebID, p.FFFARTotal
                    FROM tblPaymentsTEMP p
                    ORDER BY p.SALES_TERR, p.CUST, p.ARF_DATE, p.TRANS_NO
                ";
                var result = new List<Dictionary<string, object>>();
                var conn = _dbContext.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 600;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            var drAcc = row["DRAccount"]?.ToString();
                            var crAcc = row["CRAccount"]?.ToString();
                            row["DRAcctName"] = !string.IsNullOrEmpty(drAcc) && glAccounts.TryGetValue(drAcc, out var drName) ? drName : drAcc;
                            row["CRAcctName"] = !string.IsNullOrEmpty(crAcc) && glAccounts.TryGetValue(crAcc, out var crName) ? crName : crAcc;

                            result.Add(row);
                        }
                    }
                }
                if (!wasOpen) conn.Close();
                return result;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GetPaymentDetailsData Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        private async Task<IEnumerable<object>> GetARMasterExportDataAsync(int userId, bool isAll)
        {
            string sql = $@"
                SELECT 
                    '' AS BAN,
                    f.CODE as Code,
                    {(isAll ? "f.TRANS_NO as TransNo," : "")}
                    f.REF_NO as [Inv#], 
                    f.TranDate as [Date], 
                    f.CUST as CustNo, 
                    o.CustName as Name,
                    o.SALES_TERR as BVTerr,
                    o.BVCOCONTACT1NAME as BVContact,
                    o.BVCOCONTACT1TEL1 as BVTel,
                    NULL as MSD,
                    NULL as ShipToRegion,
                    f.BALANCE as BALANCE, 
                    CASE WHEN f.DaysOld < 30 THEN f.BALANCE ELSE 0 END as [Current],
                    CASE WHEN f.DaysOld >= 30 AND f.DaysOld < 60 THEN f.BALANCE ELSE 0 END as [30Days],
                    CASE WHEN f.DaysOld >= 60 AND f.DaysOld < 90 THEN f.BALANCE ELSE 0 END as [60Days],
                    CASE WHEN f.DaysOld >= 90 AND f.DaysOld < 120 THEN f.BALANCE ELSE 0 END as [90Days],
                    CASE WHEN f.DaysOld >= 120 THEN f.BALANCE ELSE 0 END as [120Days],
                    f.DaysOld as Days,
                    e.FirstNoticeDate, 
                    e.SecondNoticeDate,
                    NULL as BillToRegion,
                    NULL as [Terr-Inv],
                    e.RootCauseID as Root,
                    CASE WHEN f.DaysOld >= 90 THEN 13 ELSE NULL END as [Next],
                    COALESCE(r2.accttype, r1.accttype, t.RogersReportingName) AS AccountType,
                    NULL as WebOrderID,
                    YEAR(f.TranDate) as Yr,
                    NULL as GrpID,
                    NULL as GrpName,
                    NULL as Aging,
                    NULL as Aging1,
                    CASE WHEN f.DaysOld >= 90 THEN 'Escalation - Rogers Prime' ELSE 'Working to Collect' END as Category,
                    rc.Description as [Root Cause Description],
                    COALESCE(cg.CustGroup, f.CUST) AS CommentLink,
                    e.OPCResolved, 
                    e.OPCDescription,
                    {(isAll ? "" : "f.TRANS_NO as TransNo,")}
                    10 AS CommentTypeLink
                FROM tblARDetailViewFull f
                LEFT JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                LEFT JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                LEFT JOIN tblChannelIDLink c ON o.SALES_TERR = c.Territory
                LEFT JOIN tblTerritoryGroups t ON c.ChannelID = t.ID
                LEFT JOIN tblRootCauses rc ON e.RootCauseID = rc.Code
                LEFT JOIN tblCustomerGroupsRR cg ON f.CUST = cg.BVCustNo
                LEFT JOIN tblRRAcctTypeExceptions r1 ON cg.CustGroup = r1.RRGroupNo
                LEFT JOIN tblRRAcctTypeExceptions r2 ON f.CUST = r2.BVCustNo
                WHERE f.UserId = @p0
                {(isAll ? "" : "AND t.RogersReporting = 1")}
            ";

            var result = new List<Dictionary<string, object>>();
            var conn = _dbContext.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 600;
                var p0 = cmd.CreateParameter();
                p0.ParameterName = "@p0";
                p0.Value = userId;
                cmd.Parameters.Add(p0);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(dict);
                    }
                    if (result.Count == 0 && reader.FieldCount > 0)
                    {
                        var dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++) dict[reader.GetName(i)] = "EMPTY_MARKER";
                        result.Add(dict);
                    }
                }
            }
            if (!wasOpen) await conn.CloseAsync();
            return result;
        }

        private async Task<IEnumerable<object>> GetARMasterSummaryExportDataAsync(int userId)
        {
            string sql = @"
                SELECT 
                    '' AS Category,
                    f.CUST as CustNo,
                    MAX(o.CustName) AS MaxOfName,
                    MAX(o.SALES_TERR) AS MaxOfBVTerr,
                    MAX(o.BVCOCONTACT1NAME) AS MaxOfBVContact,
                    MAX(o.BVCOCONTACT1TEL1) AS MaxOfBVTel,
                    SUM(f.BALANCE) AS SumOfBALANCE,
                    SUM(CASE WHEN f.DaysOld < 30 THEN f.BALANCE ELSE 0 END) AS SumOfCurrent,
                    SUM(CASE WHEN f.DaysOld >= 30 AND f.DaysOld < 60 THEN f.BALANCE ELSE 0 END) AS SumOf30Days,
                    SUM(CASE WHEN f.DaysOld >= 60 AND f.DaysOld < 90 THEN f.BALANCE ELSE 0 END) AS SumOf60Days,
                    SUM(CASE WHEN f.DaysOld >= 90 AND f.DaysOld < 120 THEN f.BALANCE ELSE 0 END) AS SumOf90Days,
                    SUM(CASE WHEN f.DaysOld >= 120 THEN f.BALANCE ELSE 0 END) AS SumOf120Days
                FROM tblARDetailViewFull f
                LEFT JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                LEFT JOIN tblChannelIDLink c ON o.SALES_TERR = c.Territory
                LEFT JOIN tblTerritoryGroups t ON c.ChannelID = t.ID
                WHERE f.UserId = @p0 AND t.RogersReporting = 1
                GROUP BY f.CUST
            ";

            var result = new List<Dictionary<string, object>>();
            var conn = _dbContext.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 600;
                var p0 = cmd.CreateParameter();
                p0.ParameterName = "@p0";
                p0.Value = userId;
                cmd.Parameters.Add(p0);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(dict);
                    }
                    if (result.Count == 0 && reader.FieldCount > 0)
                    {
                        var dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++) dict[reader.GetName(i)] = "EMPTY_MARKER";
                        result.Add(dict);
                    }
                }
            }
            if (!wasOpen) await conn.CloseAsync();
            return result;
        }

        public async Task<IEnumerable<object>> GetARMasterDataGridAsync(int userId)
        {
            try
            {
                string sql = @"
                    SELECT 
                        '' AS BAN,
                        f.CODE as Code,
                        f.TRANS_NO as TransNo, 
                        f.REF_NO as [Inv#], 
                        f.TranDate as [Date], 
                        f.CUST as CustNo, 
                        o.CustName as Name,
                        o.SALES_TERR as BVTerr,
                        o.BVCOCONTACT1NAME as BVContact,
                        o.BVCOCONTACT1TEL1 as BVTel,
                        NULL as MSD,
                        NULL as ShipToRegion,
                        f.BALANCE, 
                        CASE WHEN f.DaysOld < 30 THEN f.BALANCE ELSE 0 END as [Current],
                        CASE WHEN f.DaysOld >= 30 AND f.DaysOld < 60 THEN f.BALANCE ELSE 0 END as [30Days],
                        CASE WHEN f.DaysOld >= 60 AND f.DaysOld < 90 THEN f.BALANCE ELSE 0 END as [60Days],
                        CASE WHEN f.DaysOld >= 90 AND f.DaysOld < 120 THEN f.BALANCE ELSE 0 END as [90Days],
                        CASE WHEN f.DaysOld >= 120 THEN f.BALANCE ELSE 0 END as [120Days],
                        f.DaysOld as Days,
                        e.FirstNoticeDate, 
                        e.SecondNoticeDate,
                        NULL as BillToRegion,
                        NULL as [Terr-Inv],
                        e.RootCauseID as Root,
                        CASE WHEN f.DaysOld >= 90 THEN 13 ELSE NULL END as [Next],
                        NULL as AccountType,
                        NULL as WebOrderID,
                        YEAR(f.TranDate) as Yr,
                        NULL as GrpID,
                        NULL as GrpName,
                        NULL as Aging,
                        NULL as Aging1,
                        CASE WHEN f.DaysOld >= 90 THEN 'Escalation - Rogers Prime' ELSE 'Working to Collect' END as Category,
                        rc.Description as [Root Cause Description],
                        ev.EventText as EventText,
                        e.OPCResolved, 
                        e.OPCDescription
                    FROM tblARDetailViewFull f
                    LEFT JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                    LEFT JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                    LEFT JOIN tblEvents ev ON f.CUST = ev.CustNo AND ev.EventType = 10
                    LEFT JOIN tblRootCauses rc ON e.RootCauseID = rc.Code
                    WHERE f.UserId = @p0
                ";
                var result = new List<Dictionary<string, object>>();
                var conn = _dbContext.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 600;
                    var p0 = cmd.CreateParameter();
                    p0.ParameterName = "@p0";
                    p0.Value = userId;
                    cmd.Parameters.Add(p0);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            result.Add(row);
                        }
                    }
                }
                if (!wasOpen) conn.Close();
                return result;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                var errorText = $"=== GetARMasterDataGrid Error ({DateTime.Now}) ===\n" +
                                $"Error: {ex.Message}\n" +
                                $"Inner Exception: {innerMsg}\n" +
                                $"Stack Trace:\n{ex.StackTrace}\n\n";
                try { System.IO.File.AppendAllText(@"d:\LAPP\backend_error.txt", errorText); } catch { }
                throw;
            }
        }

        #region Batch Notice Output

        public async Task<bool> GenerateBatchNoticeDataAsync(DateTime agingDate, int userId)
        {
            return await GenerateARMasterDataAsync(agingDate, userId);
        }

        public async Task<List<BatchNoticeSummaryRow>> GetBatchNoticeSummaryAsync(string groupCriteria, int startDays, int endDays, string noticeType, int userId)
        {
            var result = new List<BatchNoticeSummaryRow>();

            if (string.IsNullOrEmpty(groupCriteria)) groupCriteria = "1=1";
            groupCriteria = BalanceParentheses(groupCriteria);
            string typeCondition = "1=1";

            if (noticeType == "First Notice")
            {
                typeCondition = "e.FirstNoticeDate IS NULL AND e.SecondNoticeDate IS NULL AND f.CODE = 'I'";
            }
            else if (noticeType == "Second Notice")
            {
                typeCondition = "e.SecondNoticeDate IS NULL AND f.CODE = 'I'";
            }

            string sql = $@"
                WITH detailView AS (
                    SELECT 
                        f.CUST,
                        COALESCE(o.CustGroup, f.CUST) AS GroupID,
                        o.SALES_TERR,
                        CASE WHEN o.CustGroup IS NULL THEN 'Single' ELSE 'Group' END AS CustType,
                        o.CustName,
                        o.CustGroup,
                        f.TRANS_NO,
                        f.BALANCE,
                        f.CODE AS Type,
                        CASE WHEN f.CODE IN ('I', 'C') THEN 1 ELSE 0 END AS InvoiceCount,
                        CASE WHEN f.CODE = 'P' THEN 1 ELSE 0 END AS PaymentCount,
                        CASE WHEN e.FirstNoticeDate IS NOT NULL THEN 1 ELSE 0 END AS FirstNoticeCount,
                        CASE WHEN e.SecondNoticeDate IS NOT NULL THEN 1 ELSE 0 END AS SecondNoticeCount,
                        CASE WHEN b.CustNo IS NULL THEN 0 ELSE 1 END AS SendBulk
                    FROM tblARDetailViewFull f
                    LEFT JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                    LEFT JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                    LEFT JOIN tblBulkCustomers b ON o.CUST = b.CustNo
                    WHERE f.UserId = @userId 
                      AND f.DaysOld >= @startDays 
                      AND f.DaysOld <= @endDays
                      AND ({typeCondition})
                      AND ({groupCriteria})
                )
                SELECT 
                    dv.GroupID,
                    MAX(dv.SALES_TERR) AS MaxOfSALES_TERR,
                    MAX(COALESCE(g.GroupName, dv.CustName)) AS CustomerName,
                    MAX(dv.CustType) AS ARType,
                    COUNT(dv.TRANS_NO) AS TransCount,
                    SUM(dv.InvoiceCount) AS SumOfInvoiceCount,
                    SUM(dv.PaymentCount) AS SumOfPaymentCount,
                    SUM(dv.FirstNoticeCount) AS SumOfFirstNoticeCount,
                    SUM(dv.SecondNoticeCount) AS SumOfSecondNoticeCount,
                    SUM(dv.BALANCE) AS SumOfBALANCE,
                    MAX(dv.SendBulk) AS BulkInvoice
                FROM detailView dv
                LEFT JOIN tblCustomerGroups g ON dv.CUST = g.BVCustNo AND dv.GroupID = g.CustGroup
                WHERE dv.Type = 'I'
                GROUP BY dv.GroupID
                HAVING SUM(dv.BALANCE) > 0;
            ";

            var conn = _dbContext.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 600;

                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@userId"; p1.Value = userId; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@startDays"; p2.Value = startDays; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.ParameterName = "@endDays"; p3.Value = endDays; cmd.Parameters.Add(p3);

                    try
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new BatchNoticeSummaryRow
                                {
                                    GroupID = reader["GroupID"].ToString() ?? "",
                                    MaxOfSALES_TERR = reader["MaxOfSALES_TERR"].ToString() ?? "",
                                    CustomerName = reader["CustomerName"].ToString() ?? "",
                                    ARType = reader["ARType"].ToString() ?? "",
                                    TransCount = Convert.ToInt32(reader["TransCount"]),
                                    SumOfInvoiceCount = Convert.ToInt32(reader["SumOfInvoiceCount"]),
                                    SumOfPaymentCount = Convert.ToInt32(reader["SumOfPaymentCount"]),
                                    SumOfFirstNoticeCount = Convert.ToInt32(reader["SumOfFirstNoticeCount"]),
                                    SumOfSecondNoticeCount = Convert.ToInt32(reader["SumOfSecondNoticeCount"]),
                                    SumOfBALANCE = Convert.ToDecimal(reader["SumOfBALANCE"]),
                                    BulkInvoice = Convert.ToBoolean(reader["BulkInvoice"])
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"SQL Error in GetBatchNoticeSummaryAsync: {ex.Message}. Query: {sql}");
                    }
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<BatchNoticeDetailRow>> GetBatchNoticeDetailAsync(string groupCriteria, int startDays, int endDays, string noticeType, int userId)
        {
            var result = new List<BatchNoticeDetailRow>();

            if (string.IsNullOrEmpty(groupCriteria)) groupCriteria = "1=1";
            groupCriteria = BalanceParentheses(groupCriteria);
            string typeCondition = "1=1";

            if (noticeType == "First Notice")
            {
                typeCondition = "e.FirstNoticeDate IS NULL AND e.SecondNoticeDate IS NULL AND f.CODE = 'I'";
            }
            else if (noticeType == "Second Notice")
            {
                typeCondition = "e.SecondNoticeDate IS NULL AND f.CODE = 'I'";
            }

            string sql = $@"
                SELECT 
                    f.CUST,
                    COALESCE(o.CustGroup, f.CUST) AS GroupID,
                    o.SALES_TERR,
                    CASE WHEN o.CustGroup IS NULL THEN 'Single' ELSE 'Group' END AS CustType,
                    o.CustName,
                    o.CustGroup,
                    f.CODE AS Type,
                    f.TRANS_NO,
                    f.REF_NO,
                    f.TranDate,
                    CASE WHEN f.BALANCE > 0 THEN f.BALANCE ELSE 0 END AS D_AMOUNT,
                    CASE WHEN f.BALANCE < 0 THEN ABS(f.BALANCE) ELSE 0 END AS C_AMOUNT,
                    f.BALANCE,
                    f.DaysOld,
                    CAST(0 AS BIT) AS Checked,
                    e.FirstNoticeDate,
                    e.FirstNoticeBalance,
                    e.SecondNoticeDate,
                    e.SecondNoticeBalance,
                    CASE WHEN f.CODE IN ('I', 'C') THEN 1 ELSE 0 END AS InvoiceCount,
                    CASE WHEN f.CODE = 'P' THEN 1 ELSE 0 END AS PaymentCount,
                    CASE WHEN e.FirstNoticeDate IS NOT NULL THEN 1 ELSE 0 END AS FirstNoticeCount,
                    CASE WHEN e.SecondNoticeDate IS NOT NULL THEN 1 ELSE 0 END AS SecondNoticeCount,
                    e.BulkID,
                    e.BulkIDChecked,
                    o.Language,
                    CASE WHEN b.CustNo IS NULL THEN 0 ELSE 1 END AS SendBulk
                FROM tblARDetailViewFull f
                LEFT JOIN tblARDetailExtra e ON f.TRANS_NO = e.TransNo
                LEFT JOIN tblCustomersOpen o ON f.CUST = o.CUST AND f.UserId = o.UserId
                LEFT JOIN tblBulkCustomers b ON o.CUST = b.CustNo
                WHERE f.UserId = @userId 
                  AND f.DaysOld >= @startDays 
                  AND f.DaysOld <= @endDays
                  AND ({typeCondition})
                  AND ({groupCriteria})
                ORDER BY o.CustName;
            ";

            var conn = _dbContext.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 600;

                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@userId"; p1.Value = userId; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@startDays"; p2.Value = startDays; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.ParameterName = "@endDays"; p3.Value = endDays; cmd.Parameters.Add(p3);

                    try
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new BatchNoticeDetailRow
                                {
                                    CUST = reader["CUST"].ToString() ?? "",
                                    GroupID = reader["GroupID"].ToString() ?? "",
                                    SALES_TERR = reader["SALES_TERR"].ToString() ?? "",
                                    CustType = reader["CustType"].ToString() ?? "",
                                    CustName = reader["CustName"].ToString() ?? "",
                                    CustGroup = reader["CustGroup"] == DBNull.Value ? "" : reader["CustGroup"].ToString() ?? "",
                                    Type = reader["Type"].ToString() ?? "",
                                    TRANS_NO = reader["TRANS_NO"].ToString() ?? "",
                                    REF_NO = reader["REF_NO"].ToString() ?? "",
                                    TranDate = reader["TranDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["TranDate"]),
                                    D_AMOUNT = Convert.ToDecimal(reader["D_AMOUNT"]),
                                    C_AMOUNT = Convert.ToDecimal(reader["C_AMOUNT"]),
                                    BALANCE = Convert.ToDecimal(reader["BALANCE"]),
                                    DaysOld = Convert.ToInt32(reader["DaysOld"]),
                                    Checked = Convert.ToBoolean(reader["Checked"]),
                                    FirstNoticeDate = reader["FirstNoticeDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FirstNoticeDate"]),
                                    FirstNoticeBalance = reader["FirstNoticeBalance"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["FirstNoticeBalance"]),
                                    SecondNoticeDate = reader["SecondNoticeDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["SecondNoticeDate"]),
                                    SecondNoticeBalance = reader["SecondNoticeBalance"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["SecondNoticeBalance"]),
                                    InvoiceCount = Convert.ToInt32(reader["InvoiceCount"]),
                                    PaymentCount = Convert.ToInt32(reader["PaymentCount"]),
                                    FirstNoticeCount = Convert.ToInt32(reader["FirstNoticeCount"]),
                                    SecondNoticeCount = Convert.ToInt32(reader["SecondNoticeCount"]),
                                    BulkID = reader["BulkID"] == DBNull.Value ? "" : reader["BulkID"].ToString() ?? "",
                                    BulkIDChecked = reader["BulkIDChecked"] != DBNull.Value && Convert.ToBoolean(reader["BulkIDChecked"]),
                                    Language = reader["Language"] == DBNull.Value ? "" : reader["Language"].ToString() ?? "",
                                    SendBulk = Convert.ToBoolean(reader["SendBulk"])
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception($"SQL Error in GetBatchNoticeDetailAsync: {ex.Message}. Query: {sql}");
                    }
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        private async Task<byte[]> GenerateInvoicePdfBytesAsync(List<string> invoiceNos)
        {
            if (invoiceNos == null || !invoiceNos.Any()) return Array.Empty<byte>();

            using (PdfDocument pdf = new PdfDocument())
            {
                bool hasData = false;
                foreach (var invNo in invoiceNos)
                {
                    var data = await GetInvoiceDataFromSpire(invNo);
                    if (data != null && data.Lines.Count > 0)
                    {
                        GeneratePdfLayout(pdf, data, invNo.PadLeft(10, '0'));
                        hasData = true;
                    }
                }

                if (!hasData) return Array.Empty<byte>();

                using (MemoryStream ms = new MemoryStream())
                {
                    pdf.SaveToStream(ms);
                    return ms.ToArray();
                }
            }
        }

        private class GeneratedDoc
        {
            public string Path { get; set; } = string.Empty;
            public byte[] Bytes { get; set; } = Array.Empty<byte>();
        }

        public async Task<byte[]> OutputBatchNoticesAsync(List<string> selectedGroups, string noticeType, int startDays, int endDays, string groupCriteria, string templatesPath, string initials, int userId)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    int noticeTypeInt = noticeType == "First Notice" ? 1 : 2;

                    // Optimize 1: Fetch all details once, outside the loop
                    var details = await GetBatchNoticeDetailAsync(groupCriteria, startDays, endDays, noticeType, userId);

                    // Optimize 2: Pre-fetch all bulk invoice number mappings up-front to avoid using dbContext inside parallel tasks
                    var bulkIds = details
                        .Where(d => !string.IsNullOrEmpty(d.BulkID))
                        .Select(d => d.BulkID)
                        .Distinct()
                        .ToList();

                    var bulkInvoiceMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    if (bulkIds.Any())
                    {
                        var bulkMappings = await _dbContext.ARDetailView
                            .Where(d => d.UserId == userId)
                            .Join(_dbContext.tblARDetailExtra, d => d.TRANS_NO, e => e.TransNo, (d, e) => new { d.REF_NO, e.BulkID })
                            .Where(x => bulkIds.Contains(x.BulkID))
                            .Select(x => new { x.BulkID, x.REF_NO })
                            .Distinct()
                            .ToListAsync();

                        bulkInvoiceMap = bulkMappings
                            .GroupBy(x => x.BulkID, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Select(x => x.REF_NO).ToList(), StringComparer.OrdinalIgnoreCase);
                    }

                    // Parallel execution states
                    var generatedDocs = new ConcurrentBag<GeneratedDoc>();
                    var semaphore = new SemaphoreSlim(15);
                    var docTasks = new List<Task>();

                    foreach (var groupId in selectedGroups)
                    {
                        var groupDetails = details.Where(d => d.GroupID == groupId).OrderBy(d => d.CUST).ThenBy(d => d.REF_NO).ToList();

                        if (!groupDetails.Any()) continue;

                        string strLanguage = groupDetails.First().Language;
                        string custName = groupDetails.First().CustName;
                        string folderPrefix = $"{groupId}-{custName.Replace("/", "").Replace("\\", "")}";

                        var outputtedRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var rs in groupDetails)
                        {
                            var currentRs = rs; // copy for closure

                            if (currentRs.Type == "I" || currentRs.Type == "C")
                            {
                                string strInvoiceRef = string.IsNullOrEmpty(currentRs.BulkID) ? currentRs.REF_NO : currentRs.BulkID;

                                if (!outputtedRefs.Contains(strInvoiceRef))
                                {
                                    outputtedRefs.Add(strInvoiceRef);

                                    docTasks.Add(Task.Run(async () =>
                                    {
                                        await semaphore.WaitAsync();
                                        try
                                        {
                                            List<string> invoiceNos;
                                            if (!string.IsNullOrEmpty(currentRs.BulkID) && currentRs.SendBulk)
                                            {
                                                if (!bulkInvoiceMap.TryGetValue(currentRs.BulkID, out invoiceNos))
                                                {
                                                    invoiceNos = new List<string> { currentRs.REF_NO };
                                                }
                                            }
                                            else
                                            {
                                                invoiceNos = new List<string> { currentRs.REF_NO };
                                            }

                                            byte[] pdfBytes = await GenerateInvoicePdfBytesAsync(invoiceNos);
                                            if (pdfBytes != null && pdfBytes.Length > 0)
                                            {
                                                generatedDocs.Add(new GeneratedDoc
                                                {
                                                    Path = $"{folderPrefix}/Invoice-{strInvoiceRef}.pdf",
                                                    Bytes = pdfBytes
                                                });
                                            }
                                        }
                                        finally
                                        {
                                            semaphore.Release();
                                        }
                                    }));
                                }
                            }
                            else if (currentRs.Type == "P")
                            {
                                if (!outputtedRefs.Contains(currentRs.TRANS_NO))
                                {
                                    outputtedRefs.Add(currentRs.TRANS_NO);

                                    docTasks.Add(Task.Run(async () =>
                                    {
                                        await semaphore.WaitAsync();
                                        try
                                        {
                                            byte[] pdfBytes = await OutputPaymentAdvicePdfAsync(currentRs.TRANS_NO, userId);
                                            if (pdfBytes != null && pdfBytes.Length > 0)
                                            {
                                                generatedDocs.Add(new GeneratedDoc
                                                {
                                                    Path = $"{folderPrefix}/Payment-{currentRs.REF_NO}.pdf",
                                                    Bytes = pdfBytes
                                                });
                                            }
                                        }
                                        finally
                                        {
                                            semaphore.Release();
                                        }
                                    }));
                                }
                            }
                        }

                        // Generate Word Overdue Notice
                        decimal dblNoticeAmount = groupDetails.Sum(d => d.BALANCE);
                        docTasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                var noticeReq = new CreateNoticeRequest
                                {
                                    NoticeType = noticeTypeInt,
                                    CustNo = groupId,
                                    CustName = custName,
                                    Language = strLanguage,
                                    Amount = dblNoticeAmount,
                                    CheckedTransNos = groupDetails.Select(d => d.TRANS_NO).ToList()
                                };

                                byte[] docxBytes = await GenerateOverdueNoticeAsync(noticeReq, templatesPath, initials, userId);
                                if (docxBytes != null && docxBytes.Length > 0)
                                {
                                    string noticeFileName = noticeTypeInt == 1 ? "1st Notice.docx" : "2nd Notice.docx";
                                    if (strLanguage.Contains("French")) noticeFileName = noticeTypeInt == 1 ? "1er Avis.docx" : "2ieme Avis.docx";

                                    generatedDocs.Add(new GeneratedDoc
                                    {
                                        Path = $"{folderPrefix}/{noticeFileName}",
                                        Bytes = docxBytes
                                    });
                                }
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }

                    // Await all parallel tasks
                    await Task.WhenAll(docTasks);

                    // Write output sequentially (ZipArchive is not thread-safe)
                    foreach (var doc in generatedDocs)
                    {
                        var entry = archive.CreateEntry(doc.Path);
                        using (var entryStream = entry.Open())
                        {
                            await entryStream.WriteAsync(doc.Bytes, 0, doc.Bytes.Length);
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }
        #endregion
        private string BalanceParentheses(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = System.Text.RegularExpressions.Regex.Replace(input, @"(?i)(CUST\)*)\s+([0-9]+)", "$1 = $2");
            int open = 0;
            foreach (char c in input)
            {
                if (c == '(') open++;
                if (c == ')') open--;
            }
            if (open > 0)
            {
                return input + new string(')', open);
            }
            else if (open < 0)
            {
                return new string('(', -open) + input;
            }
            return input;
        }
    }

    public class PaymentAdviceDetail
    {
        public int Id { get; set; }
        public string CustNo { get; set; } = string.Empty;
        public string CustName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<PaymentAppliedInvoice> Invoices { get; set; } = new List<PaymentAppliedInvoice>();
    }

    public class PaymentAppliedInvoice
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal AppliedAmount { get; set; }
    }

}

#endregion







