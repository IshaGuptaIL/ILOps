using DAL.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Sales.CustomerSales
{
    public class CustomerSalesDA : ICustomerSales
    {
        private readonly string _pgConn;
        private readonly AppDBContext _dbContext;

        public CustomerSalesDA(IConfiguration config, AppDBContext dbContext)
        {
            _pgConn = config.GetConnectionString("spire_Connection");
            _dbContext = dbContext;
        }

        public async Task<List<CustomerGroupBO>> GetCustomerGroupsAsync()
        {
            return await _dbContext.tblCustomerGroups
                .GroupBy(g => new { g.CustGroup, g.GroupName })
                .Select(g => new CustomerGroupBO
                {
                    CustGroup = g.Key.CustGroup,
                    GroupName = g.Key.GroupName,
                    BVCustCount = g.Count()
                })
                .ToListAsync();
        }

        public async Task<List<BVCustomerBO>> GetCustomersInGroupAsync(string groupName)
        {
            return await _dbContext.tblCustomerGroups
                .Where(g => g.CustGroup == groupName)
                .Select(g => new BVCustomerBO
                {
                    BVCustNo = g.BVCustNo,
                    BVName = g.BVName ?? ""
                })
                .ToListAsync();
        }

        public async Task<bool> GenerateCustomerSalesDataAsync(CustomerSalesRequest request, int userId)
        {
            // ⬇️ Set timeout to 10 minutes (600 seconds)
            _dbContext.Database.SetCommandTimeout(600);

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Clear existing data for this user
                    await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblCustomerSalesOutput WHERE UserId = @p0", userId);

                    // 2. Fetch data from Spire
                    var spireData = await FetchSalesHistoryFromSpire(request.StartDate, request.EndDate);

                    // 3. Fetch data from local SalesActivations
                    var activations = await _dbContext.SalesActivations
                        .Where(a => a.InvoiceDate >= request.StartDate && a.InvoiceDate <= request.EndDate)
                        .ToListAsync();

                    // 4. Fetch Customer Groups mapping
                    var groupMapping = await _dbContext.tblCustomerGroups
                        .Where(g => g.CustGroup == request.CustGroup)
                        .ToDictionaryAsync(g => g.BVCustNo);

                    // 5. Combine and process
                    var outputData = new List<TblCustomerSalesOutput>();

                    // Fetch AR statuses for all invoices in this range to avoid N+1 queries
                    var invoiceNumbers = activations.Select(a => a.Invoice).Distinct().ToList();
                    var arData = await FetchARDataFromSpire(invoiceNumbers);

                    foreach (var act in activations)
                    {
                        // RBC New Flow Filter (VBA Parity)
                        bool isNewFlow = (act.POLine?.StartsWith("POL") ?? false) && (act.CustomerPONo?.StartsWith("6") ?? false);
                        if (isNewFlow) continue;

                        if (groupMapping.TryGetValue(act.Customer, out var group))
                        {
                            var spireMatch = spireData.FirstOrDefault(s => s.NUMBER == act.Invoice);
                            var arMatch = arData.Where(ar => ar.Invoice == act.Invoice).ToList();

                            // Calculate AR Status & Balance
                            string arStatus = "Closed";
                            decimal balance = 0;
                            decimal userPayAmount = 0;
                            string userPayMethod = "";

                            var invoiceAR = arMatch.FirstOrDefault(ar => ar.Code == "I" || ar.Code == "C");
                            if (invoiceAR != null)
                            {
                                balance = invoiceAR.Balance;
                                if (balance == 0) arStatus = "Closed";
                                else if (balance == invoiceAR.DebitAmt || balance == (invoiceAR.CreditAmt * -1)) arStatus = "Open";
                                else arStatus = "Partial";

                                // Payments linked to this invoice
                                var payments = arMatch.Where(ar => ar.CodeTrans == "P" && (ar.MemoTrans?.StartsWith("Inv:") ?? false)).ToList();
                                foreach (var p in payments)
                                {
                                    userPayAmount += p.CreditTrans;
                                    if (string.IsNullOrEmpty(userPayMethod) && !string.IsNullOrEmpty(p.MemoTrans))
                                    {
                                        int ccIdx = p.MemoTrans.IndexOf("CC:");
                                        if (ccIdx != -1)
                                        {
                                            int start = ccIdx + 3;
                                            int end = p.MemoTrans.IndexOf(" ", start);
                                            if (end == -1) end = p.MemoTrans.Length;
                                            userPayMethod = p.MemoTrans.Substring(start, end - start);
                                        }
                                    }
                                }
                            }

                            var row = new TblCustomerSalesOutput
                            {
                                UserId = userId,
                                WebOrderID = act.WebOrderID,
                                Invoice = act.Invoice,
                                InvoiceDate = act.InvoiceDate,
                                VoicePlanDescription = act.VoicePlanDescription,
                                DataPlanDescription = act.DataPlanDescription,
                                CellPhoneNo = act.CellPhoneNo,
                                UserName = act.UserName,
                                PONo = CleanPONo(act.CustomerPONo),
                                CostBudgetCode = act.CostBudgetCode,
                                PartNumber = act.PartNumber,
                                HardwareDescription = act.PartNumber == "COAM" ? "" : act.Description,
                                HDWQty = (int?)act.Qty,
                                IMEIESN = act.IMEIESN,
                                AccParts = ParseACCparts(act.FreeAccessoryPart),
                                AccessoryDescription = act.FreeAccessory,
                                AccQtys = ParseACCQtys(act.FreeAccessoryPart),
                                ShipToProvince = GetProvince(act.ShipToPostal),
                                InvoiceNet = (decimal?)act.InvoiceNet,
                                InvoiceShipping = (decimal?)act.InvoiceShipping,
                                InvoiceTaxes = (decimal?)act.InvoiceTaxes,
                                InvoiceTotal = (decimal?)act.InvoiceTotal,
                                CustGroup = group.CustGroup,
                                CustNO = act.Customer,
                                TypeOfService = GetTypeOfService(act.RecordType, act.RecordTypeExtended),
                                PinNumber = act.PinNo,
                                MSDCode = act.MSD,
                                CustomerName = act.CustName,
                                Territory = act.CustTerritory,
                                AccountCode = act.AccountCode,
                                AuthorizedDepartment = act.AuthorizedDepartment,
                                HardwareCharge = (decimal?)(act.ProductCode == "HCC" ? (act.Qty * act.ItemSellPrice) : 0),
                                AccessoryCharge =
    (decimal)(act.InvoiceNet ?? 0) -
    (act.ProductCode == "HCC"
        ? ((decimal)act.Qty * (decimal)(act.ItemSellPrice ?? 0))
        : 0m),
                                ARStatus = arStatus,
                                Balance = balance,
                                UserPayAmount = userPayAmount,
                                UserPayMethod = userPayMethod,
                                CreatedBy = userId,
                                CreatedDate = DateTime.Now
                            };

                            if (spireMatch != null)
                            {
                                row.HSTGST = spireMatch.BVSLSTAXTOTAMT1;
                                row.PSTQST = spireMatch.BVSLSTAXTOTAMT2;
                                row.GSTRate = spireMatch.BVSLSTAXPCT1;
                                row.PSTRate = spireMatch.BVSLSTAXPCT2;
                                row.BulkOrderID = spireMatch.FOB;
                                row.ShipToAddress = spireMatch.ShipToAddress;
                                row.ShipToStreetAddress = spireMatch.ShipToStreet;
                                row.ShipToCity = spireMatch.ShipToCity;
                                row.ShipToPostal = spireMatch.ShipToPostal;
                            }

                            outputData.Add(row);
                        }
                    }

                    // 6. Save to DB
                    await _dbContext.tblCustomerSalesOutput.AddRangeAsync(outputData);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<List<CustomerSalesRow>> GetGeneratedDataAsync(string groupName)
        {
            return await _dbContext.tblCustomerSalesOutput
                .Where(o => o.CustGroup == groupName)
                .Select(o => new CustomerSalesRow
                {
                    WebOrderID = o.WebOrderID,
                    Invoice = o.Invoice,
                    InvoiceDate = o.InvoiceDate,
                    VoicePlanDescription = o.VoicePlanDescription,
                    DataPlanDescription = o.DataPlanDescription,
                    CellPhoneNo = o.CellPhoneNo,
                    UserName = o.UserName,
                    PONo = o.PONo,
                    CostBudgetCode = o.CostBudgetCode,
                    PartNumber = o.PartNumber,
                    HardwareDescription = o.HardwareDescription,
                    HDWQty = o.HDWQty,
                    IMEIESN = o.IMEIESN,
                    AccParts = o.AccParts,
                    AccessoryDescription = o.AccessoryDescription,
                    AccQtys = o.AccQtys,
                    ShipToProvince = o.ShipToProvince,
                    InvoiceNet = o.InvoiceNet,
                    InvoiceShipping = o.InvoiceShipping,
                    InvoiceTaxes = o.InvoiceTaxes,
                    InvoiceTotal = o.InvoiceTotal,
                    CustGroup = o.CustGroup,
                    CustNO = o.CustNO,
                    TypeOfService = o.TypeOfService,
                    PinNumber = o.PinNumber,
                    HSTGST = o.HSTGST,
                    PSTQST = o.PSTQST,
                    MSDCode = o.MSDCode,
                    CustomerName = o.CustomerName,
                    Territory = o.Territory,
                    AccountCode = o.AccountCode,
                    AuthorizedDepartment = o.AuthorizedDepartment,
                    ShipToAddress = o.ShipToAddress,
                    ShipToStreetAddress = o.ShipToStreetAddress,
                    ShipToCity = o.ShipToCity,
                    ShipToPostal = o.ShipToPostal,
                    GSTRate = o.GSTRate,
                    PSTRate = o.PSTRate,
                    GSTFlag = o.GSTFlag,
                    PSTFlag = o.PSTFlag,
                    Tax1Code = o.Tax1Code,
                    Tax2Code = o.Tax2Code,
                    PortedCTN = o.PortedCTN,
                    BulkOrderID = o.BulkOrderID,
                    HardwareCharge = o.HardwareCharge,
                    AccessoryCharge = o.AccessoryCharge,
                    ARStatus = o.ARStatus,
                    UserPayAmount = o.UserPayAmount,
                    UserPayMethod = o.UserPayMethod,
                    Balance = o.Balance
                })
                .ToListAsync();
        }

        public async Task<byte[]> ExportToExcelAsync(CustomerSalesRequest request, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sales Detail");

                // Get dynamic columns
                var columns = await _dbContext.tblCustomerColumns
                    .Where(c => c.CustomerGroup == request.CustGroup && c.Include)
                    .OrderBy(c => c.Sequence)
                    .ToListAsync();

                // Get data
                var data = await _dbContext.tblCustomerSalesOutput
                    .Where(o => o.UserId == userId && o.CustGroup == request.CustGroup)
                    .ToListAsync();

                // Get group name for header
                var groupInfo = await _dbContext.tblCustomerGroups
                    .FirstOrDefaultAsync(g => g.CustGroup == request.CustGroup);
                string groupDisplayName = groupInfo?.GroupName ?? request.CustGroup;

                // --- Top Headers (VBA Parity) ---
                int x = 1;
                int y = columns.Count;

                // Tax Reg Numbers on the right
                worksheet.Cells[x++, y].Value = "HST Reg.#: 136424314-RT0001";
                worksheet.Cells[x++, y].Value = "Manitoba PST Reg.#: 136424314-MT0001";
                worksheet.Cells[x++, y].Value = "PEI PST Reg.#: 220709";
                worksheet.Cells[x++, y].Value = "Quebec QST Reg.#: 1212267151-TQ0001";
                worksheet.Cells[x++, y].Value = "Saskatchewan PST Reg.#: 2332005";

                // Company Info on the left
                x = 1;
                worksheet.Cells[x, 1].Value = "Discover Communications";
                worksheet.Cells[x, 1].Style.Font.Bold = true;
                worksheet.Cells[x, 1].Style.Font.Size = 16;
                x++;

                worksheet.Cells[x, 1].Value = "30 Victoria Cres., Brampton, Ontario   L6T1E4";
                worksheet.Cells[x, 1].Style.Font.Bold = true;
                worksheet.Cells[x, 1].Style.Font.Size = 16;
                x += 2;

                worksheet.Cells[x, 1].Value = "Sales Activity";
                worksheet.Cells[x, 1].Style.Font.Bold = true;
                worksheet.Cells[x, 1].Style.Font.Size = 16;
                x++;

                worksheet.Cells[x, 1].Value = $"For the Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}";
                worksheet.Cells[x, 1].Style.Font.Bold = true;
                worksheet.Cells[x, 1].Style.Font.Size = 16;
                x++;

                worksheet.Cells[x, 1].Value = $"Customer: {groupDisplayName}";
                worksheet.Cells[x, 1].Style.Font.Bold = true;
                worksheet.Cells[x, 1].Style.Font.Size = 16;
                x += 3;

                // --- Table Headers ---
                int headerRow = x;
                for (int i = 0; i < columns.Count; i++)
                {
                    worksheet.Cells[headerRow, i + 1].Value = columns[i].Label;
                    worksheet.Cells[headerRow, i + 1].Style.Font.Bold = true;
                }

                // --- Data ---
                int dataStartRow = headerRow + 1;
                int rowIdx = dataStartRow;
                foreach (var item in data)
                {
                    for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                    {
                        var fieldName = columns[colIdx].FieldName;
                        var prop = item.GetType().GetProperty(fieldName);
                        if (prop != null)
                        {
                            var val = prop.GetValue(item);
                            worksheet.Cells[rowIdx, colIdx + 1].Value = val;

                            // Apply text format to columns that might have leading zeros
                            if (fieldName == "IMEIESN" || fieldName == "CustNO" || fieldName == "CellPhoneNo")
                            {
                                worksheet.Cells[rowIdx, colIdx + 1].Style.Numberformat.Format = "@";
                            }
                        }
                    }
                    rowIdx++;
                }

                // --- Totals ---
                worksheet.Cells[rowIdx, 1].Value = "TOTALS:";
                worksheet.Cells[rowIdx, 1].Style.Font.Bold = true;

                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    if (columns[colIdx].SummaryType == "Total")
                    {
                        string colLetter = GetColumnLetter(colIdx + 1);
                        worksheet.Cells[rowIdx, colIdx + 1].Formula = $"SUM({colLetter}{dataStartRow}:{colLetter}{rowIdx - 1})";
                        worksheet.Cells[rowIdx, colIdx + 1].Style.Font.Bold = true;
                    }
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        private string GetColumnLetter(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public async Task<byte[]> ExportToCsvAsync(CustomerSalesRequest request, int userId)
        {
            // Get dynamic columns
            var columns = await _dbContext.tblCustomerColumns
                .Where(c => c.CustomerGroup == request.CustGroup && c.Include)
                .OrderBy(c => c.Sequence)
                .ToListAsync();

            // Get data
            var data = await _dbContext.tblCustomerSalesOutput
                .Where(o => o.UserId == userId && o.CustGroup == request.CustGroup)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();

            // Headers
            csv.AppendLine(string.Join(",", columns.Select(c => $"\"{c.Label.Replace("\"", "\"\"")}\"")));

            // Data
            foreach (var item in data)
            {
                var values = new List<string>();
                foreach (var col in columns)
                {
                    var prop = item.GetType().GetProperty(col.FieldName);
                    var val = prop?.GetValue(item)?.ToString() ?? "";
                    values.Add($"\"{val.Replace("\"", "\"\"")}\"");
                }
                csv.AppendLine(string.Join(",", values));
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<byte[]> ExportPerCustomerAsync(CustomerSalesRequest request, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Get dynamic columns
            var columns = await _dbContext.tblCustomerColumns
                .Where(c => c.CustomerGroup == request.CustGroup && c.Include)
                .OrderBy(c => c.Sequence)
                .ToListAsync();

            // Get data
            var allData = await _dbContext.tblCustomerSalesOutput
            .Where(o => o.UserId == userId && o.CustGroup == request.CustGroup)
            .ToListAsync();

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var groupedData = allData.GroupBy(d => d.CustNO);
                    foreach (var group in groupedData)
                    {
                        var custNo = group.Key;
                        var fileName = $"{custNo}_{request.CustGroup}.xlsx";
                        var entry = archive.CreateEntry(fileName);

                        using (var entryStream = entry.Open())
                        using (var package = new ExcelPackage())
                        {
                            var worksheet = package.Workbook.Worksheets.Add("Sales Detail");

                            // Headers
                            for (int i = 0; i < columns.Count; i++)
                            {
                                worksheet.Cells[1, i + 1].Value = columns[i].Label;
                                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                            }

                            // Data
                            int rowIdx = 2;
                            foreach (var item in group)
                            {
                                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                                {
                                    var fieldName = columns[colIdx].FieldName;
                                    var prop = item.GetType().GetProperty(fieldName);
                                    if (prop != null)
                                    {
                                        worksheet.Cells[rowIdx, colIdx + 1].Value = prop.GetValue(item);
                                    }
                                }
                                rowIdx++;
                            }

                            worksheet.Cells.AutoFitColumns();
                            package.SaveAs(entryStream);
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }

        public async Task<bool> GenerateListByMSDAsync(CustomerSalesRequest request, int userId)
        {
            // Logic from cmdGenByMSD_Click
            // 1. Get the current group name before deleting
            var groupInfo = await _dbContext.tblCustomerGroups
                .Where(g => g.CustGroup == request.CustGroup)
                .FirstOrDefaultAsync();

            string groupName = groupInfo?.GroupName ?? request.CustGroup;

            // 2. Delete existing members
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblCustomerGroups WHERE CustGroup = @p0", request.CustGroup);

            // 3. Re-generate
            var customers = await _dbContext.SalesActivations
                .Where(a => a.MSD == request.MSDCode && a.InvoiceDate >= request.StartDate && a.InvoiceDate <= request.EndDate)
                .GroupBy(a => a.Customer)
                .Select(g => new TblCustomerGroups
                {
                    CustGroup = request.CustGroup,
                    BVCustNo = g.Key,
                    GroupName = groupName,
                    BVName = g.Max(a => a.CustName)
                })
                .ToListAsync();

            await _dbContext.tblCustomerGroups.AddRangeAsync(customers);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> GenerateListByTerritoryAsync(CustomerSalesRequest request, int userId)
        {
            // Remove existing records for this group
            await _dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM tblCustomerGroups WHERE CustGroup = {0}",
                request.CustGroup);

            using (var conn = new NpgsqlConnection(_pgConn))
            {
                await conn.OpenAsync();

                var query = @"
            SELECT 
                a.link_no,
                c.name
            FROM addresses a
            INNER JOIN customers c 
                ON a.link_no = c.cust_no
            INNER JOIN sales_history h 
                ON c.cust_no = h.cust_no
            WHERE 
                a.link_table = 'CUST'
                AND a.addr_type = 'B'
                AND a.sales_terr = @terr
                AND h.invoice_date >= @start
                AND h.invoice_date <= @end
            GROUP BY 
                a.link_no,
                c.name";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    // Territory parameter
                    cmd.Parameters.AddWithValue("terr", request.TerritoryCode);

                    // IMPORTANT: Pass DateTime directly, not string
                    cmd.Parameters.AddWithValue("start", request.StartDate.Date);
                    cmd.Parameters.AddWithValue("end", request.EndDate.Date);

                    var customers = new List<TblCustomerGroups>();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new TblCustomerGroups
                            {
                                CustGroup = request.CustGroup,
                                BVCustNo = reader["link_no"]?.ToString(),
                                GroupName = request.CustGroup,
                                BVName = reader["name"]?.ToString()
                            });
                        }
                    }

                    if (customers.Any())
                    {
                        await _dbContext.tblCustomerGroups.AddRangeAsync(customers);
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }

            return true;
        }

        public async Task<bool> AddFDDealerGroupAsync(int userId)
        {
            // 1. Get existing customers in FDD group from SQL Server
            var existingCustNos = await _dbContext.tblCustomerGroups
                .Where(g => g.CustGroup == "FDD")
                .Select(g => g.BVCustNo)
                .ToListAsync();

            var newCustomers = new List<TblCustomerGroups>();

            // 2. Query Spire (Postgres) for FD Territory customers
            using (var conn = new NpgsqlConnection(_pgConn))
            {
                // Connection timeout (optional)
                conn.ConnectionString += ";Timeout=600;CommandTimeout=600";

                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(@"
            SELECT a.link_no, c.name
            FROM addresses a
            INNER JOIN customers c ON a.link_no = c.cust_no
            WHERE a.link_table = 'CUST' 
              AND a.addr_type = 'B' 
              AND a.sales_terr LIKE 'FD%'
            GROUP BY a.link_no, c.name", conn);

                // Query execution timeout
                cmd.CommandTimeout = 600;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var custNo = reader.GetString(0);
                        var name = reader.GetString(1);

                        // 3. Only add if NOT already in the group
                        if (!existingCustNos.Contains(custNo))
                        {
                            newCustomers.Add(new TblCustomerGroups
                            {
                                CustGroup = "FDD",
                                BVCustNo = custNo,
                                GroupName = "FDDealer",
                                BVName = name,
                                CreatedBy = userId,
                                CreatedDate = DateTime.Now
                            });
                        }
                    }
                }
            }

            if (newCustomers.Any())
            {
                await _dbContext.tblCustomerGroups.AddRangeAsync(newCustomers);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> CreateCustomerGroupAsync(CreateGroupRequest request, int userId)
        {
            // Check if group already exists
            var exists = await _dbContext.tblCustomerGroups.AnyAsync(g => g.CustGroup == request.CustGroup);
            if (exists)
            {
                throw new Exception("A group with this code already exists");
            }

            // Create Group Record (Exactly same as VBA - only 1 customer)
            var newGroup = new TblCustomerGroups
            {
                CustGroup = request.CustGroup,
                GroupName = request.GroupName,
                BVCustNo = request.BVCustNo,
                BVName = ""
            };
            await _dbContext.tblCustomerGroups.AddAsync(newGroup);

            var defaultFields = GetDefaultFields(request.IncludeFrench);

            foreach (var df in defaultFields)
            {
                var column = new TblCustomerColumns
                {
                    CustomerGroup = request.CustGroup,
                    FieldName = df.FieldName,
                    Label = df.Label,
                    Include = df.Include,
                    Sequence = df.Sequence
                };
                await _dbContext.tblCustomerColumns.AddAsync(column);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCustomerGroupAsync(string groupName, int userId)
        {
            // Logic from Command13_Click
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblCustomerGroups WHERE CustGroup = @p0", groupName);
            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM tblCustomerColumns WHERE CustomerGroup = @p0", groupName);
            return true;
        }

        // ─── PRIVATE HELPERS ──────────────────────────────────────────────────

        private async Task<List<dynamic>> FetchSalesHistoryFromSpire(DateTime startDate, DateTime endDate)
        {
            var results = new List<dynamic>();

            await using var conn = new NpgsqlConnection(_pgConn);

            await conn.OpenAsync();

            const string sql = @"
        SELECT 
            h.invoice_no,
            h.sales_tax_total[1],
            h.sales_tax_total[2],
            COALESCE(st1.rate, 0) AS rate1,
            COALESCE(st2.rate, 0) AS rate2,
            h.fob,

            concat_ws(
                ',',
                a.address[1],
                a.address[2],
                a.city,
                a.prov_state,
                a.postal_zip
            ) AS ship_to_address,

            concat_ws(
                ',',
                a.address[1],
                a.address[2]
            ) AS ship_to_street,

            a.city AS ship_to_city,
            a.postal_zip AS ship_to_postal

        FROM sales_history h

        LEFT JOIN addresses a 
            ON h.invoice_no = a.link_no
           AND a.link_table = 'SHIS'
           AND a.addr_type = 'S'

        LEFT JOIN sales_taxes st1 
            ON a.sales_tax_no[1] = st1.tax_no

        LEFT JOIN sales_taxes st2 
            ON a.sales_tax_no[2] = st2.tax_no

        WHERE h.invoice_date >= @start
          AND h.invoice_date <= @end";

            await using var cmd = new NpgsqlCommand(sql, conn);

            // ✅ Pass DateTime values directly (NOT strings)
            cmd.Parameters.AddWithValue(
                "@start",
                NpgsqlTypes.NpgsqlDbType.Date,
                startDate.Date);

            cmd.Parameters.AddWithValue(
                "@end",
                NpgsqlTypes.NpgsqlDbType.Date,
                endDate.Date);

            cmd.CommandTimeout = 600;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    NUMBER = reader.IsDBNull(0)
                        ? ""
                        : reader.GetString(0),

                    BVSLSTAXTOTAMT1 = reader.IsDBNull(1)
                        ? 0m
                        : reader.GetDecimal(1),

                    BVSLSTAXTOTAMT2 = reader.IsDBNull(2)
                        ? 0m
                        : reader.GetDecimal(2),

                    BVSLSTAXPCT1 = reader.IsDBNull(3)
                        ? 0m
                        : reader.GetDecimal(3),

                    BVSLSTAXPCT2 = reader.IsDBNull(4)
                        ? 0m
                        : reader.GetDecimal(4),

                    FOB = reader.IsDBNull(5)
                        ? ""
                        : reader.GetString(5),

                    ShipToAddress = reader.IsDBNull(6)
                        ? ""
                        : reader.GetString(6),

                    ShipToStreet = reader.IsDBNull(7)
                        ? ""
                        : reader.GetString(7),

                    ShipToCity = reader.IsDBNull(8)
                        ? ""
                        : reader.GetString(8),

                    ShipToPostal = reader.IsDBNull(9)
                        ? ""
                        : reader.GetString(9)
                });
            }

            return results;
        }
        private async Task<List<dynamic>> FetchARDataFromSpire(List<string?> invoices)
        {
            var results = new List<dynamic>();
            if (invoices == null || !invoices.Any()) return results;

            await using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();

            // Filter out nulls
            var validInvoices = invoices.Where(i => !string.IsNullOrEmpty(i)).ToList();
            if (!validInvoices.Any()) return results;

            const string sql = @"
                SELECT 
                    ar.ref_no AS Invoice,
                    ar.cust_no AS CustNo,
                    ar.code AS Code,
                    ar.balance AS Balance,
                    ar.debit_amt AS DebitAmt,
                    ar.credit_amt AS CreditAmt,
                    l.applied_amt AS CreditTrans,
                    ar_p.code AS CodeTrans,
                    ar_p.note AS MemoTrans
                FROM ar_transactions ar
                LEFT JOIN ar_transaction_links l ON ar.id = l.debit_id
                LEFT JOIN ar_transactions ar_p ON l.credit_id = ar_p.id
                WHERE ar.ref_no = ANY(@invoices) 
                  AND (ar.code = 'I' OR ar.code = 'C')";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("invoices", validInvoices);
            cmd.CommandTimeout = 600;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    Invoice = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    CustNo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Balance = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3),
                    DebitAmt = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                    CreditAmt = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                    CreditTrans = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                    CodeTrans = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    MemoTrans = reader.IsDBNull(8) ? "" : reader.GetString(8)
                });
            }

            return results;
        }

        private async Task<List<dynamic>> FetchSalesHistoryDetailsFromSpire(List<string> invoices)
        {
            var results = new List<dynamic>();
            if (invoices == null || !invoices.Any()) return results;

            await using var conn = new NpgsqlConnection(_pgConn);
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    i.invoice_no,
                    i.part_no,
                    i.description,
                    i.committed_qty,
                    i.unit_price,
                    inv.misc_1
                FROM sales_history_items i
                LEFT JOIN inventory inv ON i.part_no = inv.part_no
                WHERE i.invoice_no = ANY(@invoices)
                ORDER BY i.invoice_no";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("invoices", invoices);
            cmd.CommandTimeout = 600;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    InvoiceNo = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    PartNo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Qty = reader.IsDBNull(3) ? 0 : (int)reader.GetDecimal(3),
                    UnitPrice = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                    Misc1 = reader.IsDBNull(5) ? "" : reader.GetString(5)
                });
            }

            return results;
        }

        public async Task<List<CustomerFieldBO>> GetCustomerFieldsAsync(string groupName)
        {
            var fields = await _dbContext.tblCustomerColumns
                .Where(c => c.CustomerGroup == groupName)
                .OrderBy(c => c.Sequence)
                .Select(c => new CustomerFieldBO
                {
                    Id = c.Id,
                    CustomerGroup = c.CustomerGroup ?? "",
                    FieldName = c.FieldName ?? "",
                    Label = c.Label ?? "",
                    Include = c.Include,
                    Sequence = c.Sequence,
                    SummaryType = c.SummaryType ?? "",
                    FormatString = c.FormatString ?? "",
                    Level = c.Level ?? 0

                })
                .ToListAsync();

            // If existing group has old/incomplete field list, backfill with new defaults
            if (fields.Count < 40) // New list has 47 fields
            {
                var defaults = GetDefaultFields(false); // Default to English if not set
                foreach (var df in defaults)
                {
                    if (!fields.Any(f => f.FieldName == df.FieldName))
                    {
                        fields.Add(new CustomerFieldBO
                        {
                            CustomerGroup = groupName,
                            FieldName = df.FieldName,
                            Label = df.Label,
                            Include = df.Include,
                            Sequence = df.Sequence
                        });
                    }
                }
                return fields.OrderBy(f => f.Sequence).ToList();
            }

            return fields;
        }

        private List<CustomerFieldBO> GetDefaultFields(bool includeFrench)
        {
            var list = new List<(string Name, int Sequence, bool Include, string Label, string LabelFR)>
            {
                ("CustNO", 100, true, "Cust No", "NUMÉRO DE CLIENT"),
                ("CustomerName", 200, true, "Customer Name", "NOM DU CLIENT"),
                ("Territory", 300, true, "Territory", "TERRITOIRE"),
                ("MSDCode", 400, true, "MSD Code", "CODE MSD"),
                ("AccountCode", 425, true, "Account Code", "Numéro de compte"),
                ("AuthorizedDepartment", 450, true, "Authorized Department", "Département autorisé"),
                ("Invoice", 500, true, "Invoice", "FACTURE"),
                ("InvoiceDate", 600, true, "Invoice Date", "DATE DE FACTURATION"),
                ("TypeOfService", 700, true, "Type Of Service", "SORTE DE SERVICE"),
                ("UserName", 800, true, "User Name", "NOM DE L'UTILISATEUR"),
                ("CellPhoneNo", 900, true, "Cell Phone No", "NUMÉRO DE CELLULAIRE"),
                ("PortedCTN", 950, false, "Ported CTN", "Ported CTN"),
                ("WebOrderID", 1000, true, "Web Order ID", "NUMÉRO DE WEBID"),
                ("PONo", 1100, true, "PO No", "NUMÉRO DE PO"),
                ("CostBudgetCode", 1200, true, "Cost Budget Code", "Coût du Code du budget"),
                ("PinNumber", 1300, true, "Pin Number", "NUMÉRO DE NIP"),
                ("PartNumber", 1400, true, "Part Number", "numéro de la pièce"),
                ("HardwareDescription", 1500, true, "Hardware Description", "description du matériel"),
                ("HDWQty", 1600, true, "HDW Qty", "QUANTITÉ DE MATÉRIEL"),
                ("AccParts", 1700, true, "Acc Parts", "pièce accessoire"),
                ("AccessoryDescription", 1800, true, "Accessory Description", "description d'accessoire"),
                ("AccQtys", 1900, true, "Acc Qtys", "QUANTITÉ D'ACCESSOIRE"),
                ("IMEIESN", 2000, true, "IMEI/ESN", "# SERIE"),
                ("ShipToProvince", 2100, true, "Ship To Province", "Expédier à la province"),
                ("HardwareCharge", 2150, false, "Hardware Charge", "Hardware Charge"),
                ("AccessoryCharge", 2160, false, "Accessory Charge", "Accessory Charge"),
                ("InvoiceNet", 2200, true, "Invoice Net", "Facture net"),
                ("VoicePlanDescription", 2300, true, "Voice Plan Description", "DESCRIPTION DU PLAN DE VOIX"),
                ("DataPlanDescription", 2400, true, "Data Plan Description", "DESCRIPTION DU PLAN DE DONNEES"),
                ("HSTGST", 2500, true, "HST/GST", "TVH-TPS"),
                ("PSTQST", 2600, true, "PST/QST", "PST-TVQ"),
                ("InvoiceTaxes", 2700, true, "Invoice Taxes", "FACTURE TAXES"),
                ("InvoiceShipping", 2800, true, "Invoice Shipping", "Frais de port facture"),
                ("InvoiceTotal", 2900, true, "Invoice Total", "TOTAL DE FACTURE"),
                ("ARStatus", 3000, false, "AR Status", "État de compte"),
                ("ShipToAddress", 3100, false, "Ship To Address", "Adresse de livraison"),
                ("ShipToStreetAddress", 3200, false, "Ship To Street Address", "Rue d’expédition"),
                ("ShipToCity", 3300, false, "Ship To City", "Ville d’expédition"),
                ("ShipToPostal", 3400, false, "Ship To Postal", "Code postal d’expédition"),
                ("GSTRate", 3500, false, "GST Rate", "GSTRate"),
                ("PSTRate", 3600, false, "PST Rate", "PSTRate"),
                ("GSTFlag", 3700, false, "GST Flag", "GSTFlag"),
                ("PSTFlag", 3800, false, "PST Flag", "PSTFlag"),
                ("Tax1Code", 3900, false, "Tax 1 Code", "Tax1Code"),
                ("Tax2Code", 4000, false, "Tax 2 Code", "Tax2Code"),
                ("BulkOrderID", 4100, false, "Bulk Order ID", "BulkOrderID"),
                ("SplitPaymentDetails", 4110, false, "Split Payment Details", "Split Payment Details")
            };

            return list.Select(df => new CustomerFieldBO
            {
                FieldName = df.Name,
                Label = includeFrench ? df.LabelFR : df.Label,
                Include = df.Include,
                Sequence = df.Sequence
            }).ToList();
        }

        public async Task<bool> UpdateCustomerFieldsAsync(string groupName, List<CustomerFieldBO> fields)
        {
            var existingFields = await _dbContext.tblCustomerColumns
                .Where(c => c.CustomerGroup == groupName)
                .ToListAsync();

            _dbContext.tblCustomerColumns.RemoveRange(existingFields);

            var newFields = fields.Select(f => new TblCustomerColumns
            {
                CustomerGroup = groupName,
                FieldName = f.FieldName,
                Label = f.Label,
                Include = f.Include,
                Sequence = f.Sequence,
                SummaryType = f.SummaryType,
                FormatString = f.FormatString,
                Level = f.Level
            });

            await _dbContext.tblCustomerColumns.AddRangeAsync(newFields);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> GenerateSunLifeReportAsync(CustomerSalesRequest request, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("SunLife");

                // Headers based on qrySunLife
                string[] headers = { "Supplier No", "Site Name", "PO No", "PO Line No", "Invoice No", "Invoice Line No", "Qty", "Unit Price", "Amount Including Tax", "Currency", "Invoice Date", "Type", "Natural Account", "Cost Centre", "Description", "SunLifeTaxCode", "CODE", "PROD_CODE", "ARStatus" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                // 1. Get header data from our local output table
                var headersData = await _dbContext.tblCustomerSalesOutput
                                  .Where(o => o.UserId == userId && o.CustGroup == request.CustGroup)
                                  .ToListAsync();

                if (!headersData.Any()) return package.GetAsByteArray();

                // 2. Fetch all line items for these invoices from Spire
                var invoiceNos = headersData.Select(h => h.Invoice).Where(i => !string.IsNullOrEmpty(i)).ToList();
                var detailsData = await FetchSalesHistoryDetailsFromSpire(invoiceNos);

                int rowIdx = 2;
                foreach (var h in headersData)
                {
                    // Find matching line items for this invoice
                    var invoiceLines = detailsData.Where(d => d.InvoiceNo == h.Invoice ).ToList();

                    int subLineNo = 1;
                    foreach (var line in invoiceLines)
                    {
                        worksheet.Cells[rowIdx, 1].Value = "222233";
                        worksheet.Cells[rowIdx, 2].Value = "BRAM 30 VICTORI";
                        worksheet.Cells[rowIdx, 3].Value = h.PONo;
                        worksheet.Cells[rowIdx, 4].Value = ""; // PO Line No (Usually empty or from original PO)
                        worksheet.Cells[rowIdx, 5].Value = h.Invoice;
                        worksheet.Cells[rowIdx, 6].Value = subLineNo++; // Renumbered line
                        worksheet.Cells[rowIdx, 7].Value = line.Qty;
                        worksheet.Cells[rowIdx, 8].Value = line.UnitPrice;

                        // Amount Including Tax calculation using header rates
                        decimal lineNet = (decimal)line.Qty * line.UnitPrice;
                        decimal tax1Rate = (h.GSTRate ?? 0) / 100m;
                        decimal tax2Rate = (h.PSTRate ?? 0) / 100m;
                        decimal lineTotal = lineNet * (1 + tax1Rate + tax2Rate);

                        worksheet.Cells[rowIdx, 9].Value = Math.Round(lineTotal, 2);
                        worksheet.Cells[rowIdx, 10].Value = "CAD";
                        worksheet.Cells[rowIdx, 11].Value = h.InvoiceDate?.ToString("yyyyMMdd");
                        worksheet.Cells[rowIdx, 12].Value = "ITEM";
                        worksheet.Cells[rowIdx, 13].Value = line.Misc1; // Natural Account from Inventory MISC_1
                        worksheet.Cells[rowIdx, 14].Value = h.CostBudgetCode;
                        worksheet.Cells[rowIdx, 15].Value = line.Description;
                        worksheet.Cells[rowIdx, 17].Value = line.PartNo;
                        worksheet.Cells[rowIdx, 19].Value = h.ARStatus;

                        rowIdx++;
                    }
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

        public async Task<byte[]> GenerateSplitPaymentReportAsync(CustomerSalesRequest request, string format, int userId)
        {
            var data = await _dbContext.tblCustomerSalesOutput
                .Where(o => o.UserId == userId && o.CustGroup == request.CustGroup)
                .ToListAsync();

            string[] headers = { "CustNo", "InvoiceDate", "Invoice", "Type", "InvoiceNet", "HST-GST", "PST-QST", "InvoiceTotal", "NetPayment", "HST-GST Payment", "PST-QST Payment", "TotalPayment", "NetBalance", "GST-HST Balance", "PST-QST Balance", "Payment Balance", "WebOrderID", "UserName", "PONo", "HardwareDescription", "ShipToProvince", "UserPayAmount", "UserPayMethod" };

            if (format.ToUpper() == "CSV")
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

                foreach (var o in data)
                {
                    decimal net = o.InvoiceNet ?? 0;
                    decimal hst = o.HSTGST ?? 0;
                    decimal pst = o.PSTQST ?? 0;
                    decimal userPay = o.UserPayAmount ?? 0;

                    decimal ratio = net != 0 ? (hst + pst) / net : 0;
                    decimal netPayment = Math.Round(userPay / (ratio + 1), 2);
                    decimal hstPayment = net != 0 ? Math.Round(netPayment * (hst / net), 2) : 0;
                    decimal pstPayment = net != 0 ? Math.Round(netPayment * (pst / net), 2) : 0;

                    var values = new List<object> {
                        o.CustNO, o.InvoiceDate, o.Invoice, "I", net, hst, pst, o.InvoiceTotal,
                        netPayment, hstPayment, pstPayment, userPay,
                        net - netPayment, hst - hstPayment, pst - pstPayment, (o.InvoiceTotal ?? 0) - userPay,
                        o.WebOrderID, o.UserName, o.PONo, o.HardwareDescription, o.ShipToProvince,
                        userPay, o.UserPayMethod
                    };
                    csv.AppendLine(string.Join(",", values.Select(v => $"\"{v?.ToString()?.Replace("\"", "\"\"")}\"")));
                }
                return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sales Detail");

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                var datas = await _dbContext.tblCustomerSalesOutput
                    .Where(o => o.UserId == userId && o.CustGroup == request.CustGroup)
                    .ToListAsync();

                int rowIdx = 2;
                foreach (var o in datas)
                {
                    decimal net = o.InvoiceNet ?? 0;
                    decimal hst = o.HSTGST ?? 0;
                    decimal pst = o.PSTQST ?? 0;
                    decimal userPay = o.UserPayAmount ?? 0;

                    decimal ratio = net != 0 ? (hst + pst) / net : 0;
                    decimal netPayment = Math.Round(userPay / (ratio + 1), 2);
                    decimal hstPayment = net != 0 ? Math.Round(netPayment * (hst / net), 2) : 0;
                    decimal pstPayment = net != 0 ? Math.Round(netPayment * (pst / net), 2) : 0;

                    worksheet.Cells[rowIdx, 1].Value = o.CustNO;
                    worksheet.Cells[rowIdx, 2].Value = o.InvoiceDate;
                    worksheet.Cells[rowIdx, 3].Value = o.Invoice;
                    worksheet.Cells[rowIdx, 4].Value = "I";
                    worksheet.Cells[rowIdx, 5].Value = net;
                    worksheet.Cells[rowIdx, 6].Value = hst;
                    worksheet.Cells[rowIdx, 7].Value = pst;
                    worksheet.Cells[rowIdx, 8].Value = o.InvoiceTotal;
                    worksheet.Cells[rowIdx, 9].Value = netPayment;
                    worksheet.Cells[rowIdx, 10].Value = hstPayment;
                    worksheet.Cells[rowIdx, 11].Value = pstPayment;
                    worksheet.Cells[rowIdx, 12].Value = userPay;
                    worksheet.Cells[rowIdx, 13].Value = net - netPayment;
                    worksheet.Cells[rowIdx, 14].Value = hst - hstPayment;
                    worksheet.Cells[rowIdx, 15].Value = pst - pstPayment;
                    worksheet.Cells[rowIdx, 16].Value = (o.InvoiceTotal ?? 0) - userPay;
                    worksheet.Cells[rowIdx, 17].Value = o.WebOrderID;
                    worksheet.Cells[rowIdx, 18].Value = o.UserName;
                    worksheet.Cells[rowIdx, 19].Value = o.PONo;
                    worksheet.Cells[rowIdx, 20].Value = o.HardwareDescription;
                    worksheet.Cells[rowIdx, 21].Value = o.ShipToProvince;
                    worksheet.Cells[rowIdx, 22].Value = userPay;
                    worksheet.Cells[rowIdx, 23].Value = o.UserPayMethod;
                    rowIdx++;
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }
        private string CleanPONo(string po)
        {
            if (string.IsNullOrEmpty(po)) return "";
            int idx = po.LastIndexOf(':');
            return idx != -1 ? po.Substring(idx + 1).Trim() : po.Trim();
        }

        private string ParseACCparts(string acc)
        {
            if (string.IsNullOrEmpty(acc)) return "";
            var parts = new List<string>();
            int idx = acc.IndexOf('/');
            while (idx != -1)
            {
                int colon = acc.IndexOf(':', idx + 1);
                if (colon != -1)
                {
                    parts.Add(acc.Substring(idx + 1, colon - idx - 1));
                    idx = acc.IndexOf('/', colon + 1);
                }
                else break;
            }
            return string.Join(",", parts);
        }

        private string ParseACCQtys(string acc)
        {
            if (string.IsNullOrEmpty(acc)) return "";
            var qtys = new List<string>();
            int colon = acc.IndexOf(':');
            while (colon != -1)
            {
                int nextSlash = acc.IndexOf('/', colon + 1);
                if (nextSlash == -1) nextSlash = acc.Length;
                qtys.Add(acc.Substring(colon + 1, nextSlash - colon - 1));
                colon = acc.IndexOf(':', nextSlash);
            }
            return string.Join(",", qtys);
        }

        private string GetProvince(string postal)
        {
            if (string.IsNullOrEmpty(postal)) return "";
            char first = postal[0];
            // Mocking dbo_postalprovince logic or using a standard mapping
            return first switch
            {
                'A' => "NL",
                'B' => "NS",
                'C' => "PE",
                'E' => "NB",
                'G' => "QC",
                'H' => "QC",
                'J' => "QC",
                'K' => "ON",
                'L' => "ON",
                'M' => "ON",
                'N' => "ON",
                'P' => "MB",
                'R' => "MB",
                'S' => "SK",
                'T' => "AB",
                'V' => "BC",
                'Y' => "YT",
                'X' => "NT",
                _ => ""
            };
        }

        private string GetTypeOfService(string type, string extended)
        {
            string ext = extended?.Trim() ?? "";
            string t = type?.Trim() ?? "";
            if (ext == "HUP") return "HUP";
            return $"{t} {ext}".Trim();
        }

        public async Task<bool> UpdateGeneratedDataAsync(List<CustomerSalesRow> data, int userId)
        {
            try
            {
                var existing = await _dbContext.tblCustomerSalesOutput
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                if (existing.Any())
                {
                    _dbContext.tblCustomerSalesOutput.RemoveRange(existing);
                }

                var newRecords = data.Select(row => new TblCustomerSalesOutput
                {
                    UserId = userId,
                    WebOrderID = row.WebOrderID,
                    Invoice = row.Invoice,
                    InvoiceDate = row.InvoiceDate,
                    VoicePlanDescription = row.VoicePlanDescription,
                    DataPlanDescription = row.DataPlanDescription,
                    CellPhoneNo = row.CellPhoneNo,
                    UserName = row.UserName,
                    PONo = row.PONo,
                    CostBudgetCode = row.CostBudgetCode,
                    PartNumber = row.PartNumber,
                    HardwareDescription = row.HardwareDescription,
                    HDWQty = row.HDWQty,
                    IMEIESN = row.IMEIESN,
                    AccParts = row.AccParts,
                    AccessoryDescription = row.AccessoryDescription,
                    AccQtys = row.AccQtys,
                    ShipToProvince = row.ShipToProvince,
                    InvoiceNet = row.InvoiceNet,
                    InvoiceShipping = row.InvoiceShipping,
                    InvoiceTaxes = row.InvoiceTaxes,
                    InvoiceTotal = row.InvoiceTotal,
                    CustGroup = row.CustGroup,
                    CustNO = row.CustNO,
                    TypeOfService = row.TypeOfService,
                    PinNumber = row.PinNumber,
                    HSTGST = row.HSTGST,
                    PSTQST = row.PSTQST,
                    MSDCode = row.MSDCode,
                    CustomerName = row.CustomerName,
                    Territory = row.Territory,
                    AccountCode = row.AccountCode,
                    AuthorizedDepartment = row.AuthorizedDepartment,
                    ShipToAddress = row.ShipToAddress,
                    ShipToStreetAddress = row.ShipToStreetAddress,
                    ShipToCity = row.ShipToCity,
                    ShipToPostal = row.ShipToPostal,
                    GSTRate = row.GSTRate,
                    PSTRate = row.PSTRate,
                    GSTFlag = row.GSTFlag,
                    PSTFlag = row.PSTFlag,
                    Tax1Code = row.Tax1Code,
                    Tax2Code = row.Tax2Code,
                    PortedCTN = row.PortedCTN,
                    BulkOrderID = row.BulkOrderID,
                    HardwareCharge = row.HardwareCharge,
                    AccessoryCharge = row.AccessoryCharge,
                    ARStatus = row.ARStatus,
                    UserPayAmount = row.UserPayAmount,
                    UserPayMethod = row.UserPayMethod,
                    Balance = row.Balance,
                    ModifiedBy = userId,
                    ModifiedDate = DateTime.Now
                }).ToList();

                await _dbContext.tblCustomerSalesOutput.AddRangeAsync(newRecords);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddCustomerToGroupAsync(string groupCode, BVCustomerBO customer, int userId)
        {
            // Get group name from another member of the same group
            var groupInfo = await _dbContext.tblCustomerGroups
                .Where(g => g.CustGroup == groupCode)
                .FirstOrDefaultAsync();

            var newMember = new TblCustomerGroups
            {
                CustGroup = groupCode,
                BVCustNo = customer.BVCustNo,
                GroupName = groupInfo?.GroupName ?? groupCode,
                BVName = customer.BVName,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            await _dbContext.tblCustomerGroups.AddAsync(newMember);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCustomerInGroupAsync(string groupCode, string oldCustNo, BVCustomerBO customer, int userId)
        {
            var existing = await _dbContext.tblCustomerGroups
                .FirstOrDefaultAsync(g => g.CustGroup == groupCode && g.BVCustNo == oldCustNo);

            if (existing == null) return false;

            existing.BVCustNo = customer.BVCustNo;
            existing.BVName = customer.BVName;
            existing.ModifiedBy = userId;
            existing.ModifiedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCustomerFromGroupAsync(string groupCode, string custNo, int userId)
        {
            try
            {
                var existing = await _dbContext.tblCustomerGroups
                    .FirstOrDefaultAsync(g => g.CustGroup == groupCode && g.BVCustNo == custNo);

                if (existing != null)
                {
                    _dbContext.tblCustomerGroups.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }
                return false; // Customer not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing customer from group: {ex.Message}");
                return false;
            }
        }
    }
}
