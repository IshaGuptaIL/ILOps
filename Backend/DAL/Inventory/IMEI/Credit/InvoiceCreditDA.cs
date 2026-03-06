using DAL.Common.Login;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Inventory.IMEI.Credit
{
    public class InvoiceCreditDA :IInvoiceCredit
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
            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        long lastAccReceipt = 0;

                        // STEP 1: Get Last ID from SQL Server
                        using (var sqlConn = new SqlConnection(_sqlConn))
                        {
                            await sqlConn.OpenAsync();
                            string getSettingsSql = "SELECT ISNULL(LastACCReceipt, 0) FROM tblSettingsApi";
                            using (var cmd = new SqlCommand(getSettingsSql, sqlConn))
                            {
                                lastAccReceipt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                            }
                        }

                        // STEP 2: Fetch NEW data from PostgreSQL (Spire)
                        var newData = new List<AccReceiptTransferModel>();
                        using (var pgConn = new NpgsqlConnection(_conn)) // spire_Connection string use karein
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
                                pgCmd.Parameters.AddWithValue("LastID", lastAccReceipt);
                                using (var reader = await pgCmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        newData.Add(new AccReceiptTransferModel
                                        {
                                            ID = reader["id"].ToString(),
                                            PartNo = reader["part_no"]?.ToString() ?? "",

                                            // DateOnly ko DateTime mein convert karne ka sahi tarika:
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

                        // STEP 3: Insert into SQL Server & Update Settings
                        using (var sqlConn = new SqlConnection(_sqlConn))
                        {
                            await sqlConn.OpenAsync();
                            using (var trans = sqlConn.BeginTransaction())
                            {
                                try
                                {
                                    string insertSql = @"
                        INSERT INTO HardwareReceived (BVReceiptNo, Part, BVReceiptDate, PO, ReceiptUnitCost, QTY, Vendor, ItemType)
                        VALUES (@BVReceiptNo, @Part, @Date, @PO, @Cost, @Qty, @Vendor, 'ACC')";

                                    foreach (var item in newData)
                                    {
                                        using (var cmdInsert = new SqlCommand(insertSql, sqlConn, trans))
                                        {
                                            cmdInsert.Parameters.AddWithValue("@BVReceiptNo", item.ID.PadLeft(10, '0'));
                                            cmdInsert.Parameters.AddWithValue("@Part", item.PartNo);
                                            cmdInsert.Parameters.AddWithValue("@Date", item.ReceiveDate);
                                            cmdInsert.Parameters.AddWithValue("@PO", item.LinkNo);
                                            cmdInsert.Parameters.AddWithValue("@Cost", item.Cost);
                                            cmdInsert.Parameters.AddWithValue("@Qty", item.Qty);
                                            cmdInsert.Parameters.AddWithValue("@Vendor", item.VendorNo);
                                            await cmdInsert.ExecuteNonQueryAsync();
                                        }
                                    }

                                    // Update Settings with new Max ID
                                    long maxId = newData.Max(x => Convert.ToInt64(x.ID));
                                    string updateSql = "UPDATE tblSettingsApi SET LastACCReceipt = @MaxID";
                                    using (var cmdUpdate = new SqlCommand(updateSql, sqlConn, trans))
                                    {
                                        cmdUpdate.Parameters.AddWithValue("@MaxID", maxId);
                                        await cmdUpdate.ExecuteNonQueryAsync();
                                    }

                                    trans.Commit();
                                    response.Success = true;
                                    response.Message = $"{newData.Count} receipts loaded successfully.";
                                }
                                catch (Exception ex)
                                {
                                    trans.Rollback();
                                    throw;
                                }
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
            }
        }

        public async Task<ApiResposne> GetAllReceiptsAsync()
        {
            var response = new ApiResposne();
            var list = new List<object>();
            try
            {
                using SqlConnection conn = new SqlConnection(_sqlConn);
                // Dono HDW aur ACC mil sakein isliye WHERE clause nahi hai
                using SqlCommand cmd = new SqlCommand(
                    @"SELECT TOP 200 BVReceiptNo, Vendor, PO, BVReceiptDate, Part, Qty, ReceiptUnitCost, CMO, ItemType
              FROM HardwareReceived
              ORDER BY BVReceiptDate DESC", conn);

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
                string sql = @"SELECT BVReceiptNo, Vendor, PO, BVReceiptDate, Part, Qty, ReceiptUnitCost, CMO, ItemType
                           FROM HardwareReceived WHERE PO = @PO";

                using SqlCommand cmd = new SqlCommand(sql, conn);
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
        // Helper to keep code clean
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
                // Yahan Trim() nahi hoga toh "ACC    " aur "ACC" match nahi honge
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
                   Part AS PartNo, Qty AS QtyReceived, ReceiptUnitCost AS UnitCost,
                   CMO, ItemType AS Type
            FROM HardwareReceived
            WHERE ItemType = @ItemType";

                using SqlCommand cmd = new SqlCommand(sql, conn);
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
                // ItemType filter hata diya taaki search universal ho
                string sql = @"SELECT TOP 1 BVReceiptNo, PO, Vendor, BVReceiptDate, Part, Qty, ItemType, ReceiptUnitCost, CMO
                       FROM HardwareReceived 
                       WHERE BVReceiptNo = @BVReceiptNo";

                using SqlCommand cmd = new SqlCommand(sql, conn);
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
                string sql = @"SELECT DISTINCT BVReceiptNo, Vendor, PO, BVReceiptDate, 
                              Part, Qty, ReceiptUnitCost, CMO, ItemType
                       FROM HardwareReceived WHERE 1=1";

                using SqlCommand cmd = new SqlCommand();

                if (!string.IsNullOrEmpty(request.ReceiptNo))
                {
                    string formattedReceipt = request.ReceiptNo.Trim().PadLeft(10, '0');
                    sql += " AND BVReceiptNo = @ReceiptNo";
                    cmd.Parameters.AddWithValue("@ReceiptNo", formattedReceipt);
                }

                if (!string.IsNullOrEmpty(request.PONumber))
                {
                    string formattedPO = request.PONumber.Trim().PadLeft(10, '0');
                    sql += " AND PO = @PONumber";
                    cmd.Parameters.AddWithValue("@PONumber", formattedPO);
                }
                if (!string.IsNullOrEmpty(request.Type))
                {
                    string itemType = request.Type.Equals("Hardware", StringComparison.OrdinalIgnoreCase) ? "HDW" : "ACC";

                    sql += " AND LTRIM(RTRIM(ItemType)) = @ItemType";
                    cmd.Parameters.AddWithValue("@ItemType", itemType);
                }

                cmd.CommandText = sql;
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