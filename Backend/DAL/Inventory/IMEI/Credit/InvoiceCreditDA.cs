using DAL.Common.Login;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Inventory.IMEI.Credit
{
    public class InvoiceCreditDA : IInvoiceCredit
    {
        private readonly string _sqlConn;
        private readonly string _conn;

        public InvoiceCreditDA(IConfiguration configuration)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
            _conn = configuration.GetConnectionString("spire_Connection");
        }

        public async Task<ApiResposne> FindReceiptAsync(FindReceiptBO request)
        {
            var response = new ApiResposne();
            var list = new List<object>();

            try
            {
                if (string.IsNullOrEmpty(request.ReceiptNo))
                {
                    response.Success = false;
                    response.Message = "PO number required.";
                    return response;
                }

                using SqlConnection conn = new SqlConnection(_sqlConn);
                using SqlCommand cmd = new SqlCommand(
                    @"SELECT BVReceiptNo, Vendor, PO, BVReceiptDate, 
                             Part, Qty, ReceiptUnitCost, CMO, ItemType
                      FROM HardwareReceived
                      WHERE PO = @PO", conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@PO", request.ReceiptNo.Trim().PadLeft(10, '0'));

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        BVReceiptNo = reader["BVReceiptNo"].ToString(),
                        Vendor = reader["Vendor"].ToString(),
                        PONumber = reader["PO"].ToString(),
                        ReceiptDate = Convert.ToDateTime(reader["BVReceiptDate"]).ToString("yyyy-MM-dd"),
                        PartNo = reader["Part"].ToString(),
                        QtyReceived = Convert.ToDouble(reader["Qty"]),
                        UnitCost = Convert.ToDecimal(reader["ReceiptUnitCost"]),
                        CMO = reader["CMO"].ToString(),
                        Type = reader["ItemType"].ToString()
                    });
                }

                if (list.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No records found for this PO.";
                }
                else
                {
                    response.Success = true;
                    response.Message = "Records found.";
                    response.Result = list;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> SaveInvoiceAsync(SaveInvoiceBO request)
        {
            var response = new ApiResposne();

            try
            {
                if (string.IsNullOrEmpty(request.RefNo))
                {
                    response.Success = false;
                    response.Message = "Reference number required.";
                    return response;
                }

                using SqlConnection conn = new SqlConnection(_sqlConn);
                string sql = @"INSERT INTO tblRogersInvoice
                               (BVReceiptNo, TransType, RefNo, TransDate, PerUnitAmount, Remarks)
                               VALUES
                               (@BVReceiptNo, @TransType, @RefNo, @TransDate, @PerUnitAmount, @Remarks)";
                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;

                cmd.Parameters.AddWithValue("@BVReceiptNo", request.BVReceiptNo);
                cmd.Parameters.AddWithValue("@TransType", request.TransType);
                cmd.Parameters.AddWithValue("@RefNo", request.RefNo);
                cmd.Parameters.AddWithValue("@TransDate", request.TransDate);
                cmd.Parameters.AddWithValue("@PerUnitAmount", request.PerUnitAmount);
                cmd.Parameters.AddWithValue("@Remarks", request.Remarks ?? "");

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                response.Success = true;
                response.Message = "Invoice saved successfully.";
            }
            catch
            {
                response.Success = false;
                response.Message = "Error saving invoice.";
            }

            return response;
        }

        public async Task<ApiResposne> GetRogersInvoicesAsync(string receiptNo)
        {
            var response = new ApiResposne();
            var list = new List<object>();

            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                using SqlCommand cmd = new SqlCommand(
                    @"SELECT TransType, RefNo, TransDate, PerUnitAmount
                      FROM tblRogersInvoice
                      WHERE BVReceiptNo = @ReceiptNo", conn);
                cmd.CommandTimeout = 600;

                cmd.Parameters.AddWithValue("@ReceiptNo", receiptNo);

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        TransType = reader["TransType"].ToString(),
                        RefNo = reader["RefNo"].ToString(),
                        TransDate = Convert.ToDateTime(reader["TransDate"]),
                        Amount = Convert.ToDecimal(reader["PerUnitAmount"])
                    });
                }

                response.Success = true;
                response.Result = list;
            }
            catch
            {
                response.Success = false;
                response.Message = "Error fetching invoices.";
            }

            return response;
        }

        public async Task<ApiResposne> LoadAccReceipts()
        {
            var response = new ApiResposne();
            try
            {
                long lastAccReceipt = 0;

                using (var sqlConn = new SqlConnection(_sqlConn))
                {
                    await sqlConn.OpenAsync();
                    using (var trans = sqlConn.BeginTransaction())
                    {
                        // STEP 1: Get Last ID from SQL Server
                        string getSettingsSql = "SELECT ISNULL(LastACCReceipt, 0) FROM tblSettingsApi";
                        using (var cmd = new SqlCommand(getSettingsSql, sqlConn, trans))
                        {
                            cmd.CommandTimeout = 600;
                            lastAccReceipt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        }

                        // STEP 2: Fetch NEW data from PostgreSQL (Spire)
                        var newData = new List<AccReceiptTransferModel>();
                        using (var pgConn = new NpgsqlConnection(_conn))
                        {
                            await pgConn.OpenAsync();
                            string pgSql = @"
                                SELECT r.id, i.part_no, r.receive_date, r.link_no, r.cost, r.qty, r.vendor_no
                                FROM inventory_receipts r
                                INNER JOIN inventory i ON r.inventory_id = i.id
                                WHERE i.product_code = 'ACC' 
                                  AND r.id > @LastID 
                                  AND r.link_table = 'PORD'";

                            using (var pgCmd = new NpgsqlCommand(pgSql, pgConn))
                            {
                                pgCmd.CommandTimeout = 600;
                                pgCmd.Parameters.AddWithValue("LastID", lastAccReceipt);
                                using (var reader = await pgCmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        newData.Add(new AccReceiptTransferModel
                                        {
                                            ID = reader["id"].ToString(),
                                            PartNo = reader["part_no"]?.ToString() ?? "",
                                            ReceiveDate = reader["receive_date"] is DateOnly dateOnly
                                                ? dateOnly.ToDateTime(TimeOnly.MinValue)
                                                : Convert.ToDateTime(reader["receive_date"]),
                                            LinkNo = reader["link_no"]?.ToString() ?? "",
                                            Cost = Convert.ToDecimal(reader["cost"]),
                                            Qty = Convert.ToDouble(reader["qty"]),
                                            VendorNo = reader["vendor_no"]?.ToString() ?? ""
                                        });
                                    }
                                }
                            }
                        }

                        if (newData.Count == 0)
                        {
                            response.Success = true;
                            response.Message = "No new receipts to load.";
                            return response;
                        }

                        // STEP 3: Bulk Insert into SQL Server using SqlBulkCopy
                        var table = new DataTable();
                        table.Columns.Add("BVReceiptNo", typeof(string));
                        table.Columns.Add("Part", typeof(string));
                        table.Columns.Add("BVReceiptDate", typeof(DateTime));
                        table.Columns.Add("PO", typeof(string));
                        table.Columns.Add("ReceiptUnitCost", typeof(decimal));
                        table.Columns.Add("QTY", typeof(double));
                        table.Columns.Add("Vendor", typeof(string));
                        table.Columns.Add("ItemType", typeof(string));

                        foreach (var item in newData)
                        {
                            table.Rows.Add(
                                item.ID.PadLeft(10, '0'),
                                item.PartNo,
                                item.ReceiveDate,
                                item.LinkNo,
                                item.Cost,
                                item.Qty,
                                item.VendorNo,
                                "ACC"
                            );
                        }

                        using (var bulkCopy = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.Default, trans))
                        {
                            bulkCopy.DestinationTableName = "HardwareReceived";
                            bulkCopy.BulkCopyTimeout = 600;
                            bulkCopy.ColumnMappings.Add("BVReceiptNo", "BVReceiptNo");
                            bulkCopy.ColumnMappings.Add("Part", "Part");
                            bulkCopy.ColumnMappings.Add("BVReceiptDate", "BVReceiptDate");
                            bulkCopy.ColumnMappings.Add("PO", "PO");
                            bulkCopy.ColumnMappings.Add("ReceiptUnitCost", "ReceiptUnitCost");
                            bulkCopy.ColumnMappings.Add("Qty", "Qty");
                            bulkCopy.ColumnMappings.Add("Vendor", "Vendor");
                            bulkCopy.ColumnMappings.Add("ItemType", "ItemType");

                            await bulkCopy.WriteToServerAsync(table);
                        }

                        // Update Settings with new Max ID
                        long maxId = newData.Max(x => Convert.ToInt64(x.ID));
                        string updateSql = "UPDATE tblSettingsApi SET LastACCReceipt = @MaxID";
                        using (var cmdUpdate = new SqlCommand(updateSql, sqlConn, trans))
                        {
                            cmdUpdate.CommandTimeout = 600;
                            cmdUpdate.Parameters.AddWithValue("@MaxID", maxId);
                            await cmdUpdate.ExecuteNonQueryAsync();
                        }

                        trans.Commit();
                        response.Success = true;
                        response.Message = $"{newData.Count} receipts loaded successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Cross-DB Error: " + ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> GetAllReceiptsAsync()
        {
            var response = new ApiResposne();
            var list = new List<object>();
            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                using SqlCommand cmd = new SqlCommand(
                    @"SELECT TOP 200 BVReceiptNo, Vendor, PO, BVReceiptDate, Part, SUM(Qty) AS Qty, ReceiptUnitCost, CMO, ItemType
                      FROM HardwareReceived
                      GROUP BY BVReceiptNo, Vendor, PO, BVReceiptDate, Part, ReceiptUnitCost, CMO, ItemType
                      ORDER BY BVReceiptDate DESC", conn);
                cmd.CommandTimeout = 600;

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapReader(reader));
                }
                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex) { response.Success = false; response.Message = ex.Message; }
            return response;
        }

        public async Task<ApiResposne> GetMissingReceiptsByPOAsync(string poNumber)
        {
            var response = new ApiResposne();
            var list = new List<object>();

            string formattedPO = poNumber.Trim().PadLeft(10, '0');

            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                string sql = @"SELECT BVReceiptNo, Vendor, PO, BVReceiptDate, Part, SUM(Qty) AS Qty, ReceiptUnitCost, CMO, ItemType
                               FROM HardwareReceived 
                               WHERE PO = @PO
                               GROUP BY BVReceiptNo, Vendor, PO, BVReceiptDate, Part, ReceiptUnitCost, CMO, ItemType";

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@PO", formattedPO);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(MapReader(reader));
                }

                response.Success = list.Any();
                response.Result = list;
                if (!list.Any()) response.Message = "No records found for this PO.";
            }
            catch (Exception ex) { response.Success = false; response.Message = ex.Message; }
            return response;
        }

        private object MapReader(SqlDataReader reader)
        {
            return new
            {
                BVReceiptNo = reader["BVReceiptNo"].ToString()?.Trim(),
                PONumber = reader["PO"].ToString()?.Trim(),
                Vendor = reader["Vendor"] != DBNull.Value ? reader["Vendor"].ToString()?.Trim() : "",
                ReceiptDate = Convert.ToDateTime(reader["BVReceiptDate"]).ToString("yyyy-MM-dd"),
                PartNo = reader["Part"].ToString()?.Trim(),
                QtyReceived = Convert.ToDouble(reader["Qty"]),
                UnitCost = Convert.ToDecimal(reader["ReceiptUnitCost"]),
                CMO = reader["CMO"].ToString()?.Trim(),
                Type = reader["ItemType"].ToString()?.Trim()
            };
        }

        public async Task<ApiResposne> GetReceiptsByTypeAsync(string type)
        {
            var response = new ApiResposne();
            var list = new List<object>();

            if (string.IsNullOrEmpty(type))
            {
                response.Success = false;
                response.Message = "Type required.";
                return response;
            }

            string itemType = type == "Hardware" ? "HDW" : "ACC";

            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                string sql = @"
                    SELECT BVReceiptNo, Vendor, PO AS PONumber, BVReceiptDate AS ReceiptDate,
                           Part AS PartNo, SUM(Qty) AS QtyReceived, ReceiptUnitCost AS UnitCost,
                           CMO, ItemType AS Type
                    FROM HardwareReceived
                    WHERE ItemType = @ItemType
                    GROUP BY BVReceiptNo, Vendor, PO, BVReceiptDate, Part, ReceiptUnitCost, CMO, ItemType";

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@ItemType", itemType);

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        BVReceiptNo = reader["BVReceiptNo"].ToString(),
                        Vendor = reader["Vendor"].ToString(),
                        PONumber = reader["PONumber"].ToString(),
                        ReceiptDate = Convert.ToDateTime(reader["ReceiptDate"]).ToString("yyyy-MM-dd"),
                        PartNo = reader["PartNo"].ToString(),
                        QtyReceived = Convert.ToDouble(reader["QtyReceived"]),
                        UnitCost = Convert.ToDecimal(reader["UnitCost"]),
                        CMO = reader["CMO"].ToString(),
                        Type = reader["Type"].ToString()
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

        public async Task<ApiResposne> FindReceiptByBVNoAsync(string bvReceiptNo)
        {
            var response = new ApiResposne();
            string formattedReceipt = bvReceiptNo.Trim().PadLeft(10, '0');

            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                string sql = @"SELECT BVReceiptNo, PO, Vendor, BVReceiptDate, Part, SUM(Qty) AS Qty, ItemType, ReceiptUnitCost, CMO
                               FROM HardwareReceived 
                               WHERE BVReceiptNo = @BVReceiptNo
                               GROUP BY BVReceiptNo, PO, Vendor, BVReceiptDate, Part, ItemType, ReceiptUnitCost, CMO";

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@BVReceiptNo", formattedReceipt);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    response.Success = true;
                    response.Result = MapReader(reader);
                }
                else
                {
                    response.Success = false;
                    response.Message = "Receipt Number not found in any category.";
                }
            }
            catch (Exception ex) { response.Success = false; response.Message = ex.Message; }
            return response;
        }

        public async Task<ApiResposne> SearchReceiptsAsync(SearchReceiptsBO request)
        {
            var response = new ApiResposne();
            var list = new List<object>();

            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                string selectSql = @"SELECT BVReceiptNo, Vendor, PO, BVReceiptDate, 
                                            Part, SUM(Qty) AS Qty, ReceiptUnitCost, CMO, ItemType
                                     FROM HardwareReceived WHERE 1=1";
                string filterSql = "";

                using SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 600;

                if (!string.IsNullOrEmpty(request.ReceiptNo))
                {
                    string formattedReceipt = request.ReceiptNo.Trim().PadLeft(10, '0');
                    filterSql += " AND BVReceiptNo = @ReceiptNo";
                    cmd.Parameters.AddWithValue("@ReceiptNo", formattedReceipt);
                }

                if (!string.IsNullOrEmpty(request.PONumber))
                {
                    string formattedPO = request.PONumber.Trim().PadLeft(10, '0');
                    filterSql += " AND PO = @PONumber";
                    cmd.Parameters.AddWithValue("@PONumber", formattedPO);
                }
                if (!string.IsNullOrEmpty(request.Type))
                {
                    string itemType = request.Type.Equals("Hardware", StringComparison.OrdinalIgnoreCase) ? "HDW" : "ACC";

                    filterSql += " AND LTRIM(RTRIM(ItemType)) = @ItemType";
                    cmd.Parameters.AddWithValue("@ItemType", itemType);
                }

                string groupSql = @" GROUP BY BVReceiptNo, Vendor, PO, BVReceiptDate, Part, ReceiptUnitCost, CMO, ItemType";

                cmd.CommandText = selectSql + filterSql + groupSql;
                cmd.Connection = conn;
                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(MapReader(reader));
                }

                response.Success = list.Any();
                response.Result = list;
                response.Message = list.Any() ? "Success" : "No records found.";
            }
            catch (Exception ex) { response.Success = false; response.Message = ex.Message; }
            return response;
        }
    }
}