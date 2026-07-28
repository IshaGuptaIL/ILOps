using DAL.Common.Spire;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Inventory.InventoryEdit
{
    public class InventoryEditDA : IInventoryEdit
    {
        private readonly AppDBContext _context;
        private readonly string _pgConn;

        public InventoryEditDA(AppDBContext context, SpireDA spireDA)
        {
            _context = context;
            _pgConn = spireDA.PgConnString;
        }

        // ─── Terms Edit ───────────────────────────────────────────────────────

        public async Task<sales_history> GetInvoiceTermsAsync(string invoiceNo)
        {
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            const string sql = "SELECT invoice_no, terms_code, terms_description, total, fob FROM sales_history WHERE invoice_no = @inv LIMIT 1";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("inv", invoiceNo);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                return new sales_history
                {
                    invoice_no = r["invoice_no"]?.ToString() ?? "",
                    terms_code = r["terms_code"]?.ToString() ?? "",
                    terms_description = r["terms_description"]?.ToString() ?? "",
                    total = r["total"] != DBNull.Value ? Convert.ToDecimal(r["total"]) : 0,
                    fob = r["fob"]?.ToString() ?? ""
                };
            }
            return null;
        }

        public async Task<bool> UpdateInvoiceTermsAsync(string invoiceNo, string termsLabel, string modifiedBy)
        {
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            const string sql = "UPDATE sales_history SET terms_description = @terms WHERE invoice_no = @inv";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("terms", termsLabel);
            cmd.Parameters.AddWithValue("inv", invoiceNo);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ─── Bulk ID Edit ─────────────────────────────────────────────────────

        public async Task<int> GetBulkIdCountAsync(string bulkId)
        {
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            const string sql = "SELECT COUNT(*) FROM sales_history WHERE fob = @bulkId";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 600; // 10 minutes timeout
            cmd.Parameters.AddWithValue("bulkId", bulkId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateBulkIdAsync(string oldBulkId, string newBulkId, string modifiedBy)
        {
            // Validate newBulkId length (PostgreSQL fob field limit)
            if (string.IsNullOrEmpty(newBulkId))
                throw new ArgumentException("New Bulk ID cannot be empty");
            
            if (newBulkId.Length > 50) // Assuming 50 char limit, adjust as needed
                throw new ArgumentException("New Bulk ID is too long. Maximum 50 characters allowed.");

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            const string sql = "UPDATE sales_history SET fob = @newId WHERE fob = @oldId";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 600; // 10 minutes timeout for large updates
            cmd.Parameters.AddWithValue("newId", newBulkId);
            cmd.Parameters.AddWithValue("oldId", oldBulkId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<sales_history> GetSingleInvoiceBulkIdAsync(string invoiceNo)
        {
            return await GetInvoiceTermsAsync(invoiceNo);
        }

        public async Task<bool> UpdateSingleInvoiceBulkIdAsync(string invoiceNo, string newBulkId, string modifiedBy)
        {
            // Validate newBulkId length
            if (string.IsNullOrEmpty(newBulkId))
                throw new ArgumentException("New Bulk ID cannot be empty");
            
            if (newBulkId.Length > 50)
                throw new ArgumentException("New Bulk ID is too long. Maximum 50 characters allowed.");

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            const string sql = "UPDATE sales_history SET fob = @newId WHERE invoice_no = @inv";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 600; // 10 minutes timeout
            cmd.Parameters.AddWithValue("newId", newBulkId);
            cmd.Parameters.AddWithValue("inv", invoiceNo);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateMultipleBulkIdsAsync(List<string> invoiceNos, string newBulkId, string modifiedBy)
        {
            if (invoiceNos == null || !invoiceNos.Any()) return false;

            // Validate newBulkId length
            if (string.IsNullOrEmpty(newBulkId))
                throw new ArgumentException("New Bulk ID cannot be empty");
            
            if (newBulkId.Length > 50)
                throw new ArgumentException("New Bulk ID is too long. Maximum 50 characters allowed.");

            // 1. Audit Log in SQL Server (Local VBA Inner Table)
            var bulkEntries = invoiceNos.Select(inv => new tblBulkChangeList
            {
                InvoiceNo = inv,
                CreatedBy = modifiedBy,
                CreatedDate = DateTime.Now,
                //ModifiedBy = modifiedBy,
                //ModifiedDate = DateTime.Now
            }).ToList();

            await _context.tblBulkChangeList.AddRangeAsync(bulkEntries);
            await _context.SaveChangesAsync();

            // 2. Perform actual update in PostgreSQL
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            const string sql = "UPDATE sales_history SET fob = @newId WHERE invoice_no = ANY(@invs)";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 600; // 10 minutes timeout for large batch updates
            cmd.Parameters.AddWithValue("newId", newBulkId);
            cmd.Parameters.AddWithValue("invs", invoiceNos.ToArray());
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ─── Address Edit ─────────────────────────────────────────────────────
        // ─── Address Edit ─────────────────────────────────────────────────────
        public async Task<InvoiceAddressEditModel> GetInvoiceAddressAsync(string invoiceNo)
        {
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            
            // VBA logic uses 'addresses' table linked by link_no (Invoice No) and link_table='SHIS'
            const string sql = @"SELECT a.name, a.city, a.prov_state, a.postal_zip, a.country_code,
                                       a.address[1] as addr1, a.address[2] as addr2, a.address[3] as addr3, a.address[4] as addr4,
                                       a.addr_type
                                FROM addresses a
                                WHERE a.link_table = 'SHIS' AND a.link_no = @inv";
            
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("inv", invoiceNo);
            using var r = await cmd.ExecuteReaderAsync();
            
            var model = new InvoiceAddressEditModel { 
                InvoiceNo = invoiceNo, 
                BillTo = new AddressRecord(), 
                ShipTo = new AddressRecord() 
            };

            while (await r.ReadAsync())
            {
                var type = r["addr_type"]?.ToString();
                var record = new AddressRecord
                {
                    Name = r["name"]?.ToString() ?? "",
                    Address1 = r["addr1"]?.ToString() ?? "",
                    Address2 = r["addr2"]?.ToString() ?? "",
                    Address3 = r["addr3"]?.ToString() ?? "",
                    Address4 = r["addr4"]?.ToString() ?? "",
                    City = r["city"]?.ToString() ?? "",
                    ProvState = r["prov_state"]?.ToString() ?? "",
                    PostalZip = r["postal_zip"]?.ToString() ?? "",
                    CountryCode = r["country_code"]?.ToString() ?? ""
                };

                if (type == "B") model.BillTo = record;
                else if (type == "S") model.ShipTo = record;
            }
            return model;
        }

        public async Task<bool> UpdateInvoiceAddressAsync(InvoiceAddressEditModel model, string modifiedBy)
        {
            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();
            using var trans = await conn.BeginTransactionAsync();

            try
            {
                // 1. Update sales_history (VBA: UPDATE sales_history SET cust_name = ...)
                const string sqlSH = "UPDATE sales_history SET cust_name = @name WHERE invoice_no = @inv";
                using var cmdSH = new NpgsqlCommand(sqlSH, conn, trans);
                cmdSH.Parameters.AddWithValue("name", model.BillTo.Name?.Length > 60 ? model.BillTo.Name.Substring(0, 60) : (model.BillTo.Name ?? ""));
                cmdSH.Parameters.AddWithValue("inv", model.InvoiceNo);
                await cmdSH.ExecuteNonQueryAsync();

                // 2. Update addresses table (Bill To)
                const string sqlB = @"UPDATE addresses SET 
                                      name = @name, city = @city, prov_state = @prov, postal_zip = @zip, country_code = @country,
                                      address[1] = @a1, address[2] = @a2, address[3] = @a3, address[4] = @a4
                                      WHERE link_table = 'SHIS' AND link_no = @inv AND addr_type = 'B'";
                using var cmdB = new NpgsqlCommand(sqlB, conn, trans);
                AddAddressParameters(cmdB, model.BillTo, model.InvoiceNo);
                await cmdB.ExecuteNonQueryAsync();

                // 3. Update addresses table (Ship To)
                const string sqlS = @"UPDATE addresses SET 
                                      name = @name, city = @city, prov_state = @prov, postal_zip = @zip, country_code = @country,
                                      address[1] = @a1, address[2] = @a2, address[3] = @a3, address[4] = @a4
                                      WHERE link_table = 'SHIS' AND link_no = @inv AND addr_type = 'S'";
                using var cmdS = new NpgsqlCommand(sqlS, conn, trans);
                AddAddressParameters(cmdS, model.ShipTo, model.InvoiceNo);
                await cmdS.ExecuteNonQueryAsync();

                await trans.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        private void AddAddressParameters(NpgsqlCommand cmd, AddressRecord r, string inv)
        {
            cmd.Parameters.AddWithValue("name", r.Name ?? "");
            cmd.Parameters.AddWithValue("city", r.City ?? "");
            cmd.Parameters.AddWithValue("prov", r.ProvState ?? "");
            cmd.Parameters.AddWithValue("zip", r.PostalZip ?? "");
            cmd.Parameters.AddWithValue("country", r.CountryCode ?? "");
            cmd.Parameters.AddWithValue("a1", r.Address1 ?? "");
            cmd.Parameters.AddWithValue("a2", r.Address2 ?? "");
            cmd.Parameters.AddWithValue("a3", r.Address3 ?? "");
            cmd.Parameters.AddWithValue("a4", r.Address4 ?? "");
            cmd.Parameters.AddWithValue("inv", inv);
        }
    }
}
