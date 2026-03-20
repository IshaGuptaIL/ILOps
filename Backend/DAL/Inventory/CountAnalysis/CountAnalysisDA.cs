using DAL.Common.Login;
using DAL.Inventory.IMEI;
using DAL.Inventory.IMEI.Report;
using DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DAL.Inventory.CountAnalysis
{
    public class CountAnalysisDA : ICountAnalysis
    {
        public readonly AppDBContext _dbContext;
        private readonly string _sqlConn;
        private readonly string _pgConn;

        public CountAnalysisDA(IConfiguration configuration, AppDBContext context)
        {
            _sqlConn = configuration.GetConnectionString("bvactivation_Connection");
            _pgConn = configuration.GetConnectionString("spire_Connection");
            _dbContext = context;
        }

        // 1ROW
        public async Task<ApiResposne> LoadIMEICounts(Stream excelStream, string fileName)
        {
            var response = new ApiResposne();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using var pkg = new ExcelPackage(excelStream);
                var ws = pkg.Workbook.Worksheets[0];

                using var sqlConn = new SqlConnection(_sqlConn);
                await sqlConn.OpenAsync();

                // ================== STEP 1: READ EXCEL + INSERT ==================
                int col = 1;

                while (ws.Cells[1, col].Value != null)
                {
                    string partNumber = ws.Cells[1, col].Text.ToUpper().Trim();
                    int row = 3;

                    while (ws.Cells[row, col].Value != null || row < 6)
                    {
                        string rawImei = ws.Cells[row, col].Text.Trim();

                        if (!string.IsNullOrEmpty(rawImei))
                        {
                            
                            string cleanImei = Regex.Replace(rawImei, @"[^0-9]", "");

                            if (cleanImei.Length == 14 && cleanImei.StartsWith("1"))
                                cleanImei = "0" + cleanImei;

                            string insertSql = @"INSERT INTO tblCounts 
                                        (whse, PartNumber, IMEI, CountFile, RowNumber, ColumnNumber)
                                         VALUES ('CO', @Part, @Imei, @File, @Row, @Col)";

                            using var cmd = new SqlCommand(insertSql, sqlConn);
                            cmd.Parameters.AddWithValue("@Part", partNumber);
                            cmd.Parameters.AddWithValue("@Imei", cleanImei);
                            cmd.Parameters.AddWithValue("@File", fileName);
                            cmd.Parameters.AddWithValue("@Row", row);
                            cmd.Parameters.AddWithValue("@Col", col);

                            await cmd.ExecuteNonQueryAsync();
                        }
                        row++;
                    }
                    col++;
                }

                // ================== STEP 2: MARK UNKNOWN ==================
                string unknownSql = @"
            UPDATE t
            SET t.PartNumber = 'UNKNOWN'
            FROM tblCounts t
            LEFT JOIN WWInventory w 
                ON t.PartNumber = w.CODE 
                AND t.whse = w.WHSE
            WHERE w.CODE IS NULL";

                using (var cmd = new SqlCommand(unknownSql, sqlConn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // ================== STEP 3: FIX UNKNOWN FROM SPIRE (POSTGRES) ==================
                string selectUnknown = "SELECT Id, IMEI FROM tblCounts WHERE PartNumber = 'UNKNOWN'";

                using var selectCmd = new SqlCommand(selectUnknown, sqlConn);
                using var reader = await selectCmd.ExecuteReaderAsync();

                var unknownList = new List<(int Id, string IMEI)>();

                while (await reader.ReadAsync())
                {
                    unknownList.Add((reader.GetInt32(0), reader.GetString(1)));
                }

                await reader.CloseAsync();

                // POSTGRES CONNECTION
                using var pgConn = new NpgsqlConnection(_pgConn);
                await pgConn.OpenAsync();

                foreach (var item in unknownList)
                {
                    string pgSql = @"
                SELECT part_no, whse
                FROM inventory_serial_transactions
                WHERE number = @imei
                ORDER BY receipt_date DESC
                LIMIT 1";

                    using var pgCmd = new NpgsqlCommand(pgSql, pgConn);
                    pgCmd.Parameters.AddWithValue("@imei", item.IMEI);

                    using var pgReader = await pgCmd.ExecuteReaderAsync();

                    if (await pgReader.ReadAsync())
                    {
                        string partNo = pgReader.GetString(0);

                        await pgReader.CloseAsync();

                        string updateSql = "UPDATE tblCounts SET PartNumber = @Part WHERE Id = @Id";

                        using var updateCmd = new SqlCommand(updateSql, sqlConn);
                        updateCmd.Parameters.AddWithValue("@Part", partNo);
                        updateCmd.Parameters.AddWithValue("@Id", item.Id);

                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        await pgReader.CloseAsync();
                    }
                }

                response.Success = true;
                response.Message = "IMEI Counts uploaded and processed successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
        private async Task MarkUnknownParts(SqlConnection conn)
        {
            string updateSql = @"
        UPDATE tc
        SET tc.PartNumber = 'UNKNOWN'
        FROM tblCounts tc
        LEFT JOIN WWInventory i 
            ON tc.PartNumber = i.CODE 
            AND tc.whse = i.WHSE
        WHERE i.CODE IS NULL";

            using (var cmd = new SqlCommand(updateSql, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task FixUnknownPartsFromSpire(SqlConnection conn)
        {
            string selectSql = "SELECT ID, IMEI FROM tblCounts WHERE PartNumber = 'UNKNOWN'";

            using (var selectCmd = new SqlCommand(selectSql, conn))
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                var unknownList = new List<(int Id, string Imei)>();

                while (await reader.ReadAsync())
                {
                    unknownList.Add((reader.GetInt32(0), reader.GetString(1)));
                }

                reader.Close();

                foreach (var record in unknownList)
                {
                    string foundPart = await GetWhsePartMostRecentSpire(conn, record.Imei);

                    if (!string.IsNullOrEmpty(foundPart))
                    {
                        string updateSql = "UPDATE tblCounts SET PartNumber = @Part WHERE ID = @Id";

                        using (var updateCmd = new SqlCommand(updateSql, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@Part", foundPart);
                            updateCmd.Parameters.AddWithValue("@Id", record.Id);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        private async Task<string> GetWhsePartMostRecentSpire(SqlConnection conn, string imei)
        {
            string sql = @"
        SELECT TOP 1 PartNumber
        FROM SpireReceipts
        WHERE IMEI = @Imei
        ORDER BY ReceiptDate DESC";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Imei", imei);

                var result = await cmd.ExecuteScalarAsync();

                return result?.ToString();
            }
        }



        public async Task<ApiResposne> GetAllImportedCounts() 
        {
            var response = new ApiResposne();
            var results = new List<object>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    
                    string sql = @"
                SELECT ID, Whse, PartNumber, IMEI, CountFile, RowNumber, ColumnNumber 
                FROM tblCounts 
                ORDER BY ID";

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    Id = reader["ID"],
                                    Whse = reader["Whse"]?.ToString(),
                                    PartNumber = reader["PartNumber"]?.ToString(),
                                    Imei = reader["IMEI"]?.ToString(),
                                    CountFile = reader["CountFile"]?.ToString(),
                                    RowNumber = reader["RowNumber"] != DBNull.Value ? Convert.ToInt32(reader["RowNumber"]) : 0,
                                    ColumnNumber = reader["ColumnNumber"] != DBNull.Value ? Convert.ToInt32(reader["ColumnNumber"]) : 0
                                });
                            }
                        }
                    }
                }
                response.Success = true;
                response.Result = results;
                response.Count = results.Count; 
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }



        public async Task<ApiResposne> GetOnhandNotCounted()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    string sql = @"
               SELECT 
    ws.WAREHOUSE,
    ws.PART_NO,
    ws.NUMBER,
    wi.PROD,
    wi.ONHAND,
    wi.WHOLESALE,
    wi.LastSaleDate
FROM WWSerialnumber ws
INNER JOIN WWInventory wi 
    ON LTRIM(RTRIM(ws.PART_NO)) = LTRIM(RTRIM(wi.CODE))
    AND LTRIM(RTRIM(ws.WAREHOUSE)) = LTRIM(RTRIM(wi.WHSE))
LEFT JOIN tblCounts tc
    ON LTRIM(RTRIM(ws.NUMBER)) = LTRIM(RTRIM(tc.IMEI))
WHERE (wi.PROD = 'HCC' OR wi.PROD = 'ACC')
AND tc.IMEI IS NULL
ORDER BY ws.NUMBER;";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new
                            {
                                Imei = reader["IMEI"]?.ToString(),
                                Whse = reader["Whse"]?.ToString(),
                                PartNumber = reader["PartNumber"]?.ToString(),
                                Prod = reader["PROD"]?.ToString(),
                                Onhand = reader["ONHAND"]
                            });
                        }
                    }
                }
                response.Success = true;
                response.Result = results;
                response.Count = results.Count;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "API Error: " + ex.Message;
            }

            return response;
        }




        public async Task<ApiResposne> GetWarehouseAssignments(int pageNumber, int pageSize)
        {
            var response = new ApiResposne();
            var list = new List<dynamic>();
            int offset = (pageNumber - 1) * pageSize;

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();
                string query = @"
            SELECT sn.WAREHOUSE, sn.PART_NO, sn.NUMBER, inv.PROD, inv.ONHAND, inv.WHOLESALE, inv.LastSaleDate
            FROM WWSerialnumber sn
            INNER JOIN WWInventory inv ON (sn.PART_NO = inv.CODE) AND (sn.WAREHOUSE = inv.WHSE)
            WHERE inv.PROD IN ('HCC', 'ACC')
            ORDER BY sn.PART_NO -- Necessary for OFFSET
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                Warehouse = reader["WAREHOUSE"],
                                PartNo = reader["PART_NO"],
                                SerialNumber = reader["NUMBER"],
                                Prod = reader["PROD"],
                                Onhand = reader["ONHAND"],
                                Wholesale = reader["WHOLESALE"],
                                LastSaleDate = reader["LastSaleDate"]
                            });
                        }
                    }
                }
            }
            response.Success = true;
            response.Result = list;
            return response;
        }
        public async Task<ApiResposne> GetDuplicateIMEICounts(int pageNumber, int pageSize)
        {
            var response = new ApiResposne();
            var results = new List<object>();
            int offset = (pageNumber - 1) * pageSize;

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    string countSql = @"
                SELECT COUNT(*)
                FROM (
                    SELECT IMEI
                    FROM tblCounts
                    GROUP BY IMEI
                    HAVING COUNT(ID) > 1
                ) dup";
                    int totalRecords;
                    using (SqlCommand countCmd = new SqlCommand(countSql, conn))
                    {
                        totalRecords = (int)await countCmd.ExecuteScalarAsync();
                    }

                    string sql = @"
                WITH DuplicateIMEIs AS (
                    SELECT *, ROW_NUMBER() OVER(PARTITION BY IMEI ORDER BY ID) AS rn
                    FROM tblCounts
                    WHERE IMEI IN (
                        SELECT IMEI
                        FROM tblCounts
                        GROUP BY IMEI
                        HAVING COUNT(ID) > 1
                    )
                )
                SELECT
                    d.IMEI,
                    d.Whse,
                    d.PartNumber,
                    d.ID AS CountFile,
                    d.RowNumber,
                    d.ColumnNumber,
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM WWSerialnumber bv
                        WHERE d.IMEI = bv.NUMBER
                          AND d.PartNumber = bv.PART_NO
                          AND d.Whse = bv.WAREHOUSE
                    ) THEN 'InBV' ELSE '' END AS ExistBV
                FROM DuplicateIMEIs d
                WHERE d.rn = 1
                ORDER BY d.IMEI
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    Imei = reader["IMEI"]?.ToString(),
                                    Whse = reader["Whse"]?.ToString(),
                                    PartNumber = reader["PartNumber"]?.ToString(),
                                    CountFile = reader["CountFile"],
                                    RowNumber = reader["RowNumber"],
                                    ColumnNumber = reader["ColumnNumber"],
                                    ExistBV = reader["ExistBV"]?.ToString()
                                });
                            }
                        }
                    }

                    response.Success = true;
                    response.Result = results;
                    response.Message = "Fetched duplicate IMEIs with BV existence flag.";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> GetSystemDuplicateSerials(int pageNumber, int pageSize)
        {
            var response = new ApiResposne();
            var results = new List<object>();
            int offset = (pageNumber - 1) * pageSize;

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    string countSql = @"
                SELECT COUNT(*)
                FROM (
                    SELECT NUMBER
                    FROM WWSerialnumber
                    GROUP BY NUMBER
                    HAVING COUNT(WAREHOUSE) > 1
                ) dup";

                    int totalRecords;
                    using (SqlCommand countCmd = new SqlCommand(countSql, conn))
                    {
                        totalRecords = (int)await countCmd.ExecuteScalarAsync();
                    }

                    string sql = @"
                SELECT 
                    NUMBER AS Imei,
                    COUNT(WAREHOUSE) AS DuplicateCount
                FROM WWSerialnumber
                GROUP BY NUMBER
                HAVING COUNT(WAREHOUSE) > 1
                ORDER BY NUMBER
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    Imei = reader["Imei"]?.ToString(),
                                    DuplicateCount = reader["DuplicateCount"]
                                });
                            }
                        }
                    }

                    response.Success = true;
                    response.Result = results;
                    response.Message = "Fetched system duplicate IMEIs (multiple warehouses).";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> ProcessDuplicateCounts()
        {
            var response = new ApiResposne();
            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string deleteSql = "DELETE FROM tblIMEICountDuplicates";
                            using (SqlCommand delCmd = new SqlCommand(deleteSql, conn, transaction))
                            {
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            string insertSql = @"
                       INSERT INTO tblIMEICountDuplicates 
(IMEI, Warehouse, Part, CountFile, RowNumber, ColumnNumber)
SELECT 
    tc.IMEI,
    tc.Whse,          -- source column
    tc.PartNumber,    -- source column
    tc.CountFile,
    tc.RowNumber,
    tc.ColumnNumber
FROM tblCounts tc
WHERE tc.IMEI IN (
    SELECT IMEI 
    FROM tblCounts 
    GROUP BY IMEI 
    HAVING COUNT(IMEI) > 1
)";

                            using (SqlCommand insCmd = new SqlCommand(insertSql, conn, transaction))
                            {
                                await insCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            string selectSql = "SELECT * FROM tblIMEICountDuplicates";
                            var results = new List<object>();
                            using (SqlCommand selCmd = new SqlCommand(selectSql, conn))
                            {
                                using (var reader = await selCmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        results.Add(new
                                        {
                                            Imei = reader["IMEI"],
                                            Warehouse = reader["Warehouse"],
                                            Part = reader["Part"],
                                            CountFile = reader["CountFile"],
                                            RowNumber = reader["RowNumber"],
                                            ColumnNumber = reader["ColumnNumber"]
                                        });
                                    }
                                }
                            }

                            response.Success = true;
                            response.Result = results; 
                            response.Message = "Find Duplicates Completed Successfully.";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<ApiResposne> GetDuplicateCleanupPreview()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                   
                    string sql = @"
                SELECT 
                    ID,
                    Warehouse, 
                    Part, 
                    IMEI, 
                    CountFile, 
                    RowNumber, 
                    ColumnNumber,
                    CASE 
                        WHEN ID <> MIN(ID) OVER(PARTITION BY IMEI) THEN 'Yes' 
                        ELSE '' 
                    END AS WillDelete
                FROM tblIMEICountDuplicates
                ORDER BY Warehouse, Part, IMEI";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new
                            {
                                // Safely handle potential nulls and types
                                Id = reader["ID"],
                                Warehouse = reader["Warehouse"]?.ToString(),
                                Part = reader["Part"]?.ToString(),
                                Imei = reader["IMEI"]?.ToString(),
                                CountFile = reader["CountFile"]?.ToString(),
                                RowNumber = reader["RowNumber"] != DBNull.Value ? Convert.ToInt32(reader["RowNumber"]) : 0,
                                ColumnNumber = reader["ColumnNumber"] != DBNull.Value ? Convert.ToInt32(reader["ColumnNumber"]) : 0,
                                WillDelete = reader["WillDelete"]?.ToString()
                            });
                        }
                    }
                }

                response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> DeleteDuplicateCounts()
        {
            var response = new ApiResposne();
            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateSql = @"
                        UPDATE tc
                        SET tc.Duplicate = 1
                        FROM tblCounts tc
                        INNER JOIN tblIMEICountDuplicates td ON tc.ID = td.CountID
                        WHERE td.CountID <> td.MinOfID";

                            using (SqlCommand updCmd = new SqlCommand(updateSql, conn, transaction))
                            {
                                await updCmd.ExecuteNonQueryAsync();
                            }

                            string deleteSql = "DELETE FROM tblCounts WHERE Duplicate = 1";

                            using (SqlCommand delCmd = new SqlCommand(deleteSql, conn, transaction))
                            {
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            response.Success = true;
                            response.Message = "Duplicate Counts Deleted Successfully.";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting duplicates: " + ex.Message;
            }
            return response;
        }

        private int VerifySerial(string imei, string partNumber)
        {
            if (string.IsNullOrEmpty(imei)) return 1; // Error code 1: Empty

            if (imei.Length < 8) return 2; // Error code 2: Too short

            return 0; // 0 means Valid
        }


        public async Task<ApiResposne> GetInvalidSerialCounts()
        {
            var response = new ApiResposne();
            var allResults = new List<object>(); // DTO use kar sakte hain yahan

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    // SQL query wahi rakhi hai jo aapne di hai (C# friendly format mein)
                    // Note: Agar VerifySerial SQL ka function hai, toh ye query DB level pe filter karegi
                    string sql = @"
                SELECT 
                    PartNumber, 
                    LEN(IMEI) AS ImeiLength, 
                    IMEI, 
                    CountFile AS SpreadSheet, 
                    RowNumber AS [Row], 
                    ColumnNumber AS [Column],
                    ColumnNumber AS ColumnNumberRaw
                FROM tblCounts";

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 60;
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string currentImei = reader["IMEI"]?.ToString();
                                string currentPart = reader["PartNumber"]?.ToString();

                                // Aapka Business Logic: VerifySerial call karna
                                int errorCode = VerifySerial(currentImei, currentPart);

                                // WHERE logic: Sirf wahi add karein jo 0 nahi hain
                                if (errorCode != 0)
                                {
                                    allResults.Add(new
                                    {
                                        PartNumber = currentPart,
                                        ImeiLength = reader["ImeiLength"] != DBNull.Value ? Convert.ToInt32(reader["ImeiLength"]) : 0,
                                        Imei = currentImei,
                                        SpreadSheet = reader["SpreadSheet"]?.ToString(),
                                        Row = reader["Row"] != DBNull.Value ? Convert.ToInt32(reader["Row"]) : 0,
                                        Column = reader["Column"] != DBNull.Value ? Convert.ToInt32(reader["Column"]) : 0,
                                        ColumnNumber = reader["ColumnNumberRaw"] != DBNull.Value ? Convert.ToInt32(reader["ColumnNumberRaw"]) : 0,
                                        Expr1 = errorCode // Ye aapka VerifySerial ka result hai
                                    });
                                }
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = allResults;
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "General Error: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> GetSystemSerialVerification()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
                SELECT 
                    S.WAREHOUSE, 
                    S.PART_NO, 
                    S.NUMBER, 
                    I.PROD, 
                    I.ONHAND, 
                    I.WHOLESALE, 
                    I.LastSaleDate
                FROM WWSerialnumber S
                INNER JOIN WWInventory I ON S.PART_NO = I.CODE AND S.WAREHOUSE = I.WHSE
                WHERE I.PROD IN ('HCC', 'ACC')";

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string serial = reader["NUMBER"]?.ToString()?.Trim();
                                string part = reader["PART_NO"]?.ToString();

                                int verificationCode = VerifySerial(serial, part);

                                results.Add(new
                                {
                                    Warehouse = reader["WAREHOUSE"]?.ToString(),
                                    PartNo = part,
                                    Serial = serial,
                                    Prod = reader["PROD"]?.ToString(),
                                    Onhand = reader["ONHAND"],
                                    Wholesale = reader["WHOLESALE"],
                                    LastSaleDate = reader["LastSaleDate"],
                                    Length = serial?.Length ?? 0,
                                    VerificationCode = verificationCode,
                                    Status = verificationCode == 0 ? "Valid" : "Invalid"
                                });
                            }
                        }
                    }
                }
                response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "System Verification Error: " + ex.Message;
            }
            return response;
        }
        public async Task<ApiResposne> GetDiscrepancyReport()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
                -- 1. Base Onhand Logic (Replacing BVSerialsOnhand)
                WITH BVActualOnhand AS (
                    SELECT S.WAREHOUSE, S.PART_NO, S.NUMBER
                    FROM WWSerialnumber S
                    INNER JOIN WWInventory I ON S.PART_NO = I.CODE AND S.WAREHOUSE = I.WHSE
                    WHERE I.PROD IN ('HCC', 'ACC')
                ),
                -- 2. Unique IMEI Logic (Replacing qryBVOnhandUniqueIMEI)
                qryBVOnhandUniqueIMEI AS (
                    SELECT 
                        NUMBER AS BVOnhandIMEI, 
                        MAX(WAREHOUSE) AS MaxOfWAREHOUSE, 
                        MAX(PART_NO) AS MaxOfPART_NO
                    FROM BVActualOnhand
                    GROUP BY NUMBER
                ),
                -- 3. Not in Onhand Logic (Replacing qryCounted-NotOnhandBV)
                notInBv AS (
                    SELECT tc.IMEI
                    FROM tblCounts tc 
                    LEFT JOIN BVActualOnhand ON tc.IMEI = BVActualOnhand.NUMBER
                    WHERE BVActualOnhand.NUMBER IS NULL
                )
                -- 4. Final Discrepancy Report
                SELECT 
                    tc.Whse, 
                    tc.PartNumber, 
                    tc.IMEI, 
                    tc.CountFile, 
                    tc.RowNumber, 
                    tc.ColumnNumber, 
                    ISNULL(tc.Duplicate, 0) AS IsDuplicate, 
                    bvU.MaxOfWAREHOUSE AS OnhandWhse, 
                    bvU.MaxOfPART_NO AS OnhandPartNo,
                    CASE 
                        WHEN tc.Whse <> bvU.MaxOfWAREHOUSE THEN 'Whse Does Not Match' 
                        ELSE '' 
                    END AS WhseDisc,
                    CASE 
                        WHEN tc.PartNumber <> bvU.MaxOfPART_NO THEN 'PartNo Does Not Match' 
                        ELSE '' 
                    END AS PartNoDisc
                FROM tblCounts tc
                LEFT JOIN BVActualOnhand bv ON tc.Whse = bv.WAREHOUSE 
                    AND tc.IMEI = bv.NUMBER 
                    AND tc.PartNumber = bv.PART_NO
                LEFT JOIN qryBVOnhandUniqueIMEI bvU ON tc.IMEI = bvU.BVOnhandIMEI
                LEFT JOIN notInBv ON tc.IMEI = notInBv.IMEI
                WHERE notInBv.IMEI IS NULL 
                    AND bv.PART_NO IS NULL";

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    Whse = reader["Whse"]?.ToString(),
                                    PartNumber = reader["PartNumber"]?.ToString(),
                                    Imei = reader["IMEI"]?.ToString(),
                                    CountFile = reader["CountFile"]?.ToString(),
                                    Row = reader["RowNumber"],
                                    Col = reader["ColumnNumber"],
                                    OnhandWhse = reader["OnhandWhse"]?.ToString(),
                                    OnhandPartNo = reader["OnhandPartNo"]?.ToString(),
                                    WhseDisc = reader["WhseDisc"]?.ToString(),
                                    PartNoDisc = reader["PartNoDisc"]?.ToString(),
                                    Duplicate = reader["IsDuplicate"] != DBNull.Value && Convert.ToBoolean(reader["IsDuplicate"])
                                });
                            }
                        }
                    }
                }
                response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Discrepancy Report Error: " + ex.Message;
            }
            return response;
        }

        public async Task<ApiResposne> GetQuantityVsSerialComparison()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    
                    string sql = @"
                SELECT 
                    cq.Whse, 
                    cq.PartNumber, 
                    cq.CountQty, 
                    sn.CountOfNUMBER AS ScannedSerialsQty
                FROM qryBVSNCount sn
                RIGHT JOIN qryCountQuantities cq 
                    ON sn.PART_NO = cq.PartNumber 
                    AND sn.WAREHOUSE = cq.Whse
                ORDER BY cq.Whse, cq.PartNumber";

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int countQty = reader["CountQty"] != DBNull.Value ? Convert.ToInt32(reader["CountQty"]) : 0;
                                int scannedQty = reader["ScannedSerialsQty"] != DBNull.Value ? Convert.ToInt32(reader["ScannedSerialsQty"]) : 0;

                                results.Add(new
                                {
                                    Whse = reader["Whse"]?.ToString(),
                                    PartNumber = reader["PartNumber"]?.ToString(),
                                    CountQty = countQty,
                                    ScannedQty = scannedQty,
                                    Difference = countQty - scannedQty,
                                    Status = countQty == scannedQty ? "Match" : "Mismatch"
                                });
                            }
                        }
                    }
                }
                response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Comparison Report Error: " + ex.Message;
            }
            return response;
        }



        public async Task<ApiResposne> GetMissingFromPhysicalCount()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
                -- Derived BVSerialsOnhand dataset
                WITH BVSerialsOnhand AS (
                    SELECT 
                        ws.WAREHOUSE, 
                        ws.PART_NO, 
                        ws.NUMBER
                    FROM WWSerialnumber ws
                    INNER JOIN WWInventory wi 
                        ON ws.PART_NO = wi.CODE
                       AND ws.WAREHOUSE = wi.WHSE
                    WHERE wi.PROD = 'HCC' OR wi.PROD = 'ACC'
                )
                -- Find missing from physical counts
                SELECT 
                    bv.WAREHOUSE, 
                    bv.PART_NO, 
                    bv.NUMBER, 
                    'Should be onhand in BV' AS StatusNote
                FROM BVSerialsOnhand bv
                LEFT JOIN tblCounts tc 
                    ON bv.NUMBER = tc.IMEI
                WHERE tc.IMEI IS NULL;
            ";

                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new
                            {
                                Warehouse = reader["WAREHOUSE"]?.ToString(),
                                PartNo = reader["PART_NO"]?.ToString(),
                                Imei = reader["NUMBER"]?.ToString(),
                                Note = reader["StatusNote"]?.ToString()
                            });
                        }
                    }
                }

                 response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> ProcessCountedNotOnhandDetails()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    // 1. Fetch records that need status updates
                    string baseOnhandSql = @"
            WITH BVActualOnhand AS (
                SELECT S.WAREHOUSE, S.PART_NO, S.NUMBER
                FROM WWSerialnumber S
                INNER JOIN WWInventory I ON S.PART_NO = I.CODE AND S.WAREHOUSE = I.WHSE
                WHERE I.PROD IN ('HCC', 'ACC')
            ),
            CountedNotOnhand AS (
                SELECT tc.Whse, tc.PartNumber, tc.IMEI
                FROM tblCounts tc 
                LEFT JOIN BVActualOnhand ON tc.IMEI = BVActualOnhand.NUMBER
                WHERE BVActualOnhand.NUMBER IS NULL
            )
            SELECT DISTINCT c.Whse, c.PartNumber, c.IMEI 
            FROM CountedNotOnhand c
            LEFT JOIN IMEIStatus s ON c.IMEI = s.IMEI AND c.PartNumber = s.PartNo AND c.Whse = s.Whse
            WHERE s.Status = '' OR s.Status IS NULL";

                    var pendingRecords = new List<(string Whse, string Part, string Imei)>();

                    using (SqlCommand cmd = new SqlCommand(baseOnhandSql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            pendingRecords.Add((
                                reader["Whse"].ToString(),
                                reader["PartNumber"].ToString(),
                                reader["IMEI"].ToString()
                            ));
                        }
                    }

                    // 2. Process updates (Spire logic)
                    foreach (var rec in pendingRecords)
                    {
                        await UpdateIMEIStatusInSpire(conn, rec.Whse, rec.Part, rec.Imei);
                    }

                    // 3. Final Fetch (CountFile added and Join Improved)
                    string finalSql = @"
            WITH BVActualOnhand AS (
                SELECT S.NUMBER
                FROM WWSerialnumber S
                INNER JOIN WWInventory I ON S.PART_NO = I.CODE AND S.WAREHOUSE = I.WHSE
                WHERE I.PROD IN ('HCC', 'ACC')
            )
            SELECT 
                tc.Whse, 
                tc.PartNumber, 
                tc.IMEI, 
                tc.CountFile,  -- Missing field added
                ISNULL(s.Status, 'Not Found') AS Status, -- Status null handling
                s.LastInvoice, 
                s.LastInvoiceDate
            FROM tblCounts tc
            LEFT JOIN BVActualOnhand ON tc.IMEI = BVActualOnhand.NUMBER
            LEFT JOIN IMEIStatus s ON tc.IMEI = s.IMEI AND tc.PartNumber = s.PartNo AND tc.Whse = s.Whse
            WHERE BVActualOnhand.NUMBER IS NULL";

                    using (SqlCommand cmd = new SqlCommand(finalSql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new
                            {
                                Whse = reader["Whse"]?.ToString(),
                                PartNumber = reader["PartNumber"]?.ToString(),
                                Imei = reader["IMEI"]?.ToString(),
                                CountFile = reader["CountFile"]?.ToString(), // Now returning CountFile
                                Status = reader["Status"]?.ToString(),
                                LastInvoice = reader["LastInvoice"] == DBNull.Value ? "N/A" : reader["LastInvoice"].ToString(),
                                LastInvoiceDate = reader["LastInvoiceDate"] == DBNull.Value ? null : (DateTime?)reader["LastInvoiceDate"]
                            });
                        }
                    }
                }
                response.Success = true;
                response.Result = results;
                response.Message = "IMEI Status processing complete.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }
        private async Task UpdateIMEIStatusInSpire(SqlConnection conn, string whse, string part, string imei)
        {

        }










        //3 ROW 

        public async Task<ApiResposne> GetItemReceiptsSummary(DateTime startDate, DateTime endDate)
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
            SELECT 
                S.WHSE,
                S.CODE,
                I.INV_DESCRIPTION AS Description,
                I.PROD,
                I.MISC_1 AS [Group],
                S.TotalQty
            FROM
            (
                SELECT 
                    WHSE,
                    CODE,
                    SUM(ISNULL(QTY,0)) AS TotalQty
                FROM WWReceiptsTEMP
                -- WHERE DateColumn >= @Start AND DateColumn <= @End
                GROUP BY WHSE, CODE
            ) AS S
            INNER JOIN WWInventory I
                ON S.CODE = I.CODE 
                AND S.WHSE = I.WHSE
            WHERE I.PROD IN ('ACC','OBA','HCC','OBH')
            ORDER BY S.WHSE, S.CODE";

                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Start", startDate);
                        cmd.Parameters.AddWithValue("@End", endDate);
                        cmd.CommandTimeout = 300;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    whse = reader["WHSE"]?.ToString().Trim(),
                                    code = reader["CODE"]?.ToString().Trim(),
                                    description = reader["Description"]?.ToString().Trim(),
                                    prod = reader["PROD"]?.ToString().Trim(),
                                    group = reader["Group"]?.ToString().Trim(),
                                    totalQty = reader["TotalQty"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["TotalQty"])
                                                : 0
                                });
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Receipts Error: " + ex.Message;
            }

            return response;
        }

        public async Task<ApiResposne> GetAccessoryAnalysisReport(DateTime startDate, DateTime endDate)
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
SELECT
    LTRIM(RTRIM(a.WHSE)) AS WH,
    LTRIM(RTRIM(a.InvGroup)) AS [Group],
    a.PROD AS Expr1,
    a.CODE AS Expr2,
    a.Description AS Expr3,
    a.ONHAND AS Expr4,
    a.CurrentCost AS Expr5,
    a.AvgCost AS Expr6,
    ISNULL(o.ONHAND,0) AS OpenBalance,
    ISNULL(s.SumOfBVCMTDQTY,0) AS SalesQty,
    ISNULL(r.SumOfQTY,0) AS ReceiptsQty,
    (ISNULL(o.ONHAND,0) - ISNULL(s.SumOfBVCMTDQTY,0) + ISNULL(r.SumOfQTY,0)) AS ClosingBalCalculated,
    ISNULL(c.SumOfQtyTotal,0) AS PhysicalCount,
    (ISNULL(c.SumOfQtyTotal,0) - (ISNULL(o.ONHAND,0) - ISNULL(s.SumOfBVCMTDQTY,0) + ISNULL(r.SumOfQTY,0))) AS Difference,
    (a.CurrentCost - a.AvgCost) AS DiffAvgVsCurrent,
    ((ISNULL(o.ONHAND,0) - ISNULL(s.SumOfBVCMTDQTY,0) + ISNULL(r.SumOfQTY,0)) * a.CurrentCost) AS StockValueClosing,
    (ISNULL(c.SumOfQtyTotal,0) * a.CurrentCost) AS StockValuePhysical
FROM WWAccessories a
LEFT JOIN tblOpeningBalanceACC o ON a.CODE = o.PartNo AND a.WHSE = o.WHSE

-- Sales summary (was qrySalesDetailSummary)
LEFT JOIN (
    SELECT WHSE, CODE, SUM(BVCMTDQTY) AS SumOfBVCMTDQTY
    FROM WWSalesDetailTEMP
    GROUP BY WHSE, CODE
) s ON a.CODE = s.CODE AND a.WHSE = s.WHSE

-- Receipts summary (was qryReceiptDetailSummary)
LEFT JOIN (
    SELECT WHSE, CODE, SUM(QTY) AS SumOfQTY
    FROM WWReceiptsTEMP
    GROUP BY WHSE, CODE
) r ON a.CODE = r.CODE AND a.WHSE = r.WHSE

-- Counts summary (was qryCountsACC)
LEFT JOIN (
    SELECT Whse, PartNo, SUM(QtyTotal) AS SumOfQtyTotal
    FROM tblACCCounts
    GROUP BY Whse, PartNo
) c ON a.CODE = c.PartNo AND a.WHSE = c.Whse

ORDER BY WH, [Group], Expr1, Expr2;
";

                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    wh = reader["WH"]?.ToString(),
                                    group = reader["Group"]?.ToString(),
                                    prod = reader["Expr1"]?.ToString(),
                                    code = reader["Expr2"]?.ToString(),
                                    description = reader["Expr3"]?.ToString(),
                                    onHand = reader["Expr4"] != DBNull.Value ? Convert.ToDecimal(reader["Expr4"]) : 0,
                                    currentCost = reader["Expr5"] != DBNull.Value ? Convert.ToDecimal(reader["Expr5"]) : 0,
                                    avgCost = reader["Expr6"] != DBNull.Value ? Convert.ToDecimal(reader["Expr6"]) : 0,
                                    openBalance = reader["OpenBalance"] != DBNull.Value ? Convert.ToDecimal(reader["OpenBalance"]) : 0,
                                    salesQty = reader["SalesQty"] != DBNull.Value ? Convert.ToDecimal(reader["SalesQty"]) : 0,
                                    receiptsQty = reader["ReceiptsQty"] != DBNull.Value ? Convert.ToDecimal(reader["ReceiptsQty"]) : 0,
                                    closingBal = reader["ClosingBalCalculated"] != DBNull.Value ? Convert.ToDecimal(reader["ClosingBalCalculated"]) : 0,
                                    physCount = reader["PhysicalCount"] != DBNull.Value ? Convert.ToDecimal(reader["PhysicalCount"]) : 0,
                                    difference = reader["Difference"] != DBNull.Value ? Convert.ToDecimal(reader["Difference"]) : 0,
                                    diffAvgVsCurrent = reader["DiffAvgVsCurrent"] != DBNull.Value ? Convert.ToDecimal(reader["DiffAvgVsCurrent"]) : 0,
                                    stockValueClosing = reader["StockValueClosing"] != DBNull.Value ? Convert.ToDecimal(reader["StockValueClosing"]) : 0,
                                    stockValuePhysical = reader["StockValuePhysical"] != DBNull.Value ? Convert.ToDecimal(reader["StockValuePhysical"]) : 0
                                });
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = results;
                response.Count = results.Count;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Analysis Error: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> GetAccessorySalesByChannel(DateTime startDate, DateTime endDate)
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
                SELECT 
                    WHSE,
                    CODE,
                    MAX(Description) AS Description,
                    Territory,
                    SUM(ISNULL(BVCMTDQTY, 0)) AS SumOfBVCMTDQTY
                FROM WWSalesDetailTEMP 
                WHERE (
                    (ProdCode = 'ACC' OR ProdCode = 'OBA')
                    AND in_date >= @StartDate
                    AND in_date <= @EndDate
                )
                GROUP BY WHSE, CODE, Territory
                ORDER BY WHSE, CODE, Territory";

                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.Date;
                        cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.Date.AddDays(1).AddTicks(-1); // End of day

                        cmd.CommandTimeout = 300;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    whse = reader["WHSE"]?.ToString().Trim(),
                                    code = reader["CODE"]?.ToString().Trim(),
                                    description = reader["Description"]?.ToString().Trim(),
                                    territory = reader["Territory"]?.ToString().Trim(),
                                    sumOfBVCMTDQTY = reader["SumOfBVCMTDQTY"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["SumOfBVCMTDQTY"])
                                        : 0m
                                });
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = results;
                response.Count = results.Count;
                response.Message = $"Loaded {results.Count} accessory sales records.";
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }
        public async Task<ApiResposne> GetItemSalesSummary()
        {
            var response = new ApiResposne();
            var results = new List<object>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = @"
            SELECT 
                S.WHSE,
                S.CODE,
                I.INV_DESCRIPTION,
                I.PROD,
                I.MISC_1 AS [Group],
                S.TotalQty
            FROM
            (
                SELECT 
                    WHSE,
                    CODE,
                    SUM(ISNULL(BVCMTDQTY,0)) AS TotalQty
                FROM WWSalesDetailTEMP
                WHERE CODE <> ''
                GROUP BY WHSE, CODE
            ) AS S
            INNER JOIN WWInventory I
                ON S.CODE = I.CODE
                AND S.WHSE = I.WHSE
            WHERE I.PROD IN ('ACC','OBA','HCC','OBH')
            ORDER BY S.WHSE, S.CODE";

                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    whse = reader["WHSE"]?.ToString().Trim(),
                                    code = reader["CODE"]?.ToString().Trim(),
                                    description = reader["INV_DESCRIPTION"]?.ToString().Trim(),
                                    prod = reader["PROD"]?.ToString().Trim(),
                                    group = reader["Group"]?.ToString().Trim(),
                                    totalQty = reader["TotalQty"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["TotalQty"])
                                                : 0
                                });
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = results;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }

        // 2 ROW


        public async Task<List<string>> GetWarehouses()
        {
            var warehouses = new List<string>();
            using (SqlConnection conn = new SqlConnection(_sqlConn))
            {
                string sql = "SELECT DISTINCT WHSE FROM WWInventory ORDER BY WHSE";
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) warehouses.Add(reader["WHSE"].ToString());
                }
            }
            return warehouses;
        }
        public async Task<object> GetCountFileSummary(string fileName, string type)
        {
            string tableName = (type.ToLower() == "hardware") ? "tblCounts" : "tblACCCounts";

            // Hardware ke liye har row ko 1 count karo, Accessory ke liye QtyTotal ka sum lo
            string qtyColumnExpression = (type.ToLower() == "hardware")
                ? "COUNT(ID)" // Hardware mein har entry 1 quantity hai
                : "SUM(CASE WHEN QtyTotal IS NULL OR QtyTotal = 0 THEN 1 ELSE QtyTotal END)";

            string sql = $@"SELECT 
                        MAX(CountFile) as CountFile, 
                        MAX(Whse) as Whse, 
                        COUNT(ID) as CountEntries, 
                        {qtyColumnExpression} as SumOfQtyTotal 
                    FROM {tableName} 
                    WHERE CountFile = @file";

            using (SqlConnection conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@file", fileName);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new
                            {
                                countFile = reader["CountFile"]?.ToString(),
                                whse = reader["Whse"]?.ToString(),
                                countEntries = reader["CountEntries"] != DBNull.Value ? Convert.ToInt32(reader["CountEntries"]) : 0,
                                sumOfQtyTotal = reader["SumOfQtyTotal"] != DBNull.Value ? Convert.ToDecimal(reader["SumOfQtyTotal"]) : 0
                            };
                        }
                    }
                }
            }
            return null;
        }
        public async Task<List<object>> GetCountFiles(string type)
        {
            var list = new List<object>();
            string tableName = (type.ToLower() == "hardware") ? "tblCounts" : "tblACCCounts";

          
            string qtyLogic = (type.ToLower() == "hardware")
                              ? "COUNT(ID)"
                              : "SUM(ISNULL(QtyTotal, 0))";

            string sql = $@"
        SELECT 
            CountFile,
            Whse,
            COUNT(ID) AS CountEntries,
            {qtyLogic} AS SumOfQtyTotal
        FROM {tableName}
        GROUP BY CountFile, Whse
        ORDER BY CountFile";

            using (SqlConnection conn = new SqlConnection(_sqlConn))
            {
                try
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                countFile = reader["CountFile"]?.ToString(),
                                whse = reader["Whse"]?.ToString(),
                                countEntries = Convert.ToInt32(reader["CountEntries"]),
                                sumOfQtyTotal = Convert.ToDecimal(reader["SumOfQtyTotal"])
                            });
                        }
                    }
                }
                catch (SqlException ex)
                {
                    // Timeout ya Connection error handle karne ke liye
                    throw new Exception("Database connection failed: " + ex.Message);
                }
            }
            return list;
        }

        public async Task<bool> AssignCountsToWarehouse(AssignWarehouseRequest request)
        {
            try
            {
                // 1. Validation: Check if values are coming
                if (string.IsNullOrEmpty(request.CountType) || string.IsNullOrEmpty(request.CountFile))
                    return false;

                string tableName = (request.CountType.ToLower() == "hardware") ? "tblCounts" : "tblACCCounts";

                using (SqlConnection conn = new SqlConnection(_sqlConn))
                {
                    string sql = $@"UPDATE {tableName} SET Whse = @whse WHERE CountFile = @file";

                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        // AddWithValue ki jagah explicit types use karna better hota hai
                        cmd.Parameters.AddWithValue("@whse", (object)request.Warehouse ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@file", (object)request.CountFile ?? DBNull.Value);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Yahan breakpoint lagao aur 'ex.Message' check karo
                Console.WriteLine("Update Error: " + ex.Message);
                throw;
            }
        }


        public async Task<ApiResposne> ImportACCCounts(Stream excelStream, string fileName)
        {
            var response = new ApiResposne();
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(excelStream);
                var ws = package.Workbook.Worksheets[0];

                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();
                    int intRowCount = 2; 

                    while (ws.Cells[intRowCount, 1].Value != null)
                    {
                        string strWhse = ws.Cells[intRowCount, 1].Text.ToUpper().Trim();
                        string strProdCode = ws.Cells[intRowCount, 2].Text.ToUpper().Trim();
                        string strPartNumber = ws.Cells[intRowCount, 3].Text.ToUpper().Trim();
                        string strDescription = ws.Cells[intRowCount, 4].Text.Trim();

                        double dblQtyTotal = 0;
                        bool blnValueFound = false;

                        for (int col = 5; col <= 10; col++)
                        {
                            var cellValue = ws.Cells[intRowCount, col].Value;
                            if (cellValue != null && cellValue.ToString() != "")
                            {
                                blnValueFound = true;
                                double val = 0;
                                double.TryParse(cellValue.ToString(), out val);
                                dblQtyTotal += val;
                            }
                        }

                        if (blnValueFound)
                        {
                            string sql = @"INSERT INTO tblACCCounts 
                                 (Whse, ProdCode, PartNo, Description, QtyTotal, RowNumber, CountFile) 
                                 VALUES (@Whse, @Prod, @Part, @Desc, @Qty, @Row, @File)";

                            using (var cmd = new SqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Whse", strWhse);
                                cmd.Parameters.AddWithValue("@Prod", strProdCode);
                                cmd.Parameters.AddWithValue("@Part", strPartNumber);
                                cmd.Parameters.AddWithValue("@Desc", strDescription);
                                cmd.Parameters.AddWithValue("@Qty", dblQtyTotal);
                                cmd.Parameters.AddWithValue("@Row", intRowCount);
                                cmd.Parameters.AddWithValue("@File", fileName);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        intRowCount++; // Next row
                    }
                }

                response.Success = true;
                response.Message = "Accessory counts imported successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<ApiResposne> ImportBackOrders(Stream excelStream, string fileName)
        {
            var response = new ApiResposne();
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(excelStream);
                var ws = package.Workbook.Worksheets[0]; // First sheet

                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    // VBA: CurrentDb.Execute ("delete from tblACCBackorders")
                    using (var deleteCmd = new SqlCommand("DELETE FROM tblACCBackorders", conn))
                    {
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    int intRowCount = 2; // VBA: intRowCount = 2

                    // VBA: Do While IsEmpty(...) = False
                    while (ws.Cells[intRowCount, 1].Value != null)
                    {
                        string strWhse = ws.Cells[intRowCount, 1].Text.ToUpper().Trim();
                        string strProdCode = ws.Cells[intRowCount, 2].Text.ToUpper().Trim();
                        string strPartNumber = ws.Cells[intRowCount, 3].Text.ToUpper().Trim();
                        string strDescription = ws.Cells[intRowCount, 4].Text.Trim();

                        double dblQtyTotal = 0;
                        bool blnValueFound = false;

                        // VBA: dblCount = 5 to 10 (Total quantity calculation)
                        for (int col = 5; col <= 10; col++)
                        {
                            var cellValue = ws.Cells[intRowCount, col].Value;
                            if (cellValue != null && cellValue.ToString() != "")
                            {
                                blnValueFound = true;
                                double val = 0;
                                if (double.TryParse(cellValue.ToString(), out val))
                                {
                                    dblQtyTotal += val;
                                }
                            }
                        }

                        // VBA: If blnValueFound = True Then ... Insert
                        if (blnValueFound)
                        {
                            string sql = @"INSERT INTO tblACCBackorders 
                                 (Whse, ProdCode, PartNo, Description, QtyTotal, RowNumber, CountFile) 
                                 VALUES (@Whse, @Prod, @Part, @Desc, @Qty, @Row, @File)";

                            using (var cmd = new SqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Whse", strWhse);
                                cmd.Parameters.AddWithValue("@Prod", strProdCode);
                                cmd.Parameters.AddWithValue("@Part", strPartNumber);
                                cmd.Parameters.AddWithValue("@Desc", strDescription);
                                cmd.Parameters.AddWithValue("@Qty", dblQtyTotal);
                                cmd.Parameters.AddWithValue("@Row", intRowCount);
                                cmd.Parameters.AddWithValue("@File", fileName);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        intRowCount++; // Next row
                    }
                }

                response.Success = true;
                response.Message = "Backorders imported successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }
        public async Task<ACCEditResponse> GetACCCountsForEdit()
        {
            var query = from tc in _dbContext.tblACCCounts
                        join wa in _dbContext.WWAccessories
                        on new { tc.PartNo, tc.Whse } equals new { PartNo = wa.CODE, Whse = wa.WHSE } into joined
                        from wa in joined.DefaultIfEmpty() // Left Join logic
                        orderby tc.Whse, tc.PartNo
                        select new ACCCountsEditBO
                        {
                            ID = tc.ID,
                            Whse = tc.Whse,
                            InvGroup = wa.InvGroup,
                            ProdCode = tc.ProdCode,
                            PartNo = tc.PartNo,
                            Description = tc.Description,
                            QtyTotal = (double)tc.QtyTotal,
                            RowNumber = (int)tc.RowNumber,
                            CountFile = tc.CountFile
                        };

            var items = await query.ToListAsync();
            return new ACCEditResponse
            {
                Items = items,
                TotalItems = items.Count
            };
        }
        public async Task<bool> UpdateACCCount(int id, double newQty)
        {
            var record = await _dbContext.tblACCCounts.FindAsync(id);
            if (record == null) return false;

            record.QtyTotal = (int?)newQty; 
            return await _dbContext.SaveChangesAsync() > 0; 
        }
        //public async Task<ApiResposne> LoadSpireSalesAndReceipts(string type)
        //{
        //    var response = new ApiResposne();

        //    try
        //    {
        //        if (type == "Sales" || type == "Both")
        //        {
        //            string lastInvoice = "";
        //            using (var sqlConn = new SqlConnection(_sqlConn))
        //            {
        //                await sqlConn.OpenAsync();
        //                string query = "SELECT TOP 1 NUMBER FROM WWSalesDetailTEMP ORDER BY NUMBER DESC, RECNO DESC";
        //                using (var cmd = new SqlCommand(query, sqlConn))
        //                {
        //                    var result = await cmd.ExecuteScalarAsync();
        //                    lastInvoice = result?.ToString() ?? "";
        //                }
        //            }

        //            using (var pgConn = new NpgsqlConnection(_pgConn))
        //            {
        //                await pgConn.OpenAsync();
        //                string pgQuery = @"
        //            SELECT invoice_no, TO_CHAR(invoice_date, 'YYYYMMDD') as in_date, sequence, 
        //                   whse, part_no, description, product_code, committed_qty, unit_price 
        //            FROM sales_history_items 
        //            WHERE invoice_no > @lastInv 
        //            ORDER BY invoice_no, sequence";

        //                using (var pgCmd = new NpgsqlCommand(pgQuery, pgConn))
        //                {
        //                    pgCmd.Parameters.AddWithValue("@lastInv", lastInvoice);
        //                    using (var reader = await pgCmd.ExecuteReaderAsync())
        //                    {
        //                        using (var sqlConn = new SqlConnection(_sqlConn))
        //                        {
        //                            await sqlConn.OpenAsync();
        //                            while (await reader.ReadAsync())
        //                            {
        //                                string insertSql = @"INSERT INTO WWSalesDetailTEMP (NUMBER, IN_DATE, RECNO, whse, CODE, description, ProdCode, BVCMTDQTY, BVUNITPRICE) 
        //                                           VALUES (@no, @dt, @seq, @wh, @cd, @dsc, @pcd, @qty, @prc)";

        //                                using (var insCmd = new SqlCommand(insertSql, sqlConn))
        //                                {
        //                                    insCmd.Parameters.AddWithValue("@no", reader["invoice_no"]);
        //                                    insCmd.Parameters.AddWithValue("@dt", reader["in_date"]);
        //                                    insCmd.Parameters.AddWithValue("@seq", reader["sequence"]);
        //                                    insCmd.Parameters.AddWithValue("@wh", reader["whse"]);
        //                                    insCmd.Parameters.AddWithValue("@cd", reader["part_no"]);
        //                                    insCmd.Parameters.AddWithValue("@dsc", reader["description"]);
        //                                    insCmd.Parameters.AddWithValue("@pcd", reader["product_code"]);
        //                                    insCmd.Parameters.AddWithValue("@qty", reader["committed_qty"]);
        //                                    insCmd.Parameters.AddWithValue("@prc", reader["unit_price"]);
        //                                    await insCmd.ExecuteNonQueryAsync();
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        if (type == "Receipts" || type == "Both")
        //        {
        //            long lastReceiptKey = 0;
        //            using (var sqlConn = new SqlConnection(_sqlConn))
        //            {
        //                await sqlConn.OpenAsync();
        //                var cmd = new SqlCommand("SELECT TOP 1 RECPT_KEY FROM WWReceiptsTEMP ORDER BY RECPT_KEY DESC", sqlConn);
        //                var result = await cmd.ExecuteScalarAsync();
        //                lastReceiptKey = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;
        //            }

        //            using (var pgConn = new NpgsqlConnection(_pgConn))
        //            {
        //                await pgConn.OpenAsync();
        //                string pgQuery = @"
        //            SELECT r.id, TO_CHAR(r.receive_date, 'YYYYMMDD') as invr_date, i.whse, i.part_no, r.qty, r.receive_date 
        //            FROM inventory_receipts r 
        //            JOIN inventory i ON r.inventory_id = i.id 
        //            WHERE r.id > @lastId";

        //                using (var pgCmd = new NpgsqlCommand(pgQuery, pgConn))
        //                {
        //                    pgCmd.Parameters.AddWithValue("@lastId", lastReceiptKey);
        //                    using (var reader = await pgCmd.ExecuteReaderAsync())
        //                    {
        //                        using (var sqlConn = new SqlConnection(_sqlConn))
        //                        {
        //                            await sqlConn.OpenAsync();
        //                            while (await reader.ReadAsync())
        //                            {
        //                                string insSql = "INSERT INTO WWReceiptsTEMP (RECPT_KEY, INVR_DATE, whse, CODE, qty, ReceiptDate) VALUES (@id, @dt, @wh, @cd, @qty, @rdt)";
        //                                using (var insCmd = new SqlCommand(insSql, sqlConn))
        //                                {
        //                                    insCmd.Parameters.AddWithValue("@id", reader["id"]);
        //                                    insCmd.Parameters.AddWithValue("@dt", reader["invr_date"]);
        //                                    insCmd.Parameters.AddWithValue("@wh", reader["whse"]);
        //                                    insCmd.Parameters.AddWithValue("@cd", reader["part_no"]);
        //                                    insCmd.Parameters.AddWithValue("@qty", reader["qty"]);
        //                                    insCmd.Parameters.AddWithValue("@rdt", reader["receive_date"]);
        //                                    await insCmd.ExecuteNonQueryAsync();
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        response.Success = true;
        //        response.Message = "Cross-database Sync successful!";
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Sync Error: " + ex.Message;
        //    }

        //    return response;
        //}

        public async Task<ApiResposne> LoadSpireSalesAndReceipts(string type)
        {
            var response = new ApiResposne();
            var exportData = new List<dynamic>();

            try
            {
                string lastInvoice = "";
                int lastRecNo = 0;

                // ----------------------------
                // SALES SYNC
                // ----------------------------
                if (type == "Sales" || type == "Both")
                {
                    using (var sqlConn = new SqlConnection(_sqlConn))
                    {
                        await sqlConn.OpenAsync();

                        string lastSql = @"SELECT TOP 1 NUMBER, RECNO 
                                   FROM WWSalesDetailTEMP 
                                   ORDER BY NUMBER DESC, RECNO DESC";

                        using var cmd = new SqlCommand(lastSql, sqlConn);
                        using var reader = await cmd.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            lastInvoice = reader["NUMBER"]?.ToString()?.Trim() ?? "";
                            lastRecNo = Convert.ToInt32(reader["RECNO"]);
                        }
                    }

                    using (var pgConn = new NpgsqlConnection(_pgConn))
                    using (var sqlConn = new SqlConnection(_sqlConn))
                    {
                        await pgConn.OpenAsync();
                        await sqlConn.OpenAsync();

                        // -----------------------------------
                        // 1. LOAD REMAINING LINES OF LAST INVOICE
                        // -----------------------------------
                        string pgQuery1 = @"
                    SELECT invoice_no,
                           TO_CHAR(invoice_date,'YYYYMMDD') AS in_date,
                           sequence,
                           whse,
                           part_no,
                           description,
                           product_code,
                           committed_qty,
                           unit_price
                    FROM sales_history_items
                    WHERE invoice_no = @lastInv
                    AND sequence > @lastSeq
                    ORDER BY invoice_no, sequence";

                        using (var cmd = new NpgsqlCommand(pgQuery1, pgConn))
                        {
                            cmd.Parameters.AddWithValue("@lastInv", lastInvoice);
                            cmd.Parameters.AddWithValue("@lastSeq", lastRecNo);

                            using var reader = await cmd.ExecuteReaderAsync();

                            using var bulk = new SqlBulkCopy(sqlConn);
                            bulk.DestinationTableName = "WWSalesDetailTEMP";
                            bulk.BulkCopyTimeout = 0;

                            bulk.ColumnMappings.Add("invoice_no", "NUMBER");
                            bulk.ColumnMappings.Add("in_date", "IN_DATE");
                            bulk.ColumnMappings.Add("sequence", "RECNO");
                            bulk.ColumnMappings.Add("whse", "WHSE");
                            bulk.ColumnMappings.Add("part_no", "CODE");
                            bulk.ColumnMappings.Add("description", "Description");
                            bulk.ColumnMappings.Add("product_code", "ProdCode");
                            bulk.ColumnMappings.Add("committed_qty", "BVCMTDQTY");
                            bulk.ColumnMappings.Add("unit_price", "BVUNITPRICE");

                            await bulk.WriteToServerAsync(reader);
                        }

                        // -----------------------------------
                        // 2. LOAD NEW INVOICES
                        // -----------------------------------
                        string pgQuery2 = @"
                    SELECT invoice_no,
                           TO_CHAR(invoice_date,'YYYYMMDD') AS in_date,
                           sequence,
                           whse,
                           part_no,
                           description,
                           product_code,
                           committed_qty,
                           unit_price
                    FROM sales_history_items
                    WHERE invoice_no > @lastInv
                    ORDER BY invoice_no, sequence";

                        using (var cmd = new NpgsqlCommand(pgQuery2, pgConn))
                        {
                            cmd.Parameters.AddWithValue("@lastInv", lastInvoice);

                            using var reader = await cmd.ExecuteReaderAsync();

                            using var bulk = new SqlBulkCopy(sqlConn);
                            bulk.DestinationTableName = "WWSalesDetailTEMP";
                            bulk.BulkCopyTimeout = 0;

                            bulk.ColumnMappings.Add("invoice_no", "NUMBER");
                            bulk.ColumnMappings.Add("in_date", "IN_DATE");
                            bulk.ColumnMappings.Add("sequence", "RECNO");
                            bulk.ColumnMappings.Add("whse", "WHSE");
                            bulk.ColumnMappings.Add("part_no", "CODE");
                            bulk.ColumnMappings.Add("description", "Description");
                            bulk.ColumnMappings.Add("product_code", "ProdCode");
                            bulk.ColumnMappings.Add("committed_qty", "BVCMTDQTY");
                            bulk.ColumnMappings.Add("unit_price", "BVUNITPRICE");

                            await bulk.WriteToServerAsync(reader);
                        }

                        // -----------------------------------
                        // TERRITORY UPDATE
                        // -----------------------------------
                        string updateTerritory = @"
                    UPDATE t
                    SET t.Territory = s.CustTerritory
                    FROM WWSalesDetailTEMP t
                    INNER JOIN SalesActivations s 
                        ON t.NUMBER = s.invoice
                    WHERE t.NUMBER >= @lastInv";

                        using (var cmd = new SqlCommand(updateTerritory, sqlConn))
                        {
                            cmd.Parameters.AddWithValue("@lastInv", lastInvoice);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // -----------------------------------
                        // FETCH SALES FOR EXPORT
                        // -----------------------------------
                        string fetchSales = @"SELECT NUMBER, IN_DATE, WHSE, CODE, Description, BVCMTDQTY
                                      FROM WWSalesDetailTEMP
                                      WHERE NUMBER >= @lastInv";

                        using (var cmd = new SqlCommand(fetchSales, sqlConn))
                        {
                            cmd.Parameters.AddWithValue("@lastInv", lastInvoice);

                            using var rdr = await cmd.ExecuteReaderAsync();

                            while (await rdr.ReadAsync())
                            {
                                exportData.Add(new
                                {
                                    Number = rdr["NUMBER"],
                                    Date = rdr["IN_DATE"],
                                    Whse = rdr["WHSE"],
                                    Code = rdr["CODE"],
                                    Description = rdr["Description"],
                                    Qty = rdr["BVCMTDQTY"]
                                });
                            }
                        }
                    }
                }

                // ----------------------------
                // RECEIPTS SYNC
                // ----------------------------
                if (type == "Receipts" || type == "Both")
                {
                    long lastReceipt = 0;

                    using (var sqlConn = new SqlConnection(_sqlConn))
                    {
                        await sqlConn.OpenAsync();

                        string lastRecSql = @"SELECT ISNULL(MAX(RECPT_KEY),0) 
                                      FROM WWReceiptsTEMP";

                        using var cmd = new SqlCommand(lastRecSql, sqlConn);
                        lastReceipt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    }

                    using (var pgConn = new NpgsqlConnection(_pgConn))
                    using (var sqlConn = new SqlConnection(_sqlConn))
                    {
                        await pgConn.OpenAsync();
                        await sqlConn.OpenAsync();

                        string pgQuery = @"
                    SELECT r.id,
                           TO_CHAR(r.receive_date,'YYYYMMDD') AS invr_date,
                           i.whse,
                           i.part_no,
                           r.qty,
                           r.receive_date
                    FROM inventory_receipts r
                    INNER JOIN inventory i
                        ON r.inventory_id = i.id
                    WHERE r.id > @lastId";

                        using (var cmd = new NpgsqlCommand(pgQuery, pgConn))
                        {
                            cmd.Parameters.AddWithValue("@lastId", lastReceipt);

                            using var reader = await cmd.ExecuteReaderAsync();

                            using var bulk = new SqlBulkCopy(sqlConn);
                            bulk.DestinationTableName = "WWReceiptsTEMP";
                            bulk.BulkCopyTimeout = 0;

                            bulk.ColumnMappings.Add("id", "RECPT_KEY");
                            bulk.ColumnMappings.Add("invr_date", "INVR_DATE");
                            bulk.ColumnMappings.Add("whse", "WHSE");
                            bulk.ColumnMappings.Add("part_no", "CODE");
                            bulk.ColumnMappings.Add("qty", "QTY");
                            bulk.ColumnMappings.Add("receive_date", "ReceiptDate");

                            await bulk.WriteToServerAsync(reader);
                        }

                        // Fetch receipts for export
                        string fetchRec = @"SELECT RECPT_KEY, INVR_DATE, WHSE, CODE, QTY
                                    FROM WWReceiptsTEMP
                                    WHERE RECPT_KEY > @lastId";

                        using (var cmd = new SqlCommand(fetchRec, sqlConn))
                        {
                            cmd.Parameters.AddWithValue("@lastId", lastReceipt);

                            using var rdr = await cmd.ExecuteReaderAsync();

                            while (await rdr.ReadAsync())
                            {
                                exportData.Add(new
                                {
                                    ID = rdr["RECPT_KEY"],
                                    Date = rdr["INVR_DATE"],
                                    Whse = rdr["WHSE"],
                                    Code = rdr["CODE"],
                                    Qty = rdr["QTY"]
                                });
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = exportData;
                response.Message = $"Sync successful. {exportData.Count} records loaded.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Sync Error: " + ex.Message +
                                   (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "");
            }

            return response;
        }

        public async Task<ApiResposne> GetAccessoryDiscrepancies()
        {
            var response = new ApiResposne();
            var list = new List<dynamic>();

            try
            {
                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    // Complete SQL matching Access logic
                    string query = @"
WITH qryACCcountSummary AS (
    SELECT 
        Whse, 
        PartNo, 
        MAX(ProdCode) AS Prod, 
        MAX(Description) AS [Desc], 
        SUM(QtyTotal) AS SumOfQtyTotal
    FROM tblACCCounts
    GROUP BY Whse, PartNo
)
, qryBackOrders AS (
    SELECT Whse, PartNo, SUM(QtyTotal) AS QtyTotal
    FROM tblACCBackOrders
    GROUP BY Whse, PartNo
)
SELECT 
    a.WHSE, 
    a.InvGroup, 
    a.PROD, 
    a.CODE, 
    a.Description, 
    ISNULL(cs.SumOfQtyTotal, 0) AS [Count],
    a.ONHAND AS BVOnhand,
    (ISNULL(cs.SumOfQtyTotal, 0) - a.ONHAND) AS Diff,
    ISNULL(bo.QtyTotal, 0) AS Backorders,
    a.QtyAdjusted AS AlreadyAdjusted,
    ISNULL(a.AdjustedBy, 0) AS AlreadyAdjustedBy,
    (ISNULL(cs.SumOfQtyTotal, 0) - a.ONHAND - ISNULL(bo.QtyTotal, 0) - ISNULL(a.AdjustedBy, 0)) AS RequiredAdjustment,
    a.CurrentCost, 
    a.AvgCost,
    CASE WHEN cs.SumOfQtyTotal IS NULL THEN 'No' ELSE 'Yes' END AS InCountSheet
FROM WWAccessories a
LEFT JOIN qryACCcountSummary cs
    ON a.CODE = cs.PartNo AND a.WHSE = cs.Whse
LEFT JOIN qryBackOrders bo
    ON a.CODE = bo.PartNo AND a.WHSE = bo.Whse
WHERE 
    (a.InvGroup <> 'SPECIAL' AND (ISNULL(cs.SumOfQtyTotal, 0) - a.ONHAND) <> 0)
    OR
    (a.InvGroup <> 'SPECIAL' AND a.ONHAND <> 0 AND cs.SumOfQtyTotal IS NULL)
ORDER BY a.CODE;
";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new
                                {
                                    Whse = reader["WHSE"],
                                    InvGroup = reader["InvGroup"],
                                    Prod = reader["PROD"],
                                    Code = reader["CODE"],
                                    Description = reader["Description"],
                                    Count = reader["Count"],
                                    BVOnhand = reader["BVOnhand"],
                                    Diff = reader["Diff"],
                                    Backorders = reader["Backorders"],
                                    AlreadyAdjusted = reader["AlreadyAdjusted"],
                                    AlreadyAdjustedBy = reader["AlreadyAdjustedBy"],
                                    RequiredAdjustment = reader["RequiredAdjustment"],
                                    CurrentCost = reader["CurrentCost"],
                                    AvgCost = reader["AvgCost"],
                                    InCountSheet = reader["InCountSheet"]
                                });
                            }
                        }
                    }
                }

                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching Accessory Discrepancies: " + ex.Message +
                                   (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "");
            }

            return response;
        }



        public async Task<ApiResposne> GetCountedNotInBV()
        {
            var response = new ApiResposne();
            var list = new List<dynamic>();

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                string query = @"
            WITH qryACCcountSummary AS (
                SELECT Whse, PartNo, MAX(ProdCode) AS Prod, MAX(Description) AS [Desc], SUM(QtyTotal) AS SumOfQtyTotal
                FROM tblACCCounts
                GROUP BY Whse, PartNo
                HAVING SUM(QtyTotal) <> 0
            )
            SELECT 
                cs.Whse, 
                cs.Prod, 
                cs.PartNo, 
                cs.[Desc] as Description, 
                cs.SumOfQtyTotal as [Count]
            FROM qryACCcountSummary cs 
            LEFT JOIN WWAccessories a ON cs.PartNo = a.CODE AND cs.Whse = a.WHSE
            WHERE a.WHSE IS NULL"; 

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Whse = reader["Whse"],
                            Prod = reader["Prod"],
                            PartNo = reader["PartNo"],
                            Description = reader["Description"],
                            Count = reader["Count"]
                        });
                    }
                }
            }
            response.Success = true;
            response.Result = list;
            return response;
        }


        public async Task<ApiResposne> GetOnhandNotCounteds()
        {
            var response = new ApiResposne();
            var list = new List<dynamic>();

            using (var conn = new SqlConnection(_sqlConn))
            {
                await conn.OpenAsync();

                string query = @"
            WITH qryACCcountSummary AS (
                SELECT Whse, PartNo, SUM(QtyTotal) AS SumOfQtyTotal
                FROM tblACCCounts
                GROUP BY Whse, PartNo
                HAVING SUM(QtyTotal) <> 0
            )
            SELECT 
                a.WHSE, a.InvGroup, a.PROD, a.CODE, a.Description, 
                a.ONHAND, a.CurrentCost, a.AvgCost
            FROM WWAccessories a
            LEFT JOIN qryACCcountSummary cs ON a.CODE = cs.PartNo AND a.WHSE = cs.Whse
            WHERE a.ONHAND <> 0 AND cs.PartNo IS NULL";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Whse = reader["WHSE"],
                            InvGroup = reader["InvGroup"],
                            Prod = reader["PROD"],
                            Code = reader["CODE"],
                            Description = reader["Description"],
                            Onhand = reader["ONHAND"],
                            CurrentCost = reader["CurrentCost"],
                            AvgCost = reader["AvgCost"]
                        });
                    }
                }
            }
            response.Success = true;
            response.Result = list;
            return response;
        }
        public async Task<ApiResposne> GetLoadedStockStatus()
        {
            var response = new ApiResposne();
            var list = new List<dynamic>();

            try
            {
                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();

                    string query = @"
            SELECT WHSE, InvGroup, PROD, CODE, Description, ONHAND, CurrentCost, AvgCost
            FROM WWAccessories
            ORDER BY WHSE, InvGroup, PROD, CODE";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                Whse = reader["WHSE"]?.ToString(),
                                InvGroup = reader["InvGroup"]?.ToString(),
                                Prod = reader["PROD"]?.ToString(),
                                Code = reader["CODE"]?.ToString(),
                                Description = reader["Description"]?.ToString(),
                                Onhand = reader["ONHAND"],
                                CurrentCost = reader["CurrentCost"],
                                AvgCost = reader["AvgCost"]
                            });
                        }
                    }
                }
                response.Success = true;
                response.Result = list;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error loading Stock Status: " + ex.Message;
            }

            return response;
        }
        public async Task<ApiResposne> ImportBackorders(IFormFile file)
        {
            var response = new ApiResposne();
            var list = new List<BackorderImportDto>();

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "Invalid file.";
                    return response;
                }

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage(stream))
                    {
                        var sheet = package.Workbook.Worksheets[0];
                        int rowCount = sheet.Dimension?.Rows ?? 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            if (sheet.Cells[row, 1].Value == null) break;

                            double totalQty = 0;
                            bool valueFound = false;

                            for (int col = 5; col <= 10; col++)
                            {
                                var cellValue = sheet.Cells[row, col].Value;
                                if (cellValue != null && double.TryParse(cellValue.ToString(), out double val))
                                {
                                    totalQty += val;
                                    valueFound = true;
                                }
                            }

                            if (valueFound)
                            {
                                list.Add(new BackorderImportDto
                                {
                                    Whse = sheet.Cells[row, 1].Text.ToUpper().Trim(),
                                    ProdCode = sheet.Cells[row, 2].Text.ToUpper().Trim(),
                                    PartNo = sheet.Cells[row, 3].Text.ToUpper().Trim(),
                                    Description = sheet.Cells[row, 4].Text.Trim(),
                                    QtyTotal = totalQty
                                });
                            }
                        }
                    }
                }

                if (list.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No valid data found in Excel.";
                    return response;
                }

                using (var conn = new SqlConnection(_sqlConn))
                {
                    await conn.OpenAsync();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            using (var delCmd = new SqlCommand("DELETE FROM tblACCBackorders", conn, trans))
                            {
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            string sql = @"INSERT INTO tblACCBackorders (Whse, ProdCode, PartNo, Description, QtyTotal) 
                                   VALUES (@whse, @prodcode, @partno, @description, @qtytotal)";

                            await ExecuteAsync(sql, list, trans);

                            trans.Commit();

                            response.Success = true;
                            response.Result = list;
                            response.Message = $"Successfully imported {list.Count} records!";
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            throw new Exception("Database Error: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Import Failed: " + ex.Message;
            }
            return response;
        }

        public async Task<int> ExecuteAsync(string sql, object param, SqlTransaction trans)
        {
            int totalRows = 0;

            if (param is System.Collections.IEnumerable list && !(param is string))
            {
                foreach (var item in list)
                {
                    using (var cmd = new SqlCommand(sql, trans.Connection, trans))
                    {
                        MapParameters(cmd, item);
                        totalRows += await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            else
            {
                using (var cmd = new SqlCommand(sql, trans.Connection, trans))
                {
                    MapParameters(cmd, param);
                    totalRows = await cmd.ExecuteNonQueryAsync();
                }
            }
            return totalRows;
        }

        private void MapParameters(SqlCommand cmd, object item)
        {
            if (item == null) return;

            var properties = item.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item) ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@" + prop.Name.ToLower(), value);
            }
        }

        // 2 ROW


    }
}
