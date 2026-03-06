using DAL.Common.Login;
using DAL.Common.Spire;
using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DAL.Inventory.IMEI.RecieveIMEI
{
    public class RecieveImeiDA : IRecieveImei
    {
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public RecieveImeiDA(IConfiguration config)
        {
            _sqlConn = config.GetConnectionString("bvactivation_Connection"); // SQL Server connection string
            _pgConn = config.GetConnectionString("spire_Connection"); // Postgres connection string
        }

        // =================== CLEAR PACKING SLIP ===================
        public async Task<ApiResposne> ClearPackingSlipAsync()
        {
            var response = new ApiResposne();
            try
            {
                await ExecuteNonQueryAsync("DELETE FROM TblPackingSlip");
                response.Success = true;
                response.Message = "Packing Slip cleared";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        // =================== INSERT PACKING SLIP ===================
        public async Task<ApiResposne> InsertPackingSlipAsync(List<RecieveIMEIBO> items)
        {
            var response = new ApiResposne();
            try
            {
                using var con = new SqlConnection(_sqlConn);
                await con.OpenAsync();

                foreach (var item in items)
                {
                    using var cmd = new SqlCommand(@"
INSERT INTO TblPackingSlip(PONumber, RecNo, Whse, PartNo, GUID, IMEI, XLSRow)
VALUES (@PO, @Rec, @Whse, @Part, @Guid, @IMEI, @Row)", con);

                    cmd.Parameters.AddWithValue("@PO", item.PONumber);
                    cmd.Parameters.AddWithValue("@Rec", item.RecNo);
                    cmd.Parameters.AddWithValue("@Whse", item.Whse);
                    cmd.Parameters.AddWithValue("@Part", item.PartNo);
                    cmd.Parameters.AddWithValue("@Guid", item.GUID);
                    cmd.Parameters.AddWithValue("@IMEI", item.IMEI.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@Row", item.XLSRow);

                    await cmd.ExecuteNonQueryAsync();
                }

                response.Success = true;
                response.Message = $"Inserted {items.Count} IMEIs";
                response.Count = items.Count;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        // =================== INSERT SCAN LIST ===================
        public async Task<ApiResposne> InsertScanListAsync(List<RecieveIMEIBO> items)
        {
            var response = new ApiResposne();
            try
            {
                using var con = new SqlConnection(_sqlConn);
                await con.OpenAsync();

                // Clear existing scan list
                using var deleteCmd = new SqlCommand("DELETE FROM tblScanList", con);
                await deleteCmd.ExecuteNonQueryAsync();

                foreach (var item in items)
                {
                    using var cmd = new SqlCommand(@"
INSERT INTO tblScanList(PONumber, RecNo, Whse, PartNo, GUID, Vendor, Location, IMEI, XLSRow)
VALUES (@PO, @Rec, @Whse, @Part, @Guid, @Vendor, @Loc, @IMEI, @Row)", con);

                    cmd.Parameters.AddWithValue("@PO", item.PONumber);
                    cmd.Parameters.AddWithValue("@Rec", item.RecNo);
                    cmd.Parameters.AddWithValue("@Whse", item.Whse);
                    cmd.Parameters.AddWithValue("@Part", item.PartNo);
                    cmd.Parameters.AddWithValue("@Guid", item.GUID);
                    cmd.Parameters.AddWithValue("@Vendor", string.IsNullOrWhiteSpace(item.Vendor) ? (object)DBNull.Value : item.Vendor);
                    cmd.Parameters.AddWithValue("@Loc", string.IsNullOrWhiteSpace(item.Location) ? (object)DBNull.Value : item.Location);
                    cmd.Parameters.AddWithValue("@IMEI", item.IMEI.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@Row", item.XLSRow);

                    await cmd.ExecuteNonQueryAsync();
                }

                response.Success = true;
                response.Message = $"Inserted {items.Count} ScanList IMEIs";
                response.Count = items.Count;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        // =================== GET PURCHASE ORDERS (Postgres) ===================
        public async Task<ApiResposne> GetPurchaseOrdersAsync()
        
        {
            var response = new ApiResposne();
            try
            {
                var list = new List<object>();
                await using var con = new NpgsqlConnection(_pgConn);
                await con.OpenAsync();

                await using var cmd = new NpgsqlCommand(@"
SELECT po.po_number, po.vendor_name, po.id AS po_id, poi.id AS po_item_id,
       poi.part_no AS part_number, poi.whse AS whse, poi.order_qty AS order_qty,
       COALESCE(poi.received_qty,0) AS received_qty, poi.unit_price AS unit_cost, poi.guid
FROM purchase_orders po
JOIN purchase_order_items poi ON poi.po_number = po.po_number
WHERE po.status IN ('I','R','OPEN')
ORDER BY po.po_number DESC, poi.id
LIMIT 100", con);

                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    list.Add(new
                    {
                        poNumber = rdr["po_number"].ToString(),
                        vendor = rdr["vendor_name"].ToString(),
                        poId = rdr["po_id"],
                        poItemId = rdr["po_item_id"],
                        part = rdr["part_number"].ToString(),
                        whse = rdr["whse"].ToString(),
                        ordQty = Convert.ToInt32(rdr["order_qty"]),
                        rcvdQty = Convert.ToInt32(rdr["received_qty"]),
                        guid = rdr["guid"].ToString(),
                        unitCost = Convert.ToDecimal(rdr["unit_cost"])
                    });
                }

                response.Success = true;
                response.Message = "Purchase orders fetched";
                response.Result = list;
                response.Count = list.Count;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        // =================== MOCKED METHODS ===================
        public async Task<ApiResposne> GetIMEIGridsAsync(string poNumber)
        {
            var grids = new IMEIGridsDto();

            if (!string.IsNullOrWhiteSpace(poNumber))
            {
                poNumber = poNumber.TrimStart('0');
                if (string.IsNullOrEmpty(poNumber))
                    poNumber = "0";
            }

            await using var con = new SqlConnection(_sqlConn);
            await con.OpenAsync();

            try
            {
                // 🔥 1. SCAN LIST - VBA: tblScanList WHERE PONumber = Me.Combo3
                string sqlScanList = @"
            SELECT IMEI FROM tblScanList 
            WHERE PONumber = @poNumber";
                await using (var cmd = new SqlCommand(sqlScanList, con))
                {
                    cmd.Parameters.AddWithValue("@poNumber", poNumber);
                    await using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            grids.ScanList.Add(new IMEIItemDto { IMEI = rdr.GetString(0) });
                    }
                }

                // 🔥 2. PACKING SLIP - VBA: tblPackingSlip WHERE PONumber = Me.Combo3
                string sqlPackingSlip = @"
            SELECT IMEI FROM TblPackingSlip 
            WHERE PONumber = @poNumber";
                await using (var cmd = new SqlCommand(sqlPackingSlip, con))
                {
                    cmd.Parameters.AddWithValue("@poNumber", poNumber);
                    await using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            grids.PackingSlip.Add(new IMEIItemDto { IMEI = rdr.GetString(0) });
                    }
                }

                // 🔥 3. MATCHES - VBA: ScanList JOIN PackingSlip
                string sqlMatches = @"
            SELECT s.IMEI 
            FROM tblScanList s
            INNER JOIN TblPackingSlip p ON s.IMEI = p.IMEI
            WHERE s.PONumber = @poNumber AND p.PONumber = @poNumber";
                await using (var cmd = new SqlCommand(sqlMatches, con))
                {
                    cmd.Parameters.AddWithValue("@poNumber", poNumber);
                    await using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            grids.Matches.Add(new IMEIItemDto { IMEI = rdr.GetString(0) });
                    }
                }

                // 🔥 4. SCAN-NoPACK - VBA: txtScan_NoPackSlipCount logic
                string sqlScanNoPack = @"
            SELECT s.IMEI FROM tblScanList s
            WHERE s.PONumber = @poNumber
            AND s.IMEI NOT IN (SELECT p.IMEI FROM TblPackingSlip p WHERE p.PONumber = @poNumber)";
                await using (var cmd = new SqlCommand(sqlScanNoPack, con))
                {
                    cmd.Parameters.AddWithValue("@poNumber", poNumber);
                    await using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            grids.ScanNoPack.Add(new IMEIItemDto { IMEI = rdr.GetString(0) });
                    }
                }

                // 🔥 5. PACK-NoSCAN - VBA: txtPackingSlip_NoScanListCount logic
                string sqlPackNoScan = @"
            SELECT p.IMEI FROM TblPackingSlip p
            WHERE p.PONumber = @poNumber
            AND p.IMEI NOT IN (SELECT s.IMEI FROM tblScanList s WHERE s.PONumber = @poNumber)";
                await using (var cmd = new SqlCommand(sqlPackNoScan, con))
                {
                    cmd.Parameters.AddWithValue("@poNumber", poNumber);
                    await using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            grids.PackNoScan.Add(new IMEIItemDto { IMEI = rdr.GetString(0) });
                    }
                }

                // 🔥 6. ONHAND - VBA: WWSerialTemp + Spire (SetMode logic)
                string sqlOnhand = @"
    SELECT IMEI FROM tblScanList 
    WHERE PONumber = @poNumber";
                await using (var cmd = new SqlCommand(sqlOnhand, con))
                {
                    cmd.Parameters.AddWithValue("@poNumber", poNumber);
                    await using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                            grids.Onhand.Add(new IMEIItemDto { IMEI = rdr.GetString(0) });
                    }
                }

                return new ApiResposne
                {
                    Success = true,
                    Message = $"IMEI grids loaded for PO {poNumber}",
                    Result = grids
                };
            }
            catch (Exception ex)
            {
                return new ApiResposne
                {
                    Success = false,
                    Message = $"Error loading grids: {ex.Message}"
                };
            }
        }
        public Task<ApiResposne> ReceivePOIMEIAsync(long poId, long poItemId, List<string> imeis, bool isReversal)
        {
            var response = new ApiResposne();

            if (imeis == null || !imeis.Any())
            {
                response.Success = false;
                response.Message = "No IMEIs provided";
                response.Count = 0;
                return Task.FromResult(response);
            }

            try
            {
                // Simulated logic
                if (isReversal)
                {
                    if (imeis[0].StartsWith("ERROR"))
                    {
                        response.Success = false;
                        response.Message = "Reversal failed for IMEI " + imeis[0];
                        response.Count = 0;
                    }
                    else
                    {
                        response.Success = true;
                        response.Message = $"Reversed {imeis.Count} IMEIs successfully";
                        response.Count = imeis.Count;
                    }
                }
                else
                {
                    if (imeis[0].StartsWith("ERROR"))
                    {
                        response.Success = false;
                        response.Message = "Receipt failed for IMEI " + imeis[0];
                        response.Count = 0;
                    }
                    else
                    {
                        response.Success = true;
                        response.Message = $"Received {imeis.Count} IMEIs successfully";
                        response.Count = imeis.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Exception: {ex.Message}";
                response.Count = 0;
            }

            return Task.FromResult(response);
        }


        public async Task<ApiResposne> PostReceiptsAsync(long poId, long poItemId, string cmo, bool isReversal)
        {
            var response = new ApiResposne();

            if (string.IsNullOrWhiteSpace(cmo))
            {
                response.Success = false;
                response.Message = "CMO is required";
                response.Count = 0;
                return response;
            }

            try
            {
                // SQL Server connection
                await using var con = new SqlConnection(_sqlConn);
                await con.OpenAsync();

                // 1️⃣ Check unresolved errors
                await using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM tblErrors WHERE Resolved = 0", con))
                {
                    var errorCount = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                    if (errorCount > 0)
                    {
                        response.Success = false;
                        response.Message = "Unresolved errors exist";
                        response.Count = 0;
                        return response;
                    }
                }

                // 2️⃣ Load Scan List
                var scanList = new List<(string IMEI, string Vendor, string PONumber, string PartNo)>();

                await using (var cmd = new SqlCommand("SELECT IMEI, Vendor, PONumber, PartNo FROM tblScanList", con))
                await using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        scanList.Add((
                            rdr["IMEI"].ToString(),
                            rdr["Vendor"] == DBNull.Value ? "UNKNOWN" : rdr["Vendor"].ToString(),
                            rdr["PONumber"] == DBNull.Value ? "" : rdr["PONumber"].ToString(),
                            rdr["PartNo"] == DBNull.Value ? "" : rdr["PartNo"].ToString()
                        ));
                    }
                }

                if (!scanList.Any())
                {
                    response.Success = false;
                    response.Message = "No IMEIs found";
                    response.Count = 0;
                    return response;
                }


                var receiveResult = await ReceivePOIMEIAsync(poId, poItemId, scanList.Select(x => x.IMEI).ToList(), isReversal);

                if (!receiveResult.Success)
                {
                    response.Success = false;
                    response.Message = receiveResult.Message;
                    response.Count = 0;
                    return response;
                }

                // 4️⃣ Get latest receipt no from Postgres
                long receiptNo;
                await using var pg = new NpgsqlConnection(_pgConn);
                await pg.OpenAsync();
                await using (var rcmd = new NpgsqlCommand(
                    @"SELECT inventory_receipt_id 
              FROM purchase_receipts 
              WHERE order_id=@poId 
              ORDER BY id DESC LIMIT 1", pg))
                {
                    rcmd.Parameters.AddWithValue("@poId", poId);
                    receiptNo = Convert.ToInt64(await rcmd.ExecuteScalarAsync() ?? 0);
                }

                if (receiptNo == 0)
                {
                    response.Success = false;
                    response.Message = "Receipt not found";
                    response.Count = 0;
                    return response;
                }

                // 5️⃣ Get unit cost
                decimal unitCost;
                await using (var costCmd = new NpgsqlCommand(
                    "SELECT unit_price FROM purchase_order_items WHERE id=@id", pg))
                {
                    costCmd.Parameters.AddWithValue("@id", poItemId);
                    unitCost = Convert.ToDecimal(await costCmd.ExecuteScalarAsync() ?? 0);
                }

                // 6️⃣ Insert into HardwareReceived
                int processed = 0;
                await using (var icmd = new SqlCommand("", con))
                {
                    foreach (var item in scanList)
                    {
                        icmd.CommandText = @"
INSERT INTO HardwareReceived
(Vendor,BVReceiptNo,BVReceiptDate,CMO,PO,Part,Qty,ReceiptUnitCost,IMEI,ItemType)
VALUES
(@Vendor,@No,@Date,@CMO,@PO,@Part,@Qty,@Cost,@IMEI,'HDW')";

                        icmd.Parameters.Clear();
                        icmd.Parameters.AddWithValue("@Vendor", item.Vendor);
                        icmd.Parameters.AddWithValue("@No", receiptNo);
                        icmd.Parameters.AddWithValue("@Date", DateTime.Now);
                        icmd.Parameters.AddWithValue("@CMO", cmo);
                        icmd.Parameters.AddWithValue("@PO", item.PONumber);
                        icmd.Parameters.AddWithValue("@Part", item.PartNo);
                        icmd.Parameters.AddWithValue("@Qty", isReversal ? -1 : 1);
                        icmd.Parameters.AddWithValue("@Cost", unitCost);
                        icmd.Parameters.AddWithValue("@IMEI", item.IMEI);

                        await icmd.ExecuteNonQueryAsync();
                        processed++;
                    }
                }

                response.Success = true;
                response.Message = $"Processed {processed} IMEIs";
                response.Count = processed;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Count = 0;
            }

            return response;
        }


        public async Task<ApiResposne> CheckErrorsAsync(long poId, long poItemId, bool isReversal)
        {
            var response = new ApiResposne();
            var errors = new List<string>();
            int errorCount = 0;

            try
            {
                await using var con = new SqlConnection(_sqlConn);
                await con.OpenAsync();

                // 🧹 Clear previous errors
                await using (var cmdClear = new SqlCommand("DELETE FROM tblErrors", con))
                {
                    await cmdClear.ExecuteNonQueryAsync();
                }

                // 📝 Get Scan List count
                int scanListCount = 0;
                await using (var cmd = new SqlCommand("SELECT COUNT(*) FROM tblScanList", con))
                {
                    scanListCount = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                // 📝 Get Packing Slip count
                int packingSlipCount = 0;
                await using (var cmd = new SqlCommand("SELECT COUNT(*) FROM TblPackingSlip", con))
                {
                    packingSlipCount = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                // ========== VALIDATION 1: Scan List Empty ==========
                if (scanListCount == 0)
                {
                    errors.Add("You must import Scan List data");
                    errorCount++;
                }

                // ========== VALIDATION 2: Packing Slip Empty ==========
                if (packingSlipCount == 0)
                {
                    errors.Add("You must import Packing Slip data");
                    errorCount++;
                }

                // ========== VALIDATION 3: Scan IMEI not in Packing Slip ==========
                int scanNotInPack;
                await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM (
                SELECT IMEI FROM tblScanList
                EXCEPT
                SELECT IMEI FROM TblPackingSlip
            ) x", con))
                {
                    scanNotInPack = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                if (scanNotInPack > 0)
                {
                    errors.Add($"There are {scanNotInPack} entries on the Scan List that are not on the Packing Slip");
                    errorCount++;
                }

                // ========== VALIDATION 4: Packing Slip IMEI not in Scan List ==========
                int packNotInScan;
                await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM (
                SELECT IMEI FROM TblPackingSlip
                EXCEPT
                SELECT IMEI FROM tblScanList
            ) x", con))
                {
                    packNotInScan = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                if (packNotInScan > 0)
                {
                    errors.Add($"There are {packNotInScan} entries on the Packing Slip that are not on the Scan List");
                    errorCount++;
                }

                // ========== VALIDATION 5: Duplicate Scan List ==========
                int dupScan;
                await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM (
                SELECT IMEI FROM tblScanList
                GROUP BY IMEI HAVING COUNT(*) > 1
            ) x", con))
                {
                    dupScan = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                if (dupScan > 0)
                {
                    errors.Add($"There are {dupScan} duplicate IMEIs in the Scan List");
                    errorCount++;
                }

                // ========== VALIDATION 6: Duplicate Packing Slip ==========
                int dupPack;
                await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM (
                SELECT IMEI FROM TblPackingSlip
                GROUP BY IMEI HAVING COUNT(*) > 1
            ) x", con))
                {
                    dupPack = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                if (dupPack > 0)
                {
                    errors.Add($"There are {dupPack} duplicate IMEIs in the Packing Slip");
                    errorCount++;
                }

                // ========== VALIDATION 7: Invalid IMEI Format (Scan List) ==========
                int invalidScan;
                await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM tblScanList 
            WHERE LEN(ISNULL(IMEI,'')) < 10", con))
                {
                    invalidScan = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                if (invalidScan > 0)
                {
                    errors.Add($"There are {invalidScan} invalid entries in the Scan List");
                    errorCount++;
                }

                // ========== VALIDATION 8: Invalid IMEI Format (Packing Slip) ==========
                int invalidPack;
                await using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM TblPackingSlip 
            WHERE LEN(ISNULL(IMEI,'')) < 10", con))
                {
                    invalidPack = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }

                if (invalidPack > 0)
                {
                    errors.Add($"There are {invalidPack} invalid entries in the Packing Slip");
                    errorCount++;
                }

                // ========== FINAL RESPONSE ==========
                response.Success = true;
                response.Message = errorCount == 0 ? "No errors found" : $"{errorCount} error(s) found";
                response.Result = new
                {
                    ErrorCount = errorCount,
                    Errors = errors,
                    CanPost = errorCount == 0  // ✅ This enables Post button
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Result = new
                {
                    ErrorCount = 0,
                    Errors = new List<string>(),
                    CanPost = false
                };
            }

                return response;
        }

        // Async version of LogError
        private static async Task LogErrorAsync(SqlConnection con, string errorWhile)
        {
            await using var cmd = new SqlCommand(@"
        INSERT INTO tblErrors
        (VBCode, VBDescription, PONumber, RecNo, ErrorWhile, [RowCount], Resolved)
        VALUES
        (0, @desc, '', 0, @while, 0, 0)", con);

            cmd.Parameters.AddWithValue("@desc", errorWhile);
            cmd.Parameters.AddWithValue("@while", "CheckErrorsAsync");
            await cmd.ExecuteNonQueryAsync();
        }
        // =================== HELPER ===================
        private async Task ExecuteNonQueryAsync(string sql)
        {
            using var con = new SqlConnection(_sqlConn);
            using var cmd = new SqlCommand(sql, con);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
