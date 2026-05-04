using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DAL.Inventory.RogerAR
{
    public class RogerDA : IRoger 
    {

        private readonly string _sqlConn;
        private readonly string _pgConn;
        private static List<RogerarBO> _loadedData = new List<RogerarBO>();

        public RogerDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
            _pgConn = configuration.GetConnectionString("spire_Connection");
        }

        public async Task<RogerarListResponse> GetARDataAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var items = new List<RogerarBO>();
            int totalItems = 0;

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                string filter = "";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filter = "WHERE (a.CustomerNo LIKE @term OR a.CustomerName LIKE @term OR a.InvoiceNo LIKE @term OR a.[Transaction] LIKE @term)";
                }

                string countSql = $@"
                    SELECT COUNT(*) 
                    FROM RogersAR a
                    LEFT JOIN RogersARData d ON a.[Transaction] = d.TransactionNo
                    {filter}";

                using (var countCmd = new SqlCommand(countSql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                        countCmd.Parameters.AddWithValue("@term", $"%{searchTerm}%");
                    totalItems = (int)await countCmd.ExecuteScalarAsync();
                }

                string dataSql = $@"
                    SELECT a.*, d.Comments, d.Remarks, d.SentOn, d.Comments2, d.Comments3, d.PaymentCode, d.PaymentDate, d.CreatedBy, d.CreatedDate, d.ModifiedBy, d.ModifiedDate
                    FROM RogersAR a
                    LEFT JOIN RogersARData d ON a.[Transaction] = d.TransactionNo
                    {filter}
                    ORDER BY a.[Date] DESC
                    OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY";

                using (var cmd = new SqlCommand(dataSql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                        cmd.Parameters.AddWithValue("@term", $"%{searchTerm}%");
                    cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@size", pageSize);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new RogerarBO
                            {
                                CustomerNo = reader["CustomerNo"].ToString(),
                                Transaction = reader["Transaction"].ToString(),
                                Date = reader["Date"] != DBNull.Value ? (DateTime?)reader["Date"] : null,
                                InvoiceNo = reader["InvoiceNo"].ToString(),
                                DebitAmt = reader["DebitAmt"] != DBNull.Value ? (decimal)reader["DebitAmt"] : 0,
                                Balance = reader["Balance"] != DBNull.Value ? (decimal)reader["Balance"] : 0,
                                CustomerName = reader["CustomerName"].ToString(),
                                Territory = reader["Territory"].ToString(),
                                Comments = reader["Comments"].ToString(),
                                Remarks = reader["Remarks"].ToString(),
                                SentOn = reader["SentOn"] != DBNull.Value ? (DateTime?)reader["SentOn"] : null,
                                Comments2 = reader["Comments2"].ToString(),
                                Comments3 = reader["Comments3"].ToString(),
                                PaymentCode = reader["PaymentCode"].ToString(),
                                PaymentDate = reader["PaymentDate"] != DBNull.Value ? (DateTime?)reader["PaymentDate"] : null,
                                CreatedBy = reader["CreatedBy"].ToString(),
                                CreatedDate = (DateTime)reader["CreatedDate"],
                                ModifiedBy = reader["ModifiedBy"]?.ToString(),
                                ModifiedDate = reader["ModifiedDate"] != DBNull.Value ? (DateTime?)reader["ModifiedDate"] : null
                            });
                        }
                    }
                }
            }

            return new RogerarListResponse
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<bool> UpdateARDataAsync(RogerarBO item, string userId)
        {
            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                // Ownership Check
                string checkSql = "SELECT CreatedBy FROM RogersARData WHERE TransactionNo = @trans";
                using (var checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@trans", item.Transaction);
                    var creator = await checkCmd.ExecuteScalarAsync();

                    if (creator != null && creator != DBNull.Value && creator.ToString() != userId)
                    {
                        // Not the owner
                        return false;
                    }
                }

                // Update
                string sql = @"
                    UPDATE RogersARData 
                    SET Comments = @Comments, 
                        Remarks = @Remarks, 
                        SentOn = @SentOn, 
                        Comments2 = @Comments2, 
                        Comments3 = @Comments3, 
                        PaymentCode = @PaymentCode, 
                        PaymentDate = @PaymentDate,
                        ModifiedBy = @user,
                        ModifiedDate = GETDATE()
                    WHERE TransactionNo = @trans";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Comments", (object)item.Comments ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Remarks", (object)item.Remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SentOn", (object)item.SentOn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Comments2", (object)item.Comments2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Comments3", (object)item.Comments3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaymentCode", (object)item.PaymentCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaymentDate", (object)item.PaymentDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@user", userId);
                    cmd.Parameters.AddWithValue("@trans", item.Transaction);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }

        public async Task<RogerarListResponse> LoadARDataAsync(string userId, int pageNumber, int pageSize)
        {
            var tempList = new List<RogerarBO>();

            using (var pgConn = new NpgsqlConnection(_pgConn))
            {
                await pgConn.OpenAsync();

                string sqlMain = @"
            SELECT 
                ar.cust_no AS CustomerNo, 
                ar.trans_no AS TransactionNo, 
                ar.date AS TransDate, 
                ar.ref_no AS InvoiceNo, 
                ar.debit_amt AS DebitAmt, 
                ar.balance AS Balance, 
                UPPER(sh.cust_name) AS CustomerName, 
                sh.territory_code AS Territory 
            FROM ar_transactions ar 
            INNER JOIN sales_history sh ON ar.ref_no = sh.invoice_no 
            WHERE (ar.cust_no <> 'ROGUPS') 
              AND (UPPER(sh.cust_name) LIKE '%ROGER%') 
              AND (sh.territory_code IN ('HOF', 'HIS', 'HMS')) 
              AND (ar.open_close_flag = 'O') 
              AND (ar.code = 'I')";

                using (var cmd = new NpgsqlCommand(sqlMain, pgConn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tempList.Add(new RogerarBO
                        {
                            CustomerNo = reader["CustomerNo"].ToString(),
                            Transaction = reader["TransactionNo"].ToString(),
                            Date = reader["TransDate"] != DBNull.Value
                                ? ((DateOnly)reader["TransDate"]).ToDateTime(TimeOnly.MinValue)
                                : (DateTime?)null,
                            InvoiceNo = reader["InvoiceNo"].ToString(),
                            DebitAmt = reader["DebitAmt"] != DBNull.Value ? Convert.ToDecimal(reader["DebitAmt"]) : 0,
                            Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0,
                            CustomerName = reader["CustomerName"].ToString(),
                            Territory = reader["Territory"].ToString()
                        });
                    }
                }
            }

            using (var sqlConn = new SqlConnection(_sqlConn))
            {
                await sqlConn.OpenAsync();

                using (var truncCmd = new SqlCommand("TRUNCATE TABLE RogersAR", sqlConn))
                {
                    await truncCmd.ExecuteNonQueryAsync();
                }

                foreach (var item in tempList)
                {
                    string insertSql = @"
                INSERT INTO RogersAR 
                (CustomerNo, [Transaction], [Date], InvoiceNo, DebitAmt, Balance, CustomerName, Territory, CreatedBy, CreatedDate)
                VALUES (@cust, @trans, @date, @inv, @debit, @bal, @name, @terr, @user, GETDATE())";

                    using (var cmd = new SqlCommand(insertSql, sqlConn))
                    {
                        cmd.Parameters.AddWithValue("@cust", item.CustomerNo);
                        cmd.Parameters.AddWithValue("@trans", item.Transaction);
                        cmd.Parameters.AddWithValue("@date", (object)item.Date ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@inv", item.InvoiceNo);
                        cmd.Parameters.AddWithValue("@debit", item.DebitAmt);
                        cmd.Parameters.AddWithValue("@bal", item.Balance);
                        cmd.Parameters.AddWithValue("@name", item.CustomerName);
                        cmd.Parameters.AddWithValue("@terr", item.Territory);
                        cmd.Parameters.AddWithValue("@user", userId);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    string checkSql = "SELECT COUNT(*) FROM RogersARData WHERE TransactionNo = @trans";
                    bool exists = false;

                    using (var checkCmd = new SqlCommand(checkSql, sqlConn))
                    {
                        checkCmd.Parameters.AddWithValue("@trans", item.Transaction);
                        exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
                    }

                    if (!exists)
                    {
                        var commentsList = new List<string>();

                        using (var pgConn = new NpgsqlConnection(_pgConn))
                        {
                            await pgConn.OpenAsync();

                            string sqlComments = @"
                        SELECT comment 
                        FROM sales_history_items 
                        WHERE invoice_no = @inv AND part_no IS NULL";

                            using (var cmdComment = new NpgsqlCommand(sqlComments, pgConn))
                            {
                                cmdComment.Parameters.AddWithValue("@inv", item.InvoiceNo);

                                using (var reader = await cmdComment.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        var c = reader["comment"]?.ToString();
                                        if (!string.IsNullOrEmpty(c))
                                            commentsList.Add(c);
                                    }
                                }
                            }
                        }

                        string insertDataSql = @"
                    INSERT INTO RogersARData 
                    (TransactionNo, Comments, CreatedBy, CreatedDate)
                    VALUES (@trans, @comments, @user, GETDATE())";

                        using (var cmdData = new SqlCommand(insertDataSql, sqlConn))
                        {
                            cmdData.Parameters.AddWithValue("@trans", item.Transaction);
                            cmdData.Parameters.AddWithValue("@comments", string.Join("; ", commentsList));
                            cmdData.Parameters.AddWithValue("@user", userId);

                            await cmdData.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

            return await GetARDataAsync("", pageNumber, pageSize);
        }
        public async Task<byte[]> ExportToExcelAsync()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var data = new List<RogerarBO>();

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT a.*, d.Comments, d.Remarks, d.SentOn, d.Comments2, d.Comments3, d.PaymentCode, d.PaymentDate
                    FROM RogersAR a
                    LEFT JOIN RogersARData d ON a.[Transaction] = d.TransactionNo
                    ORDER BY a.[Date] DESC";

                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new RogerarBO
                        {
                            CustomerNo = reader["CustomerNo"].ToString(),
                            Transaction = reader["Transaction"].ToString(),
                            Date = reader["Date"] != DBNull.Value ? (DateTime?)reader["Date"] : null,
                            InvoiceNo = reader["InvoiceNo"].ToString(),
                            DebitAmt = (decimal)reader["DebitAmt"],
                            Balance = (decimal)reader["Balance"],
                            CustomerName = reader["CustomerName"].ToString(),
                            Territory = reader["Territory"].ToString(),
                            Comments = reader["Comments"].ToString(),
                            Remarks = reader["Remarks"].ToString(),
                            SentOn = reader["SentOn"] != DBNull.Value ? (DateTime?)reader["SentOn"] : null,
                            Comments2 = reader["Comments2"].ToString(),
                            Comments3 = reader["Comments3"].ToString(),
                            PaymentCode = reader["PaymentCode"].ToString(),
                            PaymentDate = reader["PaymentDate"] != DBNull.Value ? (DateTime?)reader["PaymentDate"] : null
                        });
                    }
                }
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("RogersAR");

                string[] headers = {
                    "CustomerNo", "Transaction", "Date", "InvoiceNo", "DebitAmt", "BALANCE",
                    "Comments", "Remarks", "SentOn", "Comments2", "Comments3", "PaymentCode", "PaymentDate"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    worksheet.Cells[i + 2, 1].Value = item.CustomerNo;
                    worksheet.Cells[i + 2, 2].Value = item.Transaction;
                    worksheet.Cells[i + 2, 3].Value = item.Date?.ToString("yyyy-MM-dd");
                    worksheet.Cells[i + 2, 4].Value = item.InvoiceNo;
                    worksheet.Cells[i + 2, 5].Value = item.DebitAmt;
                    worksheet.Cells[i + 2, 6].Value = item.Balance;
                    worksheet.Cells[i + 2, 7].Value = item.Comments;
                    worksheet.Cells[i + 2, 8].Value = item.Remarks;
                    worksheet.Cells[i + 2, 9].Value = item.SentOn?.ToString("yyyy-MM-dd");
                    worksheet.Cells[i + 2, 10].Value = item.Comments2;
                    worksheet.Cells[i + 2, 11].Value = item.Comments3;
                    worksheet.Cells[i + 2, 12].Value = item.PaymentCode;
                    worksheet.Cells[i + 2, 13].Value = item.PaymentDate?.ToString("yyyy-MM-dd");
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }
    }
}

