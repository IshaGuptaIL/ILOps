using DAL.Common.Login;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.CustomSearch
{
    public class CustomSearchDA : ICustomSearch
    {

        private readonly AppDBContext _dbContext;
        private readonly string _pgConn;
        private readonly string _sqlConn;

        public CustomSearchDA(AppDBContext context, IConfiguration config)
        {
            _dbContext = context;
            _pgConn = config.GetConnectionString("spire_Connection");      // Postgres
            _sqlConn = config.GetConnectionString("bvactivation_Connection"); // SQL Server
        }

        public async Task<ApiResposne> GetSalesActivationHeaders(string fieldName, string value)
        {
            var response = new ApiResposne();

            try
            {
                // ✅ Use AsNoTracking for performance
                var query = _dbContext.SalesActivations.AsNoTracking().AsQueryable();

                // ✅ Apply search filters for all buttons
                query = fieldName switch
                {
                    "WebOrderID" => query.Where(x => x.WebOrderID == value),
                    "IMEIESN" => query.Where(x => x.IMEIESN == value),
                    "CellPhoneNo" => query.Where(x => x.CellPhoneNo == value),
                    "Invoice" => query.Where(x => x.Invoice == value),
                    "CustomerName" => query.Where(x => x.CustName.StartsWith(value)), // Index-friendly
                    "CHTRChaseID" => query.Where(x => x.CHTRChaseID == value),
                    "CustomerPO" => query.Where(x => x.CustomerPONo == value),
                    "SimCardNo" => query.Where(x => x.SIMCardNo == value),
                    "PortedCTN" => query.Where(x => x.PortedCTN == value),
                    "OriginalInvoice" => query.Where(x => x.OriginalInvoice == value),
                    "UserName" => query.Where(x => x.UserName == value),
                    "CHTRWebID" => query.Where(x => x.CHTRWebID == value),

                    // ❌ default safe fallback
                    _ => query.Where(x => false)
                };

                // ✅ Increase SQL command timeout
                _dbContext.Database.SetCommandTimeout(180); // 3 minutes

                // ✅ Fetch header data grouped by Invoice
                var data = await query
                    .GroupBy(x => x.Invoice)
                    .Select(g => new SalesActivationHeaderBO
                    {
                        Invoice = g.Key,
                        InvoiceDate = g.Max(x => x.InvoiceDate),
                        CustomerNo = g.Max(x => x.Customer),
                        CustomerName = g.Max(x => x.CustName),
                        InvoiceTotal = (decimal)g.Sum(x => x.ItemSellPrice ?? 0),
                        CustTerritory = g.Max(x => x.CustTerritory),
                        WebOrderId = g.Max(x => x.WebOrderID),
                        OriginalInvoice = g.Max(x => x.OriginalInvoice),
                        UpfrontEdge = g.Max(x => x.UpFrontEdgePrice),
                        Adjustment = g.Max(x => x.AdjustmentType),
                        PaymentMethod = g.Max(x => x.PayMeth),
                        TransactionNumber = g.Max(x => x.TransactionNo)
                    })
                    .ToListAsync();

                // ✅ Assign sequence numbers
                int seq = 1;
                foreach (var item in data)
                {
                    item.Seq = seq++;
                }

                response.Success = true;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
    
public async Task<ApiResposne> GetSalesActivationDetails(string invoiceNo)
        {
            var response = new ApiResposne();

            try
            {
                var result = await _dbContext.SalesActivations
                    .Where(x => x.Invoice == invoiceNo)
                    .Select(x => new SalesActivationDetailBO
                    {
                        Whse = x.Whse,
                        PartNo = x.PartNumber,
                        Description = x.Description,
                        SerialNo = x.IMEIESN,
                        Comment = x.Comments,
                        Committed = (int)(x.Qty ?? 0),
                        UnitPrice = (decimal)(x.ItemSellPrice ?? 0)
                    })
                    .ToListAsync();

                response.Success = true;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }


        public async Task<List<tblSpireInvoice>> GenerateInvoiceAsync(string invoiceNo, int seq)
        {
            var result = new List<tblSpireInvoice>();
            if (string.IsNullOrEmpty(invoiceNo))
                return result;

            // 1️⃣ Fetch from Postgres
            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            var sqlFetch = @"
        SELECT 
            sh.invoice_no, sh.cust_no, sh.invoice_date, sh.territory_code, sh.terms_description,
            item.whse, item.part_no, item.description, item.comment, item.committed_qty, item.unit_price, item.current_cost,
            sh.subtotal, sh.freight, sh.total_discount, sh.total,
            item.guid, item.serialized_qty
        FROM ""sales_history"" sh
        INNER JOIN ""sales_history_items"" item ON sh.invoice_no = item.invoice_no
        WHERE sh.invoice_no = @invoiceNo
        ORDER BY sh.invoice_no;
    ";

            await using var fetchCmd = new NpgsqlCommand(sqlFetch, pgConn);
            fetchCmd.Parameters.AddWithValue("@invoiceNo", invoiceNo);

            await using var reader = await fetchCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new tblSpireInvoice
                {
                    invoice_no = reader["invoice_no"] != DBNull.Value ? reader["invoice_no"].ToString() : string.Empty,
                    cust_no = reader["cust_no"] != DBNull.Value ? reader["cust_no"].ToString() : string.Empty,
                    invoice_date = reader["invoice_date"] != DBNull.Value
    ? reader["invoice_date"] is DateTime dt
        ? dt
        : ((DateOnly)reader["invoice_date"]).ToDateTime(TimeOnly.MinValue)
    : DateTime.MinValue,
                    territory_code = reader["territory_code"] != DBNull.Value ? reader["territory_code"].ToString() : string.Empty,
                    terms_description = reader["terms_description"] != DBNull.Value ? reader["terms_description"].ToString() : string.Empty,
                    whse = reader["whse"] != DBNull.Value ? reader["whse"].ToString() : string.Empty,
                    part_no = reader["part_no"] != DBNull.Value ? reader["part_no"].ToString() : string.Empty,
                    description = !string.IsNullOrEmpty(reader["comment"]?.ToString())
                ? reader["comment"].ToString()
                : reader["description"] != DBNull.Value ? reader["description"].ToString() : string.Empty,
                    committed_qty = reader["committed_qty"] != DBNull.Value ? Convert.ToInt32(reader["committed_qty"]) : 0,
                    unit_price = (int?)(reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0),
                    current_cost = (int?)(reader["current_cost"] != DBNull.Value ? Convert.ToDecimal(reader["current_cost"]) : 0),
                    subtotal = (int?)(reader["subtotal"] != DBNull.Value ? Convert.ToDecimal(reader["subtotal"]) : 0),
                    freight = (int?)(reader["freight"] != DBNull.Value ? Convert.ToDecimal(reader["freight"]) : 0),
                    total_discount = (int?)(reader["total_discount"] != DBNull.Value ? Convert.ToDecimal(reader["total_discount"]) : 0),
                    total = (int?)(reader["total"] != DBNull.Value ? Convert.ToDecimal(reader["total"]) : 0),
                    strGUID = reader["guid"] != DBNull.Value ? reader["guid"].ToString() : string.Empty,
                    serialized_qty = reader["serialized_qty"] != DBNull.Value ? Convert.ToInt32(reader["serialized_qty"]) : 0,
                    number = reader["guid"] != DBNull.Value ? GenerateSerial(reader["guid"].ToString()) : string.Empty
                };

                result.Add(row);
            }

            await reader.CloseAsync();
            await pgConn.CloseAsync();

            // 2️⃣ Insert into SQL Server
            using var sqlConn = new SqlConnection(_sqlConn);
            await sqlConn.OpenAsync();

            // Clear previous temp rows
            using (var delCmd = new SqlCommand("DELETE FROM tblSpireInvoice;", sqlConn))
            {
                await delCmd.ExecuteNonQueryAsync();
            }

            foreach (var inv in result)
            {
                var insertCmd = new SqlCommand(@"
            INSERT INTO tblSpireInvoice (
                invoice_no, cust_no, invoice_date, territory_code, terms_description,
                whse, part_no, description, committed_qty, unit_price, current_cost,
                subtotal, freight, total_discount, total,
                strGUID, serialized_qty, number
            ) VALUES (
                @invoice_no, @cust_no, @invoice_date, @territory_code, @terms_description,
                @whse, @part_no, @description, @committed_qty, @unit_price, @current_cost,
                @subtotal, @freight, @total_discount, @total,
                @strGUID, @serialized_qty, @number
            );", sqlConn);

                insertCmd.Parameters.AddWithValue("@invoice_no", inv.invoice_no);
                insertCmd.Parameters.AddWithValue("@cust_no", inv.cust_no);
                insertCmd.Parameters.AddWithValue("@invoice_date", inv.invoice_date);
                insertCmd.Parameters.AddWithValue("@territory_code", inv.territory_code);
                insertCmd.Parameters.AddWithValue("@terms_description", inv.terms_description);
                insertCmd.Parameters.AddWithValue("@whse", inv.whse);
                insertCmd.Parameters.AddWithValue("@part_no", inv.part_no);
                insertCmd.Parameters.AddWithValue("@description", inv.description);
                insertCmd.Parameters.AddWithValue("@committed_qty", inv.committed_qty);
                insertCmd.Parameters.AddWithValue("@unit_price", inv.unit_price);
                insertCmd.Parameters.AddWithValue("@current_cost", inv.current_cost);
                insertCmd.Parameters.AddWithValue("@subtotal", inv.subtotal);
                insertCmd.Parameters.AddWithValue("@freight", inv.freight);
                insertCmd.Parameters.AddWithValue("@total_discount", inv.total_discount);
                insertCmd.Parameters.AddWithValue("@total", inv.total);
                insertCmd.Parameters.AddWithValue("@strGUID", inv.strGUID);
                insertCmd.Parameters.AddWithValue("@serialized_qty", inv.serialized_qty);
                insertCmd.Parameters.AddWithValue("@number", inv.number);

                await insertCmd.ExecuteNonQueryAsync();
            }

            return result;
        }

        private string GenerateSerial(string guid)
        {
            return "SERIAL-" + guid.Substring(0, 8);
        }
        public async Task<ApiResposne> GetTransactionData(string invoiceNo)
        {
            var response = new ApiResposne();

            try
            {
                await using var pgConn = new NpgsqlConnection(_pgConn);
                await pgConn.OpenAsync();

                var sql = @"
            SELECT 
                t.account_no,
                a.name,
                t.debit_amt,
                t.credit_amt,
                t.trans_no,
                s.invoice_no,
                t.date
            FROM sales_history s
            INNER JOIN gl_transactions t 
                ON s.trans_no = t.trans_no
            INNER JOIN gl_accounts a 
                ON t.account_no = a.account_no
                AND t.division = a.division
                AND t.currency = a.currency
            WHERE s.invoice_no = @invoiceNo
        ";

                var list = new List<object>();

                await using var cmd = new NpgsqlCommand(sql, pgConn);
                cmd.Parameters.AddWithValue("@invoiceNo", invoiceNo);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        AccountNo = reader["account_no"]?.ToString(),
                        Name = reader["name"]?.ToString(),
                        Debit = reader["debit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["debit_amt"]) : 0,
                        Credit = reader["credit_amt"] != DBNull.Value ? Convert.ToDecimal(reader["credit_amt"]) : 0,
                        TransNo = reader["trans_no"]?.ToString(),
                        Invoice = reader["invoice_no"]?.ToString(),
                        Date = reader["date"] != DBNull.Value
    ? reader["date"] is DateTime dt
        ? dt
        : ((DateOnly)reader["date"]).ToDateTime(TimeOnly.MinValue)
    : DateTime.MinValue
                    });
                }

                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

    }
}