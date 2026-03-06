using DAL.Common.Login;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System.Collections.Generic;
using NpgsqlTypes;
using System.Threading.Tasks;

namespace DAL.Inventory.IMEI.Report
{
    public class ReportsDA : IReports
    {
        private readonly string _pgConn;
        private readonly string _sqlConn;

        public ReportsDA(IConfiguration config)
        {
            _pgConn = config.GetConnectionString("spire_Connection");
            _sqlConn = config.GetConnectionString("bvactivation_Connection");
        }

        public async Task<List<InventoryStockBO>> GetInventoryStockStatus()
        {
            var list = new List<InventoryStockBO>();

            var sql = @"
            SELECT
                i.whse,
                i.part_no,
                i.description,
                i.product_code,
                i.sales_acct,
                sn.number AS serial_number,

                CASE 
                    WHEN sn.id IS NULL THEN i.onhand_qty 
                    ELSE 1 
                END AS onhand,

                CASE 
                    WHEN sn.id IS NULL THEN i.committed_qty 
                    ELSE 0 
                END AS committed,

                (
                    CASE 
                        WHEN sn.id IS NULL THEN i.onhand_qty 
                        ELSE 1 
                    END
                    -
                    CASE 
                        WHEN sn.id IS NULL THEN i.committed_qty 
                        ELSE 0 
                    END
                ) AS available,

                i.current_cost,
                i.average_cost,

                (
                    i.current_cost *
                    CASE 
                        WHEN sn.id IS NULL THEN i.onhand_qty 
                        ELSE 1 
                    END
                ) AS current_value,

                0 AS backorder,
                i.misc_1 AS group_name

            FROM inventory i
            LEFT JOIN inventory_serial_numbers sn
                ON i.part_no = sn.part_no
               AND i.whse = sn.whse
LIMIT 10;
            ";

            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var item = new InventoryStockBO
                        {
                            Whse = reader["whse"].ToString(),
                            PartNo = reader["part_no"].ToString(),
                            Description = reader["description"].ToString(),
                            ProductCode = reader["product_code"].ToString(),
                            SalesAcct = reader["sales_acct"].ToString(),
                            SerialNumber = reader["serial_number"] == DBNull.Value
                                ? null
                                : reader["serial_number"].ToString(),

                            Onhand = reader.GetInt32(reader.GetOrdinal("onhand")),
                            Committed = reader.GetInt32(reader.GetOrdinal("committed")),
                            Available = reader.GetInt32(reader.GetOrdinal("available")),

                            CurrentCost = reader.GetDecimal(reader.GetOrdinal("current_cost")),
                            AverageCost = reader.GetDecimal(reader.GetOrdinal("average_cost")),
                            CurrentValue = reader.GetDecimal(reader.GetOrdinal("current_value")),

                            Backorder = reader.GetInt32(reader.GetOrdinal("backorder")),
                            Group = reader["group_name"]?.ToString()
                        };

                        list.Add(item);
                    }
                }
            }

            return list;
        }



        public async Task<ApiResposne> GetVendors()
        {
            var response = new ApiResposne();

            try
            {
                var sql = @"
            SELECT DISTINCT Vendor
            FROM HardwareReceived
            WHERE ItemType = 'HDW'
            ORDER BY Vendor";

                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();
                    var vendors = new List<VendorBO>();

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader["Vendor"].ToString().Trim();

                            vendors.Add(new VendorBO
                            {
                                Id = name,   // 👈 Use Vendor Name as ID
                                Name = name
                            });
                        }
                    }

                    response.Success = true;
                    response.Result = vendors;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Failed to fetch vendors: {ex.Message}";
            }

            return response;
        }

        // Get Parts
        public async Task<ApiResposne> GetParts(string itemType)
        {
            var response = new ApiResposne();

            try
            {
                var sql = @"SELECT DISTINCT Part FROM HardwareReceived WHERE ItemType = @ItemType ORDER BY Part";

                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();
                    var parts = new List<string>();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ItemType", itemType);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                parts.Add(reader["Part"].ToString());
                            }
                        }
                    }

                    response.Success = true;
                    response.Message = "Parts fetched successfully";
                    response.Result = parts;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Failed to fetch parts: {ex.Message}";
            }

            return response;
        }

        public async Task<List<ReceivedReportBO>> GetReceivedReport(
         string itemType,
         string vendor,
         string part,
         DateTime? startDate,
         DateTime? endDate)
        {
            var list = new List<ReceivedReportBO>();

            try
            {
                using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                // ✅ Step 1: Get Hardware Received Data
                var hardwareSQL = @"
            SELECT 
                Vendor,
                BVReceiptNo,
                BVReceiptDate,
                CMO,
                PO,
                Part,
                ReceiptUnitCost,
                IMEI,
                Qty
            FROM HardwareReceived
            WHERE ItemType = @ItemType
        ";

                if (!string.IsNullOrEmpty(vendor))
                    hardwareSQL += " AND Vendor = @Vendor";

                if (!string.IsNullOrEmpty(part))
                    hardwareSQL += " AND Part = @Part";

                if (startDate.HasValue && endDate.HasValue)
                    hardwareSQL += " AND BVReceiptDate BETWEEN @StartDate AND @EndDate";

                using var cmd = new SqlCommand(hardwareSQL, conn);
                cmd.Parameters.AddWithValue("@ItemType", itemType);

                if (!string.IsNullOrEmpty(vendor))
                    cmd.Parameters.AddWithValue("@Vendor", vendor);

                if (!string.IsNullOrEmpty(part))
                    cmd.Parameters.AddWithValue("@Part", part);

                if (startDate.HasValue && endDate.HasValue)
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                    cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                }

                // ✅ Step 2: Get all Receipt Numbers for Rogers Invoice lookup
                var receiptNumbers = new List<string>();
                var hardwareData = new List<ReceivedReportBO>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var receipt = new ReceivedReportBO
                        {
                            Vendor = reader["Vendor"]?.ToString(),
                            BVReceiptNo = reader["BVReceiptNo"]?.ToString(),
                            BVReceiptDate = reader["BVReceiptDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["BVReceiptDate"])
                                : DateTime.MinValue,
                            CMO = reader["CMO"]?.ToString(),
                            PO = reader["PO"]?.ToString(),
                            Part = reader["Part"]?.ToString(),
                            ReceiptUnitCost = reader["ReceiptUnitCost"] != DBNull.Value
                                ? Convert.ToDecimal(reader["ReceiptUnitCost"])
                                : 0,
                            IMEI = reader["IMEI"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value
                                ? Convert.ToInt32(reader["Qty"])
                                : (int?)null
                        };

                        hardwareData.Add(receipt);

                        if (!string.IsNullOrEmpty(receipt.BVReceiptNo))
                            receiptNumbers.Add(receipt.BVReceiptNo);
                    }
                }

                // ✅ Step 3: Get Rogers Invoice Data (Manual GROUP BY)
                var rogersDataDict = new Dictionary<string, ReceivedReportBO>();

                if (receiptNumbers.Any())
                {
                    var receiptNosParam = string.Join(",", receiptNumbers.Select((_, i) => $"@receipt{i}"));

                    var rogersSQL = $@"
                SELECT 
                    BVReceiptNo,
                    TransType,
                    RefNo,
                    TransDate,
                    PerUnitAmount,
                    Remarks
                FROM tblRogersInvoice
                WHERE BVReceiptNo IN ({receiptNosParam})
                ORDER BY BVReceiptNo, ID
            ";

                    using var rogersCmd = new SqlCommand(rogersSQL, conn);

                    for (int i = 0; i < receiptNumbers.Count; i++)
                    {
                        rogersCmd.Parameters.AddWithValue($"@receipt{i}", receiptNumbers[i]);
                    }

                    using var rogersReader = await rogersCmd.ExecuteReaderAsync();

                    var tempRogersData = new List<dynamic>();

                    while (await rogersReader.ReadAsync())
                    {
                        tempRogersData.Add(new
                        {
                            BVReceiptNo = rogersReader["BVReceiptNo"]?.ToString(),
                            TransType = rogersReader["TransType"]?.ToString(),
                            RefNo = rogersReader["RefNo"]?.ToString(),
                            TransDate = rogersReader["TransDate"] != DBNull.Value
                                ? Convert.ToDateTime(rogersReader["TransDate"])
                                : (DateTime?)null,
                            PerUnitAmount = rogersReader["PerUnitAmount"] != DBNull.Value
                                ? Convert.ToDecimal(rogersReader["PerUnitAmount"])
                                : 0,
                            Remarks = rogersReader["Remarks"]?.ToString()
                        });
                    }

                    // ✅ Manual GROUP BY logic
                    var groupedRogers = tempRogersData
             .GroupBy(x => x.BVReceiptNo)
             .Select(g =>
             {
                 var firstRecord = g.First();

                 // ✅ Fix: Explicitly cast to decimal
                 decimal rogersTotal = g.Sum(r =>
                     r.TransType == "C"
                         ? (decimal)r.PerUnitAmount * -1
                         : (decimal)r.PerUnitAmount
                 );

                 return new
                 {
                     BVReceiptNo = g.Key,
                     RogersTotal = rogersTotal,
                     RogersCount = g.Count(),
                     FirstOfTransType = firstRecord.TransType,
                     FirstOfRefNo = firstRecord.RefNo,
                     FirstOfTransDate = firstRecord.TransDate,
                     FirstOfPerUnitAmount = firstRecord.PerUnitAmount,
                     FirstOfRemarks = firstRecord.Remarks
                 };
             });

                    // Store in dictionary for easy lookup
                    foreach (var item in groupedRogers)
                    {
                        rogersDataDict[item.BVReceiptNo] = new ReceivedReportBO
                        {
                            RogersTotal = item.RogersTotal,
                            RogersCount = item.RogersCount,
                            FirstOfTransType = item.FirstOfTransType,
                            FirstOfRefNo = item.FirstOfRefNo,
                            FirstOfTransDate = item.FirstOfTransDate,
                            FirstOfPerUnitAmount = item.FirstOfPerUnitAmount,
                            FirstOfRemarks = item.FirstOfRemarks
                        };
                    }
                }

                // ✅ Step 4: Merge Hardware and Rogers data (LEFT JOIN simulation)
                foreach (var hardware in hardwareData)
                {
                    var result = new ReceivedReportBO
                    {
                        // Copy hardware fields
                        Vendor = hardware.Vendor,
                        BVReceiptNo = hardware.BVReceiptNo,
                        BVReceiptDate = hardware.BVReceiptDate,
                        CMO = hardware.CMO,
                        PO = hardware.PO,
                        Part = hardware.Part,
                        ReceiptUnitCost = hardware.ReceiptUnitCost,
                        IMEI = hardware.IMEI,
                        Qty = hardware.Qty,

                        // Default Rogers fields to null
                        RogersTotal = null,
                        RogersCount = null,
                        FirstOfTransType = null,
                        FirstOfRefNo = null,
                        FirstOfTransDate = null,
                        FirstOfPerUnitAmount = null,
                        FirstOfRemarks = null
                    };

                    // If Rogers data exists for this receipt, add it
                    if (!string.IsNullOrEmpty(hardware.BVReceiptNo) &&
                        rogersDataDict.ContainsKey(hardware.BVReceiptNo))
                    {
                        var rogers = rogersDataDict[hardware.BVReceiptNo];
                        result.RogersTotal = rogers.RogersTotal;
                        result.RogersCount = rogers.RogersCount;
                        result.FirstOfTransType = rogers.FirstOfTransType;
                        result.FirstOfRefNo = rogers.FirstOfRefNo;
                        result.FirstOfTransDate = rogers.FirstOfTransDate;
                        result.FirstOfPerUnitAmount = rogers.FirstOfPerUnitAmount;
                        result.FirstOfRemarks = rogers.FirstOfRemarks;
                    }

                    list.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetReceivedReport: {ex.Message}");
                throw;
            }

            return list;
        }
        public async Task<List<SpireReceiptBO>> GetSpireReceipts(DateOnly startDate, DateOnly endDate, string whse = "CO")
        {
            var list = new List<SpireReceiptBO>();

            var sql = @"
SELECT 
    r.id,
    r.receive_date,
    i.whse,
    i.part_no,
    i.description,
    i.product_code,
    r.qty,
    r.cost,
    r.selling,
    r.link_no,
    r.link_table,
    r.ref_no,
    r.new_average_cost,
    r.new_onhand_qty
FROM inventory_receipts r
INNER JOIN inventory i ON r.inventory_id = i.id
WHERE r.receive_date BETWEEN @StartDate AND @EndDate
  AND i.whse = @Whse
  AND r.link_table <> 'SHIS'
ORDER BY r.receive_date;
";

            using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);

            // ✅ Convert DateOnly to DateTime explicitly
            DateTime startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
            DateTime endDateTime = endDate.ToDateTime(TimeOnly.MaxValue); // Include the entire end day

            // ✅ Use NpgsqlDbType.Date explicitly
            cmd.Parameters.Add("@StartDate", NpgsqlDbType.Timestamp).Value = startDateTime;
            cmd.Parameters.Add("@EndDate", NpgsqlDbType.Timestamp).Value = endDateTime;
            cmd.Parameters.AddWithValue("@Whse", whse);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SpireReceiptBO
                {
                    Id = Convert.ToInt32(reader["id"]),
                    ReceiveDate = ((DateOnly)reader["receive_date"]).ToDateTime(TimeOnly.MinValue),
                    Whse = reader["whse"].ToString(),
                    PartNo = reader["part_no"].ToString(),
                    Description = reader["description"].ToString(),
                    ProductCode = reader["product_code"].ToString(),
                    Qty = Convert.ToDecimal(reader["qty"]),
                    Cost = Convert.ToDecimal(reader["cost"]),
                    Selling = Convert.ToDecimal(reader["selling"]),
                    LinkNo = reader["link_no"].ToString(),
                    LinkTable = reader["link_table"].ToString(),
                    RefNo = reader["ref_no"].ToString(),
                    NewAverageCost = Convert.ToDecimal(reader["new_average_cost"]),
                    NewOnhandQty = Convert.ToDecimal(reader["new_onhand_qty"])
                });
            }

            return list;
        }



        public async Task<List<HardwareReceiptBO>> GetHardwareReceipts(string receiptNo, string poNumber)
        {
            var list = new List<HardwareReceiptBO>();

            var sql = @"
        SELECT hr.Vendor,
               hr.BVReceiptNo,
               hr.BVReceiptDate,
               hr.CMO,
               hr.PO,
               hr.Part,
               hr.Qty,
               hr.ReceiptUnitCost,
               hr.IMEI,
               rt.RogersTotal,
               rt.RogersCount,
               rt.FirstOfTransType,
               rt.FirstOfRefNo,
               rt.FirstOfTransDate,
               rt.FirstOfPerUnitAmount,
               rt.FirstOfRemarks
        FROM HardwareReceived hr
        LEFT JOIN (
            SELECT BVReceiptNo,
                   SUM(CASE WHEN TransType = 'C' THEN PerUnitAmount * -1 ELSE PerUnitAmount END) AS RogersTotal,
                   COUNT(ID) AS RogersCount,
                   MIN(TransType) AS FirstOfTransType,
                   MIN(RefNo) AS FirstOfRefNo,
                   MIN(TransDate) AS FirstOfTransDate,
                   MIN(PerUnitAmount) AS FirstOfPerUnitAmount,
                   MIN(Remarks) AS FirstOfRemarks
            FROM tblRogersInvoice
            GROUP BY BVReceiptNo
        ) rt ON hr.BVReceiptNo = rt.BVReceiptNo
        WHERE (@ReceiptNo IS NULL OR hr.BVReceiptNo = @ReceiptNo)
          AND (@PONumber IS NULL OR hr.PO = @PONumber)
    ";

            using var conn = new SqlConnection(_sqlConn);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ReceiptNo", string.IsNullOrEmpty(receiptNo) ? DBNull.Value : receiptNo);
            cmd.Parameters.AddWithValue("@PONumber", string.IsNullOrEmpty(poNumber) ? DBNull.Value : poNumber);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new HardwareReceiptBO
                {
                    Vendor = reader["Vendor"].ToString(),
                    BVReceiptNo = reader["BVReceiptNo"].ToString(),
                    BVReceiptDate = reader["BVReceiptDate"] != DBNull.Value ? Convert.ToDateTime(reader["BVReceiptDate"]) : DateTime.MinValue,
                    CMO = reader["CMO"].ToString(),
                    PO = reader["PO"].ToString(),
                    Part = reader["Part"].ToString(),
                    Qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0,
                    ReceiptUnitCost = reader["ReceiptUnitCost"] != DBNull.Value ? Convert.ToDecimal(reader["ReceiptUnitCost"]) : 0,
                    IMEI = reader["IMEI"].ToString(),
                    RogersTotal = reader["RogersTotal"] != DBNull.Value ? Convert.ToDecimal(reader["RogersTotal"]) : (decimal?)null,
                    RogersCount = reader["RogersCount"] != DBNull.Value ? Convert.ToInt32(reader["RogersCount"]) : (int?)null,
                    FirstOfTransType = reader["FirstOfTransType"].ToString(),
                    FirstOfRefNo = reader["FirstOfRefNo"].ToString(),
                    FirstOfTransDate = reader["FirstOfTransDate"] != DBNull.Value ? Convert.ToDateTime(reader["FirstOfTransDate"]) : (DateTime?)null,
                    FirstOfPerUnitAmount = reader["FirstOfPerUnitAmount"] != DBNull.Value ? Convert.ToDecimal(reader["FirstOfPerUnitAmount"]) : (decimal?)null,
                    FirstOfRemarks = reader["FirstOfRemarks"].ToString()
                });
            }

            return list;
        }



    }




}