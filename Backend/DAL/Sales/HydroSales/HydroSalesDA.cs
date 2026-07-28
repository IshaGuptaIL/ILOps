using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DAL.Sales.HydroSales
{
    public class HydroSalesDA : IHydroSales
    {
        private readonly AppDBContext _dbContext;
        private readonly string _pgConn;

        public HydroSalesDA(AppDBContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _pgConn = config.GetConnectionString("spire_Connection");
        }

        public async Task<PostPaymentResponse> PostPaymentAsync(PostPaymentRequest request, int userId)
        {
            if (string.IsNullOrWhiteSpace(request.InvoiceNo))
            {
                return new PostPaymentResponse { Success = false, Message = "Invoice number is required." };
            }

            // Set EF Command Timeout to 10 minutes (600 seconds)
            _dbContext.Database.SetCommandTimeout(600);

            string invoiceNo = request.InvoiceNo.Trim();
            string description = "";
            string orderNo = "";
            bool foundSpire = false;

            // 1. Query Spire PostgreSQL for invoice and partially paid comment
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                const string spireSql = @"
                    SELECT h.invoice_no, i.description, h.order_no 
                    FROM sales_history h
                    INNER JOIN sales_history_items i ON h.invoice_no = i.invoice_no
                    WHERE h.invoice_no = @invoiceNo AND i.description LIKE '%This invoice is partially paid%'";

                using (var cmd = new NpgsqlCommand(spireSql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@invoiceNo", invoiceNo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            foundSpire = true;
                            description = reader["description"]?.ToString() ?? "";
                            orderNo = reader["order_no"]?.ToString() ?? "";
                        }
                    }
                }
            }

            if (!foundSpire)
            {
                return new PostPaymentResponse
                {
                    Success = false,
                    Message = $"Invoice: {invoiceNo} was not found or does not have comment with Import ID"
                };
            }

            // 2. Parse ImportID from description
            int importId = 0;
            int idx = description.IndexOf("ID:", StringComparison.OrdinalIgnoreCase);
            if (idx != -1)
            {
                int start = idx + 3;
                string idStr = "";
                while (start < description.Length && char.IsWhiteSpace(description[start])) start++;
                while (start < description.Length && char.IsDigit(description[start]))
                {
                    idStr += description[start];
                    start++;
                }
                int.TryParse(idStr, out importId);
            }

            if (importId == 0)
            {
                return new PostPaymentResponse
                {
                    Success = false,
                    Message = "Could not determine ImportID from the invoice."
                };
            }

            // 3. Look up record from SQL Server dbo_t_orderimport table
            var importRecord = await _dbContext.dbo_t_orderimport
                .FirstOrDefaultAsync(x => x.ImportId == importId);

            if (importRecord == null)
            {
                return new PostPaymentResponse
                {
                    Success = false,
                    Message = "Could not find original order import record."
                };
            }

            // 4. Validate import row found
            bool ccPosted = importRecord.CreditCardPosted ?? false;
            bool chargedOnCc = importRecord.ChargedOnCreditCard ?? false;

            if (!ccPosted || !chargedOnCc)
            {
                return new PostPaymentResponse
                {
                    Success = false,
                    Message = "There is a problem with the Import Record."
                };
            }

            string ccTrans = importRecord.CreditCardTransaction ?? "";
            if (!ccTrans.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            {
                return new PostPaymentResponse
                {
                    Success = false,
                    Message = $"This invoice already has a payment linked to it, transaction: {ccTrans}"
                };
            }

            // 5. Update dbo_t_orderimport
            importRecord.imported = orderNo;
            importRecord.CreditCardPosted = false;
            importRecord.ModifiedBy = userId;
            importRecord.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            return new PostPaymentResponse
            {
                Success = true,
                Message = "The payment will be posted on the next scheduled payment process."
            };
        }

        public async Task<GenerateMemoResponse> GenerateMemoAsync(GenerateMemoRequest request, int userId)
        {
            if (request.OriginalAmount <= 0 ||
                string.IsNullOrWhiteSpace(request.InvoiceNo) ||
                string.IsNullOrWhiteSpace(request.CardType) ||
                string.IsNullOrWhiteSpace(request.WebOrderID))
            {
                return new GenerateMemoResponse
                {
                    Success = false,
                    Message = "Information is not complete."
                };
            }

            string cleanInvoice = request.InvoiceNo.Trim();
            long invNum = 0;
            if (long.TryParse(cleanInvoice, out long parsedInv))
            {
                invNum = parsedInv;
                cleanInvoice = invNum.ToString("D10");
            }

            bool foundAr = false;
            decimal totalAmount = 0;

            // Query Spire PostgreSQL for matching invoice in AR
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();
                const string arSql = @"
                    SELECT h.invoice_no, h.cust_no, h.total 
                    FROM sales_history h
                    INNER JOIN ar_transactions a ON h.trans_no = a.trans_no AND h.cust_no = a.cust_no AND h.invoice_no = a.ref_no
                    WHERE h.invoice_no = @invoiceNo";

                using (var cmd = new NpgsqlCommand(arSql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@invoiceNo", cleanInvoice);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            foundAr = true;
                            totalAmount = reader["total"] != DBNull.Value ? Convert.ToDecimal(reader["total"]) : 0m;
                        }
                    }
                }
            }

            if (!foundAr)
            {
                return new GenerateMemoResponse
                {
                    Success = false,
                    Message = "Information provided does not match a Hydro One invoice posted to AR"
                };
            }

            if (totalAmount != request.OriginalAmount)
            {
                return new GenerateMemoResponse
                {
                    Success = false,
                    Message = "Invoice amount is incorrect."
                };
            }

            string inv7 = invNum > 0 ? invNum.ToString("D7") : request.InvoiceNo.Trim();
            string memo = $"Inv:{inv7} Web:{request.WebOrderID.Trim()} CC:{request.CardType.Trim()} Tot:${request.OriginalAmount:0.00}";

            return new GenerateMemoResponse
            {
                Success = true,
                Message = "Information Verified",
                GeneratedMemo = memo
            };
        }
    }
}
