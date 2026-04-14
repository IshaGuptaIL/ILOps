using DocumentFormat.OpenXml.Drawing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using System.Data;
using System.Globalization;
using System.Drawing;

namespace DAL.Inventory.RunRate
{
    public class RunRateDA : IRunRate
    {

        private readonly string _pgConn;
        private readonly string _sqlConn;

        public RunRateDA(IConfiguration config)
        {
            _pgConn = config.GetConnectionString("spire_Connection");
            _sqlConn = config.GetConnectionString("bvactivation_Connection");
        }


        //public async Task<int> LoadRunRateDataAsync(DateTime startDate, DateTime endDate)
        //{
        //    int workingDays = 21; // default

        //    try
        //    {
        //        // ✅ 1️⃣ SQL Server part: WWSalesDetailTEMP, tblOnhandIMEIs, tblLastPOItem
        //        await using var sqlConn = new SqlConnection(_sqlConn);
        //        await sqlConn.OpenAsync();
        //        await using (var cmd = new SqlCommand("DELETE FROM WWSalesDetailTEMP;", sqlConn) { CommandTimeout = 1200 })
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }
        //        // Clear WWSalesDetailTEMP
        //        await using (var cmd = new SqlCommand("DELETE FROM WWSalesDetailTEMP;", sqlConn))
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // Insert into WWSalesDetailTEMP
        //        var sqlInsertSales = @"
        //    INSERT INTO WWSalesDetailTEMP ([number], RECNO, in_date, whse, code, BVCMTDQTY, BVUnitPrice)
        //    SELECT invoice_no, sequence, invoice_date, whse, part_no, committed_qty, unit_price
        //    FROM public_sales_history
        //    INNER JOIN public_sales_history_items
        //        ON public_sales_history.invoice_no = public_sales_history_items.invoice_no
        //    WHERE invoice_date BETWEEN @StartDate AND @EndDate;
        //";
        //        await using (var cmd = new SqlCommand(sqlInsertSales, sqlConn))
        //        {
        //            cmd.Parameters.AddWithValue("@StartDate", startDate);
        //            cmd.Parameters.AddWithValue("@EndDate", endDate);
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // Clear and populate tblOnhandIMEIs
        //        await using (var cmd = new SqlCommand("DELETE FROM tblOnhandIMEIs;", sqlConn))
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }
        //        await using (var cmd = new SqlCommand("INSERT INTO tblOnhandIMEIs SELECT * FROM qryMakeOnhandIMEIs2;", sqlConn))
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // Calculate working days
        //        await using (var cmd = new SqlCommand("SELECT COUNT(DISTINCT in_date) FROM WWSalesDetailTEMP;", sqlConn))
        //        {
        //            var wdResult = await cmd.ExecuteScalarAsync();
        //            workingDays = wdResult != DBNull.Value ? Convert.ToInt32(wdResult) : 21;
        //        }

        //        // LastPOItem queries
        //        var lastPOQueries = new[]
        //        {
        //    "DELETE FROM tblLastPOItem;",
        //    "INSERT INTO tblLastPOItem SELECT * FROM LastPOItemNEW;",
        //    "INSERT INTO tblLastPOItem SELECT * FROM qryLastPOItem2New;",
        //    @"
        //    UPDATE tblLastPOItem
        //    SET RECNO = purchase_history_items.sequence,
        //        POQty = purchase_history_items.order_qty,
        //        PODate = purchase_history.date
        //    FROM tblLastPOItem
        //    INNER JOIN purchase_history_items 
        //        ON tblLastPOItem.NUMBER = purchase_history_items.po_number
        //        AND tblLastPOItem.CODE = purchase_history_items.part_no
        //    INNER JOIN purchase_history 
        //        ON purchase_history_items.po_number = purchase_history.po_number;
        //    "
        //};

        //        foreach (var q in lastPOQueries)
        //        {
        //            await using var cmd = new SqlCommand(q, sqlConn);
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // ✅ 2️⃣ PostgreSQL part: inventory, purchase_history, public_sales_history
        //        await using var pgConn = new NpgsqlConnection(_pgConn);
        //        await pgConn.OpenAsync();

        //        // Example: Update qryAccessories
        //        var sqlAccessories = $@"
        //    UPDATE qryAccessories
        //    SET ""SQL"" = 'SELECT Max(inventory.misc_1) AS ""Group"", Max(inventory.product_code) AS PROD,
        //                    inventory.part_no AS CODE, Max(inventory.description) AS inv_description,
        //                    Round(Sum(inventory.onhand_qty)/{workingDays},2) AS AvgDailySales
        //                    FROM inventory
        //                    WHERE inventory.product_code IN (''ACC'',''OBA'')
        //                    GROUP BY inventory.part_no;';
        //";
        //        await using (var cmd = new NpgsqlCommand(sqlAccessories, pgConn))
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // Example: Update qryHardware
        //        var sqlHardware = $@"
        //    UPDATE qryHardware
        //    SET ""SQL"" = 'SELECT Max(inventory.misc_1) AS Manufacturer, Max(inventory.product_code) AS PROD,
        //                    inventory.part_no AS CODE, Max(inventory.description) AS inv_description,
        //                    Round(Sum(inventory.onhand_qty)/{workingDays},2) AS AvgDailySales
        //                    FROM inventory
        //                    WHERE inventory.product_code IN (''HCC'',''OBH'')
        //                    GROUP BY inventory.part_no;';
        //";
        //        await using (var cmd = new NpgsqlCommand(sqlHardware, pgConn))
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error loading Run Rate data: {ex.Message}");
        //        throw;
        //    }

        //    return workingDays;
        //}




        public async Task<int> LoadRunRateDataAsync(DateTime startDate, DateTime endDate, int createdId)
        {
            try
            {
                await using var sqlConn = new SqlConnection(_sqlConn);
                await sqlConn.OpenAsync();

                await using var pgConn = new NpgsqlConnection(_pgConn);
                await pgConn.OpenAsync();

                // 1. Clear SQL Temp Tables for current user only
                string[] tablesToClear = { "WWSalesDetailTEMP", "tblOnhandIMEIs", "tblLastPOItem" };
                foreach (var table in tablesToClear)
                {
                    await using var cmd = new SqlCommand($"DELETE FROM {table} WHERE Created_by = @UserId;", sqlConn) { CommandTimeout = 600 };
                    cmd.Parameters.AddWithValue("@UserId", createdId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Load Sales Data (Spire -> SQL Server)
                // Matches VBA: public_sales_history INNER JOIN public_sales_history_items
                var pgSalesQuery = @"
                    SELECT 
                        h.invoice_no AS NUMBER,
                        COALESCE(i.sequence, 0) AS RECNO,
                        h.invoice_date::timestamp AS IN_DATE,
                        COALESCE(h.whse, '') AS WHSE,
                        COALESCE(i.part_no, '') AS CODE,
                        COALESCE(i.committed_qty, 0) AS BVCMTDQTY,
                        COALESCE(i.unit_price, 0) AS BVUNITPRICE
                    FROM sales_history h
                    INNER JOIN sales_history_items i ON h.invoice_no = i.invoice_no
                    WHERE h.invoice_date BETWEEN @StartDate AND @EndDate;";

                var dtSales = new DataTable();
                dtSales.Columns.Add("NUMBER", typeof(string));
                dtSales.Columns.Add("RECNO", typeof(int));
                dtSales.Columns.Add("IN_DATE", typeof(DateTime));
                dtSales.Columns.Add("WHSE", typeof(string));
                dtSales.Columns.Add("CODE", typeof(string));
                dtSales.Columns.Add("BVCMTDQTY", typeof(decimal));
                dtSales.Columns.Add("BVUNITPRICE", typeof(decimal));
                dtSales.Columns.Add("Created_by", typeof(int));

                await using (var pgCmd = new NpgsqlCommand(pgSalesQuery, pgConn))
                {
                    pgCmd.Parameters.AddWithValue("@StartDate", startDate);
                    pgCmd.Parameters.AddWithValue("@EndDate", endDate);
                    await using var reader = await pgCmd.ExecuteReaderAsync();
                    dtSales.Load(reader);

                    foreach (DataRow row in dtSales.Rows)
                    {
                        row["Created_by"] = createdId;
                    }

                    Console.WriteLine($"[RunRate] Spire Sales Rows Found: {dtSales.Rows.Count} for range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                }

                using (var bulkSales = new SqlBulkCopy(sqlConn))
                {
                    bulkSales.DestinationTableName = "WWSalesDetailTEMP";
                    bulkSales.BulkCopyTimeout = 600;

                    bulkSales.ColumnMappings.Add("NUMBER", "NUMBER");
                    bulkSales.ColumnMappings.Add("RECNO", "RECNO");
                    bulkSales.ColumnMappings.Add("IN_DATE", "IN_DATE");
                    bulkSales.ColumnMappings.Add("WHSE", "WHSE");
                    bulkSales.ColumnMappings.Add("CODE", "CODE");
                    bulkSales.ColumnMappings.Add("BVCMTDQTY", "BVCMTDQTY");
                    bulkSales.ColumnMappings.Add("BVUNITPRICE", "BVUNITPRICE");
                    bulkSales.ColumnMappings.Add("Created_by", "Created_by");

                    await bulkSales.WriteToServerAsync(dtSales);

                    Console.WriteLine("[RunRate] Successfully bulk copied sales data to WWSalesDetailTEMP.");
                }



                // 3. Load Onhand IMEI Data (Spire -> SQL Server)
                // Matches qryMakeOnhandIMEIs2
                var pgIMEIQuery = @"
                    SELECT whse AS WAREHOUSE, part_no, number AS NUMBER
                    FROM inventory_serial_numbers
                    WHERE whse NOT IN ('ZZ','FR')
                    AND pending_receipt = 0
                    AND committed_qty = 0
                    AND temp_qty = 0
                    AND onhand_qty <> 0;";

                var dtIMEI = new DataTable();
                dtIMEI.Columns.Add("WAREHOUSE", typeof(string));
                dtIMEI.Columns.Add("part_no", typeof(string));
                dtIMEI.Columns.Add("NUMBER", typeof(string));
                dtIMEI.Columns.Add("Created_by", typeof(int));

                await using (var pgCmd = new NpgsqlCommand(pgIMEIQuery, pgConn))
                {
                    await using var reader = await pgCmd.ExecuteReaderAsync();
                    dtIMEI.Load(reader);
                    foreach (DataRow row in dtIMEI.Rows) row["Created_by"] = createdId;
                    Console.WriteLine($"runrate imei count:{dtIMEI.Rows.Count}");
                }

                using (var bulkIMEI = new SqlBulkCopy(sqlConn))
                {
                    bulkIMEI.DestinationTableName = "tblOnhandIMEIs";
                    bulkIMEI.BulkCopyTimeout = 600;
                    bulkIMEI.ColumnMappings.Add("WAREHOUSE", "WAREHOUSE");
                    bulkIMEI.ColumnMappings.Add("part_no", "PART_NO");
                    bulkIMEI.ColumnMappings.Add("NUMBER", "NUMBER");
                    bulkIMEI.ColumnMappings.Add("Created_by", "Created_by");
                    await bulkIMEI.WriteToServerAsync(dtIMEI);
                    Console.WriteLine("tblOnhandIMEIs done");
                }

                // 4. Calculate Working Days (filtered by current user)
                int workingDays = 0;
                await using (var cmd = new SqlCommand("SELECT COUNT(DISTINCT IN_DATE) FROM WWSalesDetailTEMP WHERE Created_by = @UserId;", sqlConn))
                {
                    cmd.Parameters.AddWithValue("@UserId", createdId);
                    var result = await cmd.ExecuteScalarAsync();
                    workingDays = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }

                // 5. Load Last PO Data (Spire -> SQL Server)
                // Matches LastPOItemNEW and qryLastPOItem2New
                var pgLastPO1Query = @"
                    SELECT part_no AS CODE, 1 AS LastNumber, MAX(po_number) AS NUMBER
                    FROM purchase_history_items
                    GROUP BY part_no;";

                var dtLastPO = new DataTable();
                dtLastPO.Columns.Add("CODE", typeof(string));
                dtLastPO.Columns.Add("LastNumber", typeof(int));
                dtLastPO.Columns.Add("NUMBER", typeof(string));
                dtLastPO.Columns.Add("Created_by", typeof(int));

                await using (var pgCmd = new NpgsqlCommand(pgLastPO1Query, pgConn))
                {
                    await using var reader = await pgCmd.ExecuteReaderAsync();
                    dtLastPO.Load(reader);
                    foreach (DataRow row in dtLastPO.Rows) row["Created_by"] = createdId;
                }

                using (var bulkPO = new SqlBulkCopy(sqlConn))
                {
                    bulkPO.DestinationTableName = "tblLastPOItem";
                    bulkPO.BulkCopyTimeout = 600;
                    bulkPO.ColumnMappings.Add("CODE", "CODE");
                    bulkPO.ColumnMappings.Add("LastNumber", "LastNumber");
                    bulkPO.ColumnMappings.Add("NUMBER", "NUMBER");
                    bulkPO.ColumnMappings.Add("Created_by", "Created_by");
                    await bulkPO.WriteToServerAsync(dtLastPO);
                }

                // Next Max PO logic
                var pgLastPO2Query = @"
                    WITH MaxPOs AS (
                        SELECT part_no, MAX(po_number) as max_po
                        FROM purchase_history_items
                        GROUP BY part_no
                    )
                    SELECT phi.part_no AS CODE, 2 AS LastNumber, MAX(phi.po_number) AS NUMBER
                    FROM purchase_history_items phi
                    INNER JOIN MaxPOs m ON phi.part_no = m.part_no
                    WHERE phi.po_number < m.max_po AND phi.product_code = 'ACC'
                    GROUP BY phi.part_no;";

                await using (var pgCmd = new NpgsqlCommand(pgLastPO2Query, pgConn))
                {
                    var dtNextPO = new DataTable();
                    dtNextPO.Columns.Add("CODE", typeof(string));
                    dtNextPO.Columns.Add("LastNumber", typeof(int));
                    dtNextPO.Columns.Add("NUMBER", typeof(string));
                    dtNextPO.Columns.Add("Created_by", typeof(int));

                    await using var reader = await pgCmd.ExecuteReaderAsync();
                    dtNextPO.Load(reader);
                    foreach (DataRow row in dtNextPO.Rows) row["Created_by"] = createdId;

                    using (var bulkPO = new SqlBulkCopy(sqlConn))
                    {
                        bulkPO.DestinationTableName = "tblLastPOItem";
                        bulkPO.BulkCopyTimeout = 600;
                        bulkPO.ColumnMappings.Add("CODE", "CODE");
                        bulkPO.ColumnMappings.Add("LastNumber", "LastNumber");
                        bulkPO.ColumnMappings.Add("NUMBER", "NUMBER");
                        bulkPO.ColumnMappings.Add("Created_by", "Created_by");
                        await bulkPO.WriteToServerAsync(dtNextPO);
                    }
                }

                // 6. Update PO Details (filter by CreatedBy)
                var foundPOs = new List<string>();
                await using (var cmd = new SqlCommand("SELECT DISTINCT NUMBER FROM tblLastPOItem WHERE NUMBER IS NOT NULL AND Created_by = @UserId;", sqlConn) { CommandTimeout = 600 })
                {
                    cmd.Parameters.AddWithValue("@UserId", createdId);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) foundPOs.Add(reader["NUMBER"].ToString());
                }

                if (foundPOs.Count > 0)
                {
                    var dtDetails = new DataTable();
                    var pgDetailQuery = @"
                        SELECT phi.po_number AS ""NUMBER"", phi.part_no AS ""CODE"", phi.sequence AS ""RECNO"", 
                               phi.order_qty AS ""POQty"", ph.date AS ""PODate""
                        FROM purchase_history_items phi
                        INNER JOIN purchase_history ph ON phi.po_number = ph.po_number
                        WHERE phi.po_number = ANY(@POs);";

                    await using (var pgCmd = new NpgsqlCommand(pgDetailQuery, pgConn))
                    {
                        pgCmd.Parameters.AddWithValue("@POs", foundPOs.ToArray());
                        await using var reader = await pgCmd.ExecuteReaderAsync();
                        dtDetails.Load(reader);
                    }

                    await using (var cmdCreate = new SqlCommand(@"
                        IF OBJECT_ID('tempdb..#PODetails') IS NOT NULL DROP TABLE #PODetails;
                        CREATE TABLE #PODetails (NUMBER varchar(50), CODE varchar(50), RECNO int, POQty decimal(18,2), PODate datetime);
                    ", sqlConn) { CommandTimeout = 600 }) await cmdCreate.ExecuteNonQueryAsync();

                    if (dtDetails.Columns["PODate"].DataType != typeof(DateTime))
                    {
                        dtDetails.Columns.Add("PODate_TMP", typeof(DateTime));

                        foreach (DataRow row in dtDetails.Rows)
                        {
                            if (row["PODate"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["PODate"].ToString()))
                            {
                                row["PODate_TMP"] = DateTime.ParseExact(
                                    row["PODate"].ToString(),
                                    "dd-MM-yyyy",
                                    CultureInfo.InvariantCulture
                                );
                            }
                            else
                            {
                                row["PODate_TMP"] = DBNull.Value;
                            }
                        }

                        dtDetails.Columns.Remove("PODate");
                        dtDetails.Columns["PODate_TMP"].ColumnName = "PODate";
                    }

                    // Bulk copy
                    using (var bulkDetails = new SqlBulkCopy(sqlConn))
                    {
                        bulkDetails.DestinationTableName = "#PODetails";
                        bulkDetails.BulkCopyTimeout = 600;

                        bulkDetails.ColumnMappings.Add("NUMBER", "NUMBER");
                        bulkDetails.ColumnMappings.Add("CODE", "CODE");
                        bulkDetails.ColumnMappings.Add("RECNO", "RECNO");
                        bulkDetails.ColumnMappings.Add("POQty", "POQty");
                        bulkDetails.ColumnMappings.Add("PODate", "PODate");

                        bulkDetails.WriteToServer(dtDetails);
                    }

                    await using (var cmdUpdate = new SqlCommand(@"
                        UPDATE lpo
                        SET lpo.RECNO = d.RECNO,
                            lpo.POQty = d.POQty,
                            lpo.PODate = d.PODate
                        FROM tblLastPOItem lpo
                        INNER JOIN #PODetails d ON lpo.NUMBER = d.NUMBER AND lpo.CODE = d.CODE
                        WHERE lpo.Created_by = @UserId;
                    ", sqlConn) { CommandTimeout = 600 })
                    {
                        cmdUpdate.Parameters.AddWithValue("@UserId", createdId);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                }

                return workingDays;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error LoadRunRateDataAsync: {ex.Message}");
                throw;
            }
        }
        public async Task<List<RunRateItem>> GetWFHInventoryAsync()
        {

            var result = new List<RunRateItem>();

            var sql = @"
        SELECT part_no, description, onhand_qty
        FROM INVENTORY
        WHERE whse = @Whse AND misc_1 = @Misc1
    ";

            try
            {
                await using var conn = new NpgsqlConnection(_pgConn);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Whse", "CO");
                cmd.Parameters.AddWithValue("@Misc1", "WORK FROM HOME");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var item = new RunRateItem
                    {
                        Code = reader["part_no"]?.ToString(),
                        Description = reader["description"]?.ToString(),
                        OnHand = reader["onhand_qty"] != DBNull.Value ? Convert.ToDecimal(reader["onhand_qty"]) : 0
                    };

                    result.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching WFH Inventory: {ex.Message}");
                throw; // API will return 500 so Angular shows error
            }

            return result;
        }


        // ✅ Export to Excel using EPPlus (like VBA)
        

        public async Task<List<HardwareRunRateItem>> GetHardwareAsync(int createdId)
        {
            var result = new List<HardwareRunRateItem>();

            // 1. Calculate Working Days from SQL Server
            int workingDays = 0;
            var salesData = new Dictionary<string, decimal>();

            await using (var sqlConn = new SqlConnection(_sqlConn))
            {
                await sqlConn.OpenAsync();

                // Working Days
                await using (var cmdWD = new SqlCommand("SELECT COUNT(DISTINCT IN_DATE) FROM WWSalesDetailTEMP WHERE Created_by = @UserId;", sqlConn))
                {
                    cmdWD.Parameters.AddWithValue("@UserId", createdId);
                    var wd = await cmdWD.ExecuteScalarAsync();
                    workingDays = wd != DBNull.Value ? Convert.ToInt32(wd) : 0;
                }

                // Sales Data
                var sqlSales = @"
                    SELECT CODE, SUM(BVCMTDQTY) AS TotalUnitSales
                    FROM WWSalesDetailTEMP
                    WHERE WHSE NOT IN ('ZZ','FR') AND Created_by = @UserId
                    GROUP BY CODE;";

                await using var cmdSales = new SqlCommand(sqlSales, sqlConn);
                cmdSales.Parameters.AddWithValue("@UserId", createdId);
                await using var reader = await cmdSales.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salesData[reader["CODE"].ToString()] = reader["TotalUnitSales"] != DBNull.Value ? Convert.ToDecimal(reader["TotalUnitSales"]) : 0;
                }
            }

            if (workingDays <= 0) workingDays = 1; // Avoid division by zero

            // 2. Fetch inventory from PostgreSQL
            var sqlPg = @"
                SELECT 
                    MAX(i.misc_1) AS Manufacturer,
                    MAX(i.product_code) AS PROD,
                    i.part_no AS CODE,
                    MAX(i.description) AS inv_description,
                    MAX(CASE WHEN i.whse = 'CO' THEN i.current_cost ELSE 0 END) AS Cost,
                    SUM(i.onhand_qty) AS OnHand
                FROM inventory i
                WHERE i.product_code IN ('HCC','OBH')
                  AND i.part_no NOT IN ('RETURNGOV','RETURNONMGS')
                  AND i.whse NOT IN ('FR','COADV')
                GROUP BY i.part_no
                HAVING SUM(i.onhand_qty) <> 0 OR EXISTS (
                    -- This matches VBA's HAVING clause logic (OnHand <> 0 OR TotalSales <> 0)
                    -- TotalSales check is done in C# merge, but we fetch all with onhand here.
                    SELECT 1 FROM sales_history_items shi WHERE shi.part_no = i.part_no LIMIT 1
                );";

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            await using var pgCmd = new NpgsqlCommand(sqlPg, pgConn) { CommandTimeout = 600 };
            await using var pgReader = await pgCmd.ExecuteReaderAsync();
            while (await pgReader.ReadAsync())
            {
                var code = pgReader["CODE"].ToString();
                var onHand = pgReader["OnHand"] != DBNull.Value ? Convert.ToDecimal(pgReader["OnHand"]) : 0;
                var totalSales = salesData.ContainsKey(code) ? salesData[code] : 0;

                // VBA Calculations
                // AvgDailySales = TotalSales / WorkingDays
                // WeeklyRunRate = AvgDailySales * 5
                // WeeksAvailable = OnHand / WeeklyRunRate

                var avgDailySales = workingDays > 0
      ? Math.Round(totalSales / workingDays, 2)
      : 0;
                var weeklyRunRate = Math.Round(avgDailySales * 5, 2);
                var weeksAvailable = weeklyRunRate != 0 ? Math.Round(onHand / weeklyRunRate, 1) : 0;

                result.Add(new HardwareRunRateItem
                {
                    Manufacturer = pgReader["Manufacturer"].ToString(),
                    PROD = pgReader["PROD"].ToString(),
                    CODE = code,
                    Description = pgReader["inv_description"].ToString(),
                    Cost = pgReader["Cost"] != DBNull.Value ? Convert.ToDecimal(pgReader["Cost"]) : 0,
                    OnHand = onHand,
                    TotalSales = totalSales,
                    AvgDailySales = avgDailySales,
                    WeeklyRunRate = weeklyRunRate,
                    WeeksAvailable = weeksAvailable
                });
            }

            return result;
        }
        public async Task<byte[]> ExportHardwareExcel(Stream templateStream, int createdId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Fetch ALL data for export
            var hardwarePaged = await GetHardwareViewAsync(1, 1000000, createdId);
            var data = hardwarePaged.Items;

            if (data == null || data.Count == 0)
                throw new Exception("No data found");

            using var package = new ExcelPackage(templateStream);
            var ws = package.Workbook.Worksheets[0];

            // Header date
            ws.Cells[3, 1].Value = "AS ON " + DateTime.Now.ToString("MMM dd, yyyy");

            int row = 6;
            foreach (var item in data)
            {
                // Data fill karo (existing)
                ws.Cells[row, 1].Value = item.Manufacturer;
                ws.Cells[row, 2].Value = item.CODE;
                ws.Cells[row, 3].Value = item.Description;
                ws.Cells[row, 4].Value = item.OnHand;
                ws.Cells[row, 5].Value = item.AvgDailySales;
                ws.Cells[row, 6].Value = item.WeeklyRunRate;
                ws.Cells[row, 7].Value = item.TotalSales;
                ws.Cells[row, 8].Value = item.WeeklyRunRate == 0 ? "NA" : item.WeeksAvailable.ToString();

                // ← NEW: Alternating colors
                ws.Cells[row, 1, row, 8].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                if (row % 2 == 0)
                {
                    ws.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(Color.White);
                }
                else
                {
                    ws.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 206, 204));
                    ws.Cells[row, 1, row, 8].Style.Font.Color.SetColor(Color.White);  
                }

                row++;
            }

            // Total Row
            ws.Cells[row, 3].Value = "Total:";
            ws.Cells[row, 4].Value = data.Sum(x => x.OnHand);
            ws.Cells[row, 3, row, 4].Style.Font.Bold = true;
            ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";

            return package.GetAsByteArray();
        }



        public async Task<List<RunRateItemBO>> GetAccessoriesAsync(int createdId)
        {
            var result = new List<RunRateItemBO>();

            // 1. Calculate Working Days from SQL Server
            int workingDays = 0;
            var salesData = new Dictionary<string, decimal>();

            await using (var sqlConn = new SqlConnection(_sqlConn))
            {
                await sqlConn.OpenAsync();
                await using (var cmdWD = new SqlCommand("SELECT COUNT(DISTINCT IN_DATE) FROM WWSalesDetailTEMP WHERE Created_by = @UserId;", sqlConn))
                {
                    cmdWD.Parameters.AddWithValue("@UserId", createdId);
                    var wd = await cmdWD.ExecuteScalarAsync();
                    workingDays = wd != DBNull.Value ? Convert.ToInt32(wd) : 0;
                }

                var sqlSales = @"
                    SELECT CODE, SUM(BVCMTDQTY) AS TotalUnitSales
                    FROM WWSalesDetailTEMP
                    WHERE WHSE NOT IN ('ZZ','FR') AND Created_by = @UserId
                    GROUP BY CODE;";

                await using var cmdSales = new SqlCommand(sqlSales, sqlConn);
                cmdSales.Parameters.AddWithValue("@UserId", createdId);
                await using var reader = await cmdSales.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salesData[reader["CODE"].ToString()] = reader["TotalUnitSales"] != DBNull.Value ? Convert.ToDecimal(reader["TotalUnitSales"]) : 0;
                }
            }

            if (workingDays <= 0) workingDays = 1;

            // 2. Fetch inventory from PostgreSQL
            var sqlPg = @"
                SELECT 
                    MAX(i.misc_1) AS GroupName,
                    MAX(i.product_code) AS Prod,
                    i.part_no AS CODE,
                    MAX(i.description) AS inv_description,
                    MAX(CASE WHEN i.whse = 'CO' THEN i.current_cost ELSE 0 END) AS Cost,
                    SUM(i.onhand_qty) AS OnHand
                FROM inventory i
                WHERE i.product_code IN ('ACC','OBA')
                  AND i.whse NOT IN ('ZZ','FR','COADV')
                GROUP BY i.part_no
                HAVING (MAX(i.misc_1) NOT IN ('SPECIAL','DEPLOYMENT','STAGING','LICENSE','WORK FROM HOME') AND SUM(i.onhand_qty) <> 0)
                   OR (MAX(i.misc_1) NOT IN ('SPECIAL','DEPLOYMENT','STAGING','LICENSE','WORK FROM HOME') AND EXISTS (
                        SELECT 1 FROM sales_history_items shi WHERE shi.part_no = i.part_no LIMIT 1
                   ));";

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();
            await using var pgCmd = new NpgsqlCommand(sqlPg, pgConn) { CommandTimeout = 600 };
            await using var pgReader = await pgCmd.ExecuteReaderAsync();
            while (await pgReader.ReadAsync())
            {
                var code = pgReader["CODE"].ToString();
                if (code == "SHIPPING") continue;

                var onHand = pgReader["OnHand"] != DBNull.Value ? Convert.ToDecimal(pgReader["OnHand"]) : 0;
                var totalSales = salesData.ContainsKey(code) ? salesData[code] : 0;

                var avgDailySales = workingDays > 0
     ? Math.Round(totalSales / workingDays, 2)
     : 0;
                var weeklyRunRate = Math.Round(avgDailySales * 5, 2);
                var weeksAvailable = weeklyRunRate != 0 ? Math.Round(onHand / weeklyRunRate, 1) : 0;

                result.Add(new RunRateItemBO
                {
                    Group = pgReader["GroupName"].ToString(),
                    Prod = pgReader["Prod"].ToString(),
                    Code = code,
                    Description = pgReader["inv_description"].ToString(),
                    Cost = pgReader["Cost"] != DBNull.Value ? Convert.ToDecimal(pgReader["Cost"]) : 0,
                    OnHand = onHand,
                    TotalSales = totalSales,
                    AvgDailySales = avgDailySales,
                    WeeklyRunRate = weeklyRunRate,
                    WeeksAvailable = totalSales == 0 ? 0 : weeksAvailable
                });
            }

            return result;
        }
        public async Task<byte[]> ExportAccessoriesExcel(Stream templateStream, int createdId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var pagedData = await GetAccessoriesAsyncView(1, 1000000, createdId);
            var data = pagedData.Items;
            if (data == null || data.Count == 0)
                throw new Exception("No data found");

            using var package = new ExcelPackage(templateStream);
            var ws = package.Workbook.Worksheets[0];

            // Header date
            ws.Cells[3, 1].Value = "AS ON " + DateTime.Now.ToString("MMM dd, yyyy");

            int row = 6;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.Group;
                ws.Cells[row, 2].Value = item.PROD;
                ws.Cells[row, 3].Value = item.CODE;
                ws.Cells[row, 4].Value = item.Description;
                ws.Cells[row, 5].Value = item.OnHand;
                ws.Cells[row, 6].Value = item.AvgDailySales;
                ws.Cells[row, 7].Value = item.WeeklyRunRate;
                ws.Cells[row, 8].Value = item.TotalSales;
                ws.Cells[row, 9].Value = item.WeeksAvailable.ToString();

                row++;
            }

            // Total Row
            ws.Cells[row, 4].Value = "Total:";
            ws.Cells[row, 5].Value = data.Sum(x => x.OnHand);
            ws.Cells[row, 4, row, 5].Style.Font.Bold = true;
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0";

            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportAccessoriesRogersExcel(Stream templateStream, int createdId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var pagedData = await GetAccessoriesAsyncView(1, 1000000, createdId);
            var data = pagedData.Items;
            if (data == null || data.Count == 0)
                throw new Exception("No data found");

            using var package = new ExcelPackage(templateStream);
            var ws = package.Workbook.Worksheets[0];

            ws.Cells[3, 1].Value = "AS ON " + DateTime.Now.ToString("MMM dd, yyyy");

            int row = 6;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.Group;
                ws.Cells[row, 2].Value = item.PROD;
                ws.Cells[row, 3].Value = item.CODE;
                ws.Cells[row, 4].Value = item.Description;
                ws.Cells[row, 5].Value = item.OnHand;
                ws.Cells[row, 6].Value = item.AvgDailySales;
                ws.Cells[row, 7].Value = item.WeeklyRunRate;
                ws.Cells[row, 8].Value = item.TotalSales;
                ws.Cells[row, 9].Value = item.WeeksAvailable.ToString();

                ws.Cells[row, 1, row, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                if (row % 2 == 0)
                {
                    ws.Cells[row, 1, row, 9].Style.Fill.BackgroundColor.SetColor(Color.White);
                    ws.Cells[row, 1, row, 9].Style.Font.Color.SetColor(Color.Black);
                }
                else
                {
                    ws.Cells[row, 1, row, 9].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 206, 204));
                    ws.Cells[row, 1, row, 9].Style.Font.Color.SetColor(Color.Black);
                }
                row++;
            }

            ws.Cells[row, 4].Value = "Total:";
            ws.Cells[row, 5].Value = data.Sum(x => x.OnHand);
            ws.Cells[row, 4, row, 5].Style.Font.Bold = true;
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0";

            return package.GetAsByteArray();
        }

        public async Task<PagedResult<HardwareViewItem>> GetHardwareViewAsync(int pageNumber, int pageSize, int createdId)
        {
            var pagedResult = new PagedResult<HardwareViewItem>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = new List<HardwareViewItem>()
            };

            int workingDays = 0;
            var salesData = new Dictionary<string, decimal>();

            await using (var sqlConn = new SqlConnection(_sqlConn))
            {
                await sqlConn.OpenAsync();
                await using (var cmdWD = new SqlCommand("SELECT COUNT(DISTINCT IN_DATE) FROM WWSalesDetailTEMP WHERE Created_by = @UserId;", sqlConn))
                {
                    cmdWD.Parameters.AddWithValue("@UserId", createdId);
                    var wd = await cmdWD.ExecuteScalarAsync();
                    workingDays = wd != DBNull.Value ? Convert.ToInt32(wd) : 1;
                }

                var sqlSales = @"SELECT CODE, SUM(BVCMTDQTY) AS TotalUnitSales FROM WWSalesDetailTEMP WHERE WHSE NOT IN ('ZZ','FR') AND Created_by = @UserId GROUP BY CODE;";
                await using var cmdSales = new SqlCommand(sqlSales, sqlConn);
                cmdSales.Parameters.AddWithValue("@UserId", createdId);
                await using var reader = await cmdSales.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salesData[reader["CODE"].ToString()] = reader["TotalUnitSales"] != DBNull.Value ? Convert.ToDecimal(reader["TotalUnitSales"]) : 0;
                }
            }

            int offset = (pageNumber - 1) * pageSize;
            var soldCodes = salesData.Keys.ToArray();
            var sqlPg = $@"
                SELECT 
                    COUNT(*) OVER() as TotalCount,
                    MAX(i.misc_1) AS Manufacturer,
                    MAX(i.product_code) AS PROD,
                    i.part_no AS CODE,
                    MAX(i.description) AS inv_description,
                    MAX(CASE WHEN i.whse = 'CO' THEN i.current_cost ELSE 0 END) AS Cost,
                    SUM(i.onhand_qty) AS OnHand
                FROM inventory i
                WHERE i.product_code IN ('HCC','OBH')
                  AND i.part_no NOT IN ('RETURNGOV','RETURNONMGS')
                  AND i.whse NOT IN ('FR','COADV')
                GROUP BY i.part_no
                HAVING (SUM(i.onhand_qty) <> 0 OR i.part_no = ANY(@SoldCodes))
                ORDER BY i.part_no
                LIMIT {pageSize} OFFSET {offset};";

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            await using var pgCmd = new NpgsqlCommand(sqlPg, pgConn) { CommandTimeout = 600 };
            pgCmd.Parameters.AddWithValue("@SoldCodes", soldCodes);
            await using var pgReader = await pgCmd.ExecuteReaderAsync();
            while (await pgReader.ReadAsync())
            {
                pagedResult.TotalCount = Convert.ToInt32(pgReader["TotalCount"]);
                var code = pgReader["CODE"].ToString();
                var onHand = pgReader["OnHand"] != DBNull.Value ? Convert.ToDecimal(pgReader["OnHand"]) : 0;
                var totalSales = salesData.ContainsKey(code) ? salesData[code] : 0;

                var avgDailySales = workingDays > 0
    ? Math.Round(totalSales / workingDays, 2)
    : 0;
                var weeklyRunRate = Math.Round(avgDailySales * 5, 2);
                var weeksAvailable = weeklyRunRate != 0 ? Math.Round(onHand / weeklyRunRate, 1) : 0;

                pagedResult.Items.Add(new HardwareViewItem
                {
                    Manufacturer = pgReader["Manufacturer"].ToString(),
                    PROD = pgReader["PROD"].ToString(),
                    CODE = code,
                    Description = pgReader["inv_description"].ToString(),
                    Cost = pgReader["Cost"] != DBNull.Value ? Convert.ToDecimal(pgReader["Cost"]) : 0,
                    OnHand = onHand,
                    TotalSales = totalSales,
                    AvgDailySales = avgDailySales,
                    WeeklyRunRate = weeklyRunRate,
                    WeeksAvailable = weeklyRunRate == 0 ? "NA" : weeksAvailable.ToString()
                });
            }

            return pagedResult;
        }

        public async Task<PagedResult<AccessoriesRunRateItem>> GetAccessoriesAsyncView(int pageNumber, int pageSize, int createdId)
        {
            var pagedResult = new PagedResult<AccessoriesRunRateItem>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = new List<AccessoriesRunRateItem>()
            };

            int workingDays = 0;
            var salesData = new Dictionary<string, decimal>();

            await using (var sqlConn = new SqlConnection(_sqlConn))
            {
                await sqlConn.OpenAsync();
                await using (var cmdWD = new SqlCommand("SELECT COUNT(DISTINCT IN_DATE) FROM WWSalesDetailTEMP WHERE Created_by = @UserId;", sqlConn))
                {
                    cmdWD.Parameters.AddWithValue("@UserId", createdId);
                    var wd = await cmdWD.ExecuteScalarAsync();
                    workingDays = wd != DBNull.Value ? Convert.ToInt32(wd) : 1;
                }

                var sqlSales = @"SELECT CODE, SUM(BVCMTDQTY) AS TotalUnitSales FROM WWSalesDetailTEMP WHERE WHSE NOT IN ('ZZ','FR') AND Created_by = @UserId GROUP BY CODE;";
                await using var cmdSales = new SqlCommand(sqlSales, sqlConn);
                cmdSales.Parameters.AddWithValue("@UserId", createdId);
                await using var reader = await cmdSales.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salesData[reader["CODE"].ToString()] = reader["TotalUnitSales"] != DBNull.Value ? Convert.ToDecimal(reader["TotalUnitSales"]) : 0;
                }
            }

            int offset = (pageNumber - 1) * pageSize;
            var soldCodes = salesData.Keys.ToArray();
            var sqlPg = $@"
                SELECT 
                    COUNT(*) OVER() as TotalCount,
                    MAX(i.misc_1) AS ""Group"",
                    MAX(i.product_code) AS PROD,
                    i.part_no AS CODE,
                    MAX(i.description) AS inv_description,
                    MAX(CASE WHEN i.whse = 'CO' THEN i.current_cost ELSE 0 END) AS Cost,
                    SUM(i.onhand_qty) AS OnHand
                FROM inventory i
                WHERE (i.product_code IN ('ACC','OBA') AND i.whse NOT IN ('ZZ','FR','COADV'))
                GROUP BY i.part_no
                HAVING ((MAX(i.misc_1) NOT IN ('SPECIAL','DEPLOYMENT','STAGING','LICENSE','WORK FROM HOME') AND SUM(i.onhand_qty) <> 0)
                    OR (MAX(i.misc_1) NOT IN ('SPECIAL','DEPLOYMENT','STAGING','LICENSE','WORK FROM HOME') AND i.part_no = ANY(@SoldCodes)))
                ORDER BY i.part_no
                LIMIT {pageSize} OFFSET {offset};";

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            await using var pgCmd = new NpgsqlCommand(sqlPg, pgConn) { CommandTimeout = 600 };
            pgCmd.Parameters.AddWithValue("@SoldCodes", soldCodes);
            await using var pgReader = await pgCmd.ExecuteReaderAsync();
            while (await pgReader.ReadAsync())
            {
                pagedResult.TotalCount = Convert.ToInt32(pgReader["TotalCount"]);
                var code = pgReader["CODE"].ToString();
                if (code == "SHIPPING") continue;

                var onHand = pgReader["OnHand"] != DBNull.Value ? Convert.ToDecimal(pgReader["OnHand"]) : 0;
                var totalSales = salesData.ContainsKey(code) ? salesData[code] : 0;


                var avgDailySales = workingDays > 0
    ? Math.Round(totalSales / workingDays, 2)
    : 0;
                var weeklyRunRate = Math.Round(avgDailySales * 5, 2);
                var weeksAvailable = weeklyRunRate != 0 ? Math.Round(onHand / weeklyRunRate, 1) : 0;

                pagedResult.Items.Add(new AccessoriesRunRateItem
                {
                    Group = pgReader["Group"].ToString(),
                    PROD = pgReader["PROD"].ToString(),
                    CODE = code,
                    Description = pgReader["inv_description"].ToString(),
                    Cost = pgReader["Cost"] != DBNull.Value ? Convert.ToDecimal(pgReader["Cost"]) : 0,
                    OnHand = onHand,
                    TotalSales = totalSales,
                    AvgDailySales = avgDailySales,
                    WeeklyRunRate = weeklyRunRate,
                    WeeksAvailable = totalSales == 0 ? "NA" : weeksAvailable.ToString()
                });
            }

            return pagedResult;
        }
        public async Task<List<RunRateItemBO>> GetRunRateAsync(int minDays, int maxDays, int createdId)
        {
            var result = new List<RunRateItemBO>();

            // 1. Calculate Working Days and Fetch Sales/PO data from SQL Server
            int workingDays = 0;
            var salesData = new Dictionary<string, decimal>();
            var poData = new Dictionary<string, dynamic>();

            var sqlServerQuery = @"
                -- WORKING DAYS
                SELECT COUNT(DISTINCT IN_DATE) FROM WWSalesDetailTEMP WHERE Created_by = @UserId;

                -- SALES
                SELECT CODE, SUM(BVCMTDQTY) AS TotalUnitSales
                FROM WWSalesDetailTEMP
                WHERE WHSE NOT IN ('ZZ','FR') AND Created_by = @UserId
                GROUP BY CODE;

                -- PO DATA
                SELECT 
                    CODE,
                    MAX(CASE WHEN LastNumber = 1 THEN number END) AS POLast,
                    MAX(CASE WHEN LastNumber = 1 THEN POQty END) AS QtyLast,
                    MAX(CASE WHEN LastNumber = 1 THEN PODate END) AS DateLast,
                    MAX(CASE WHEN LastNumber = 1 THEN DATEDIFF(DAY, PODate, GETDATE()) END) AS AgeLast,

                    MAX(CASE WHEN LastNumber = 2 THEN number END) AS POLast2,
                    MAX(CASE WHEN LastNumber = 2 THEN POQty END) AS QtyLast2,
                    MAX(CASE WHEN LastNumber = 2 THEN PODate END) AS DateLast2,
                    MAX(CASE WHEN LastNumber = 2 THEN DATEDIFF(DAY, PODate, GETDATE()) END) AS AgeLast2
                FROM tblLastPOItem
                WHERE Created_by = @UserId
                GROUP BY CODE;
            ";

            await using (var sqlConn = new SqlConnection(_sqlConn))
            {
                await sqlConn.OpenAsync();
                await using var cmd = new SqlCommand(sqlServerQuery, sqlConn) { CommandTimeout = 600 };
                cmd.Parameters.AddWithValue("@UserId", createdId);
                await using var reader = await cmd.ExecuteReaderAsync();

                // Working Days
                if (await reader.ReadAsync())
                {
                    workingDays = reader[0] != DBNull.Value ? Convert.ToInt32(reader[0]) : 0;
                }

                // Sales
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    salesData[reader["CODE"].ToString()] = reader["TotalUnitSales"] != DBNull.Value ? Convert.ToDecimal(reader["TotalUnitSales"]) : 0;
                }

                // PO Data
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    var code = reader["CODE"].ToString();
                    poData[code] = new
                    {
                        POLast = reader["POLast"]?.ToString(),
                        QtyLast = reader["QtyLast"] != DBNull.Value ? Convert.ToDecimal(reader["QtyLast"]) : 0,
                        DateLast = reader["DateLast"] != DBNull.Value ? Convert.ToDateTime(reader["DateLast"]) : (DateTime?)null,
                        AgeLast = reader["AgeLast"] != DBNull.Value ? Convert.ToInt32(reader["AgeLast"]) : 0,

                        POLast2 = reader["POLast2"]?.ToString(),
                        QtyLast2 = reader["QtyLast2"] != DBNull.Value ? Convert.ToDecimal(reader["QtyLast2"]) : 0,
                        DateLast2 = reader["DateLast2"] != DBNull.Value ? Convert.ToDateTime(reader["DateLast2"]) : (DateTime?)null,
                        AgeLast2 = reader["AgeLast2"] != DBNull.Value ? Convert.ToInt32(reader["AgeLast2"]) : 0
                    };
                }
            }

            if (workingDays <= 0) workingDays = 1;

            // 2. Fetch inventory from PostgreSQL
            var sqlPg = @"
                SELECT 
                    i.part_no AS code,
                    MAX(i.misc_1) AS ""Group"",
                    MAX(i.product_code) AS prod,
                    MAX(i.description) AS inv_description,
                    MAX(CASE WHEN i.whse = 'CO' THEN i.current_cost ELSE 0 END) AS cost,
                    SUM(i.onhand_qty) AS qty
                FROM inventory i
                WHERE i.product_code IN ('ACC','OBA')
                  AND i.whse NOT IN ('ZZ','FR','COADV')
                GROUP BY i.part_no
                HAVING 
                    SUM(i.onhand_qty) <> 0
                    AND MAX(i.misc_1) NOT IN ('SPECIAL','DEPLOYMENT','STAGING','LICENSE','WORK FROM HOME');";

            await using var pgConn = new NpgsqlConnection(_pgConn);
            await pgConn.OpenAsync();

            await using var pgCmd = new NpgsqlCommand(sqlPg, pgConn) { CommandTimeout = 600 };
            await using var pgReader = await pgCmd.ExecuteReaderAsync();

            while (await pgReader.ReadAsync())
            {
                var code = pgReader["code"].ToString();
                if (code == "SHIPPING") continue;

                var onHand = pgReader["qty"] != DBNull.Value ? Convert.ToDecimal(pgReader["qty"]) : 0;
                var totalSales = salesData.ContainsKey(code) ? salesData[code] : 0;

                var avgDaily = Math.Round(totalSales / (decimal)workingDays, 2);
                var weeklyRun = Math.Round(avgDaily * 5, 2);
                var weeksAvailable = weeklyRun != 0 ? Math.Round(onHand / weeklyRun, 1) : 0;

                var po = poData.ContainsKey(code) ? poData[code] : null;
                int ageLast = po?.AgeLast ?? -1;
                int ageLast2 = po?.AgeLast2 ?? -1;

                if ((ageLast >= minDays && ageLast <= maxDays) || (ageLast2 >= minDays && ageLast2 <= maxDays))
                {
                    result.Add(new RunRateItemBO
                    {
                        Code = code,
                        Group = pgReader["Group"].ToString(),
                        Prod = pgReader["prod"].ToString(),
                        Description = pgReader["inv_description"].ToString(),
                        Cost = pgReader["cost"] != DBNull.Value ? Convert.ToDecimal(pgReader["cost"]) : 0,
                        OnHand = onHand,
                        TotalSales = totalSales,
                        AvgDailySales = avgDaily,
                        WeeklyRunRate = weeklyRun,
                        WeeksAvailable = totalSales == 0 ? 0 : weeksAvailable,

                        POLast = po?.POLast,
                        QtyLast = po?.QtyLast ?? 0,
                        DateLast = po?.DateLast,
                        AgeLast = ageLast,

                        POLast2 = po?.POLast2,
                        QtyLast2 = po?.QtyLast2 ?? 0,
                        DateLast2 = po?.DateLast2,
                        AgeLast2 = ageLast2
                    });
                }
            }

            return result;
        }


    }
}


