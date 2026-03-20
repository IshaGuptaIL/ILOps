using DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace DAL.Inventory.Count
{
    public class CountDA : ICount
    {
        private readonly AppDBContext _context;
        private readonly string _spireConn; 
        private readonly string _sqlConn;   

        public CountDA(AppDBContext context, IConfiguration configuration)
        {
            _context = context;
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection"); 
            _spireConn = configuration.GetConnectionString("spire_Connection");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public async Task<bool> DeleteCounts(string fileName, bool isACC)
        {
            string tableName = isACC ? "tblACCCounts" : "tblCounts";

            string sql = $"DELETE FROM {tableName} WHERE CountFile = @p0";

            var rows = await _context.Database.ExecuteSqlRawAsync(sql, fileName);
            return rows > 0;
        }

        public async Task<bool> LoadSnapshot(InventorySnapshotBO options)
        {
            try
            {

//                SELECT
//    whse,
//    part_no,
//    description,
//    product_code,
//    onhand_qty,
//    current_cost,
//    average_cost,
//    misc_1
//FROM public.inventory
//WHERE product_code = 'ACC' 
//  AND whse NOT IN('ZZ', 'FR')
//ORDER BY id asc 
                if (options.LoadACC)
                {
                    await ExecuteSqlAction("TRUNCATE TABLE WWAccessories");

                    string accQuery = @"
    SELECT whse, part_no, description, product_code, onhand_qty, 
           committed_qty, backorder_qty, current_cost, average_cost, misc_1
    FROM public.inventory 
    WHERE whse NOT IN ('ZZ', 'FR') 
    AND (product_code = 'ACC' OR product_code = 'OBA' OR product_code = 'HCC' OR product_code = 'PHO')";

                    await TransferDataFromPgToSql(accQuery, "WWAccessories", mapping => {
                        mapping.Add("whse", "WHSE");
                        mapping.Add("part_no", "CODE");

                        mapping.Add("description", "Description");

                        mapping.Add("product_code", "PROD");
                        mapping.Add("onhand_qty", "ONHAND");
                        mapping.Add("committed_qty", "INV_COMMITTED");
                        mapping.Add("backorder_qty", "BACK_ORDER");

                        mapping.Add("current_cost", "CurrentCost");
                        mapping.Add("average_cost", "AvgCost");
                        mapping.Add("misc_1", "InvGroup");
                    });
                }

                // STEP 2: HARDWARE LOADING (VBA: chkLoadIMEI)



                if (options.LoadIMEI)
                {
                    await ExecuteSqlAction("TRUNCATE TABLE WWInventory");

                    string query = @"
        SELECT 
            whse, 
            part_no, 
            description,
            product_code,
            onhand_qty,
            current_cost,
            average_cost,
            misc_1
        FROM public.inventory
        WHERE whse NOT IN ('ZZ', 'FR')
        AND (product_code IN ('HCL','HCC','OBH'))";

                    await TransferDataFromPgToSql(query, "WWInventory", mapping => {
                        mapping.Add("whse", "WHSE");
                        mapping.Add("part_no", "CODE");
                        mapping.Add("description", "INV_DESCRIPTION");
                        mapping.Add("product_code", "PROD");
                        mapping.Add("onhand_qty", "ONHAND");
                        mapping.Add("current_cost", "WHOLESALE");
                        mapping.Add("average_cost", "WEIGHTED");
                        mapping.Add("misc_1", "misc_1");
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Snapshot Load Failed: " + ex.Message);
            }
        }

        private async Task ExecuteSqlAction(string sqlText)
        {
            using (SqlConnection conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(sqlText, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private DataTable GetSqlServerData(string sql)
        {
            DataTable dt = new DataTable();
            using var con = new SqlConnection(_sqlConn);
            using var da = new SqlDataAdapter(sql, con);
            da.Fill(dt);
            return dt;
        }
        public async Task<byte[]> ExportHardwareCounts()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string sql = @"
        SELECT 
            LTRIM(RTRIM(c.PartNumber)) AS SKU, 
            MAX(ISNULL(a.DESCRIPTION, 'No Description Found')) AS Description
        FROM tblCounts c
        LEFT JOIN WWAccessories a ON LTRIM(RTRIM(c.PartNumber)) = LTRIM(RTRIM(a.CODE))
        GROUP BY c.PartNumber
        ORDER BY c.PartNumber";

            DataTable dt = GetSqlServerData(sql);

            if (dt == null || dt.Rows.Count == 0)
                return Array.Empty<byte>();

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Hardware Counts");

                ws.Cells["A1"].Value = "SKU";
                ws.Cells["B1"].Value = "Description";
                //ws.Cells["C1"].Value = "Physical Count";
                ws.Cells["A1:C1"].Style.Font.Bold = true;

                int row = 2;
                foreach (DataRow dr in dt.Rows)
                {
                    ws.Cells[row, 1].Value = dr["SKU"];
                    // Agar description abhi bhi khali hai toh "N/A" likh dein
                    ws.Cells[row, 2].Value = dr["Description"]?.ToString() ?? "No Description Found";
                    //ws.Cells[row, 3].Value = dr["PhysicalCount"]; // Ab counts dikheng

                    row++;
                }

                ws.Cells.AutoFitColumns();
                return await package.GetAsByteArrayAsync();
            }
        }


        public async Task<byte[]> ExportAccessoryCounts()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string sql = @"
        SELECT 
            LTRIM(RTRIM(a.WHSE)) AS WHSE, 
            LTRIM(RTRIM(a.CODE)) AS CODE, 
            a.Description, 
            SUM(ISNULL(c.QtyTotal, 0)) AS ScannedQty
        FROM WWAccessories a
        LEFT JOIN tblACCCounts c ON LTRIM(RTRIM(a.CODE)) = LTRIM(RTRIM(c.PartNo)) 
                                AND LTRIM(RTRIM(a.WHSE)) = LTRIM(RTRIM(c.Whse))
        WHERE a.PROD <> 'OBA'
        GROUP BY a.WHSE, a.CODE, a.Description
        ORDER BY a.WHSE, a.CODE";

            DataTable dt = GetSqlServerData(sql);

            if (dt == null || dt.Rows.Count == 0)
                return Array.Empty<byte>();

            using (var package = new ExcelPackage())
            {
                var warehouses = dt.AsEnumerable()
                                   .Select(r => r.Field<string>("WHSE"))
                                   .Distinct()
                                   .OrderBy(w => w)
                                   .ToList();

                foreach (var whse in warehouses)
                {
                    string sheetName = string.IsNullOrWhiteSpace(whse) ? "Main" : whse;
                    var ws = package.Workbook.Worksheets.Add(sheetName);

                    // VBA style Headers
                    ws.Cells["A1"].Value = "PartNo";
                    ws.Cells["B1"].Value = "Description";
                    ws.Cells["C1"].Value = "Qty"; // VBA calls it Qty
                    ws.Cells["A1:C1"].Style.Font.Bold = true;

                    var whseRows = dt.AsEnumerable().Where(r => r.Field<string>("WHSE") == whse);

                    int row = 2;
                    foreach (var dr in whseRows)
                    {
                        ws.Cells[row, 1].Value = dr["CODE"]?.ToString();
                        ws.Cells[row, 2].Value = dr["Description"]?.ToString();

                        decimal qty = Convert.ToDecimal(dr["ScannedQty"]);

                        ws.Cells[row, 3].Value = (qty == 0) ? "" : (object)qty;

                        row++;
                    }
                    ws.Cells.AutoFitColumns();
                }
                return await package.GetAsByteArrayAsync();
            }
        }
        private async Task TransferDataFromPgToSql(string pgQuery, string destinationTable, Action<SqlBulkCopyColumnMappingCollection> mapAction)
        {
            using (NpgsqlConnection pgConn = new NpgsqlConnection(_spireConn))
            {
                await pgConn.OpenAsync();
                using (NpgsqlCommand pgCmd = new NpgsqlCommand(pgQuery, pgConn))
                using (var reader = await pgCmd.ExecuteReaderAsync())
                {
                    using (SqlConnection sqlConn = new SqlConnection(_sqlConn))
                    {
                        await sqlConn.OpenAsync();
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConn))
                        {
                            bulkCopy.DestinationTableName = destinationTable;
                            bulkCopy.BulkCopyTimeout = 300; // 5 minutes timeout

                            // Agar mapping null hai toh names exact hone chahiye
                            mapAction?.Invoke(bulkCopy.ColumnMappings);

                            await bulkCopy.WriteToServerAsync(reader);
                        }
                    }
                }
            }
        }
        public async Task<string> TestFileAccess()
        {
            string path = @"\\dcibvaz02\BVDATA\Invent\serial.btr";

            try
            {
                if (System.IO.File.Exists(path))
                {
                    var info = new FileInfo(path);
                    return $"Success! File Found. Size: {info.Length} bytes, Last Modified: {info.LastWriteTime}";
                }
                else
                {
                    return $"Error: Path not found. Server cannot reach {path}. Check network permissions.";
                }
            }
            catch (Exception ex)
            {
                return $"Permission Denied: {ex.Message}";
            }
        }
        public async Task<object> GetFileStatus()
        {
            var serialPath = @"\\dcibvaz02\BVDATA\Invent\serial.btr";
            var inventPath = @"\\dcibvaz02\BVDATA\Invent\invent.btr";
            var lastNightPath = @"\\dcibvaz02\BVDATA\Invent\LastNightInvIMEI\"; // Folder path

            var serialInfo = new FileInfo(serialPath);
            var inventInfo = new FileInfo(inventPath);

            var lnSerialInfo = new FileInfo(Path.Combine(lastNightPath, "serial.btr"));
            var lnInventInfo = new FileInfo(Path.Combine(lastNightPath, "invent.btr"));

            return new
            {
                success = true,
                result = new
                {
                    // Current Dates
                    serialCurrent = serialInfo.Exists ? serialInfo.LastWriteTime.ToString("MM/dd/yyyy HH:mm") : "Not Found",
                    inventoryCurrent = inventInfo.Exists ? inventInfo.LastWriteTime.ToString("MM/dd/yyyy HH:mm") : "Not Found",

                    serialLastNight = lnSerialInfo.Exists ? lnSerialInfo.LastWriteTime.ToString("MM/dd/yyyy HH:mm") : "Not Found",
                    inventoryLastNight = lnInventInfo.Exists ? lnInventInfo.LastWriteTime.ToString("MM/dd/yyyy HH:mm") : "Not Found"
                }
            };
        }
        public async Task<bool> SyncInventoryFiles()
        {
            string sourceDir = @"\\dcibvaz02\BVDATA\Invent\LastNightInvIMEI\";
            string destDir = @"\\dcibvaz02\BVDATA\Invent\";

            try
            {
                System.IO.File.Copy(Path.Combine(sourceDir, "serial.btr"), Path.Combine(destDir, "serial.btr"), true);
                System.IO.File.Copy(Path.Combine(sourceDir, "invent.btr"), Path.Combine(destDir, "invent.btr"), true);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Sync failed: " + ex.Message);
            }
        }


        public async Task<List<string>> GetUniqueFileNames(bool isACC)
        {
            string tableName = isACC ? "tblACCCounts" : "tblCounts";
            string sql = $"SELECT DISTINCT CountFile FROM {tableName} WHERE CountFile IS NOT NULL AND CountFile <> '' ORDER BY CountFile";

            var fileNames = new List<string>();

            using (SqlConnection conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        fileNames.Add(reader["CountFile"].ToString());
                    }
                }
            }
            return fileNames;
        }

        public async Task<bool> DeleteAllCounts(bool isACC)
        {
            string tableName = isACC ? "tblACCCounts" : "tblCounts";
            await _context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {tableName}");
            return true;
        }
    }
}