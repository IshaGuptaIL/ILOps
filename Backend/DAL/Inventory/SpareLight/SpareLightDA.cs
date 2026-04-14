using DAL.Common.Login;
using DAL.Common.Spire;

using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace DAL.Inventory.SpareLight.DA
{
    public class SpareLightDA : ISpareLight
    {
        private readonly AppDBContext _dbContext;
        private readonly SpireDA _spire;
        private readonly string _pgConnString;
        private readonly string _baseUrl;
        private readonly string _user;
        private readonly string _pass;
        private readonly HttpClient _httpClient;

        public SpareLightDA(AppDBContext context, SpireDA spire, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _dbContext = context;
            _spire = spire;
            _pgConnString = spire.PgConnString;

            var section = config.GetSection("SpireApi");
            _baseUrl = (section["BaseUrl"] ?? "").TrimEnd('/') + "/";
            _user = section["UserName"] ?? "";
            _pass = section["Password"] ?? "";

            _httpClient = httpClientFactory.CreateClient("SpireClient");
        }

        public async Task<List<HardwareTransferBO>> ParseHardwareExcelAsync(System.IO.Stream fileStream)
        {
            var items = new List<HardwareTransferBO>();
            using var package = new OfficeOpenXml.ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // Map Headers to Indices
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(header)) headerMap[header] = col;
            }

            // VBA: DELETE * from tblTransferList
            var existingItems = await _dbContext.tblTransferList.ToListAsync();
            _dbContext.tblTransferList.RemoveRange(existingItems);

            for (int row = 2; row <= rowCount; row++)
            {
                var fromWhse = GetValue(worksheet, row, headerMap, "WarehouseCodeTransferFrom");
                var toWhse = GetValue(worksheet, row, headerMap, "WarehouseCodeTransferTo");
                var partNo = GetValue(worksheet, row, headerMap, "PartNo");
                var imei = GetValue(worksheet, row, headerMap, "IMEI");
                var simPartNo = GetValue(worksheet, row, headerMap, "SimPartNo");
                var pin = GetValue(worksheet, row, headerMap, "Pin");
                var simNo = GetValue(worksheet, row, headerMap, "SimNo");

                if (string.IsNullOrEmpty(fromWhse) && string.IsNullOrEmpty(partNo) && string.IsNullOrEmpty(imei))
                    continue;

                var bo = new HardwareTransferBO
                {
                    WarehouseCodeTransferFrom = fromWhse.ToUpper(),
                    WarehouseCodeTransferTo = toWhse.ToUpper(),
                    PartNo = partNo.ToUpper(),
                    IMEI = imei.ToUpper(),
                    SimPartNo = simPartNo.ToUpper(),
                    SimNo = simNo.ToUpper(),
                    Pin = pin.ToUpper(),
                    RowNumber = row
                };
                items.Add(bo);

                // Save to Entity
                var entity = new tblTransferList
                {
                    WarehouseCodeTransferFrom = bo.WarehouseCodeTransferFrom,
                    WarehouseCodeTransferTo = bo.WarehouseCodeTransferTo,
                    PartNo = bo.PartNo,
                    IMEI = bo.IMEI,
                    SimPartNo = bo.SimPartNo,
                    SimNo = bo.SimNo,
                    Pin = bo.Pin,
                    RowNumber = bo.RowNumber
                };
                _dbContext.tblTransferList.Add(entity);
            }

            await _dbContext.SaveChangesAsync();
            return items;
        }

        public async Task<List<AccessoryTransferBO>> ParseAccessoryExcelAsync(System.IO.Stream fileStream)
        {
            var items = new List<AccessoryTransferBO>();
            using var package = new OfficeOpenXml.ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // Map Headers to Indices
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= colCount; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(header)) headerMap[header] = col;
            }

            // VBA: DELETE * from tblTransferListAcc
            var existingItems = await _dbContext.tblTransferListACC.ToListAsync();
            _dbContext.tblTransferListACC.RemoveRange(existingItems);

            for (int row = 2; row <= rowCount; row++)
            {
                var fromWhse = GetValue(worksheet, row, headerMap, "WarehouseCodeTransferFrom");
                var toWhse = GetValue(worksheet, row, headerMap, "WarehouseCodeTransferTo");
                var partNo = GetValue(worksheet, row, headerMap, "PartNo");
                var qtyStr = GetValue(worksheet, row, headerMap, "Quantity");

                if (string.IsNullOrEmpty(fromWhse) && string.IsNullOrEmpty(partNo))
                    continue;

                decimal.TryParse(qtyStr, out decimal qty);

                var bo = new AccessoryTransferBO
                {
                    WarehouseCodeTransferFrom = fromWhse.ToUpper(),
                    WarehouseCodeTransferTo = toWhse.ToUpper(),
                    PartNo = partNo.ToUpper(),
                    Quantity = qty,
                    RowNumber = row
                };
                items.Add(bo);

                // Save to Entity
                var entity = new tblTransferListACC
                {
                    WarehouseCodeTransferFrom = bo.WarehouseCodeTransferFrom,
                    WarehouseCodeTransferTo = bo.WarehouseCodeTransferTo,
                    PartNo = bo.PartNo,
                    Quantity = (int?)bo.Quantity,
                    RowNumber = bo.RowNumber
                };
                _dbContext.tblTransferListACC.Add(entity);
            }

            await _dbContext.SaveChangesAsync();
            return items;
        }

        private string GetValue(OfficeOpenXml.ExcelWorksheet ws, int row, Dictionary<string, int> map, string colName)
        {
            if (map.TryGetValue(colName, out int colIndex))
            {
                return ws.Cells[row, colIndex].Value?.ToString()?.Trim() ?? "";
            }
            return "";
        }

        public async Task<ApiResposne> ValidateHardwareTransferAsync()
        {
            var items = await _dbContext.tblTransferList.ToListAsync();
            int errorCount = 0;

            foreach (var item in items)
            {
                var sbErrors = new StringBuilder();

                // Field Presence Checks (VBA exact strings)
                if (string.IsNullOrWhiteSpace(item.WarehouseCodeTransferFrom)) sbErrors.Append("WarehouseCodeTransferFrom is not present.");
                if (string.IsNullOrWhiteSpace(item.WarehouseCodeTransferTo)) sbErrors.Append("WarehouseCodeTransferTo is not present.");
                if (string.IsNullOrWhiteSpace(item.PartNo)) sbErrors.Append("PartNo  is not present."); // Double space from VBA
                if (string.IsNullOrWhiteSpace(item.IMEI)) sbErrors.Append("IMEI is not present.");
                else if (item.IMEI.Length != 15) sbErrors.Append("IMEI is not 15 digits.");

                if (sbErrors.Length == 0)
                {
                    // Warehouse Checks
                    if (!await CheckWarehouseExistsAsync(item.WarehouseCodeTransferFrom))
                        sbErrors.Append("FROM Warehouse does not exist.");

                    if (!await CheckWarehouseExistsAsync(item.WarehouseCodeTransferTo))
                        sbErrors.Append("TO Warehouse does not exist.");

                    // Inventory Checks
                    if (!await _spire.PartExistsAsync(item.PartNo, item.WarehouseCodeTransferFrom))
                        sbErrors.Append("Item not found in FROM Warehouse.");

                    if (!string.IsNullOrWhiteSpace(item.SimPartNo))
                    {
                        if (!await _spire.PartExistsAsync(item.SimPartNo, item.WarehouseCodeTransferFrom))
                            sbErrors.Append("SimPartNo not found in FROM Warehouse.");
                    }

                    // Serial Number Checks
                    if (await CheckSerialOnhandAsync(item.WarehouseCodeTransferTo, item.PartNo, item.IMEI))
                        sbErrors.Append("IMEI already onhand in TO Warehouse.");

                    if (!await CheckSerialOnhandAsync(item.WarehouseCodeTransferFrom, item.PartNo, item.IMEI))
                        sbErrors.Append("IMEI not available in FROM Warehouse.");
                }

                item.ValidationResult = sbErrors.ToString().Trim();
                if (!string.IsNullOrEmpty(item.ValidationResult)) errorCount++;
            }

            // Duplicate Checks in File (VBA lines 2008-2060)
            var duplicateImeis = items.GroupBy(i => i.IMEI).Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1).Select(g => g.Key);
            foreach (var imei in duplicateImeis)
            {
                foreach (var di in items.Where(i => i.IMEI == imei))
                {
                    di.ValidationResult = (string.IsNullOrEmpty(di.ValidationResult) ? "" : di.ValidationResult + "; ") + "IMEI is duplicated.";
                    errorCount++;
                }
            }

            await _dbContext.SaveChangesAsync();
            return new ApiResposne { Success = true, Result = items, Count = errorCount };
        }

        public async Task<ApiResposne> ValidateAccessoryTransferAsync()
        {
            var items = await _dbContext.tblTransferListACC.ToListAsync();
            int errorCount = 0;

            foreach (var item in items)
            {
                var sbErrors = new StringBuilder();

                if (string.IsNullOrWhiteSpace(item.WarehouseCodeTransferFrom)) sbErrors.Append("WarehouseCodeTransferFrom is not present.");
                if (string.IsNullOrWhiteSpace(item.WarehouseCodeTransferTo)) sbErrors.Append("WarehouseCodeTransferTo is not present.");
                if (string.IsNullOrWhiteSpace(item.PartNo)) sbErrors.Append("PartNo is not present.");
                if (item.Quantity <= 0) sbErrors.Append("Quantity is less than zero.");

                if (sbErrors.Length == 0)
                {
                    if (!await CheckWarehouseExistsAsync(item.WarehouseCodeTransferFrom))
                        sbErrors.Append("FROM Warehouse does not exist.");

                    if (!await CheckWarehouseExistsAsync(item.WarehouseCodeTransferTo))
                        sbErrors.Append("TO Warehouse does not exist.");

                    if (!await _spire.PartExistsAsync(item.PartNo, item.WarehouseCodeTransferFrom))
                        sbErrors.Append("Item not found in FROM Warehouse.");
                }

                item.ValidationResult = sbErrors.ToString().Trim();
                if (!string.IsNullOrEmpty(item.ValidationResult)) errorCount++;
            }

            await _dbContext.SaveChangesAsync();
            return new ApiResposne { Success = true, Result = items, Count = errorCount };
        }

        private async Task<bool> CheckSerialOnhandAsync(string whse, string partNo, string serial)
        {
            await using var conn = new NpgsqlConnection(_pgConnString);
            // Replicating VBA SQL: committed_qty <> 0 OR temp_qty <> 0 OR onhand_qty <> 0
            string sql = @"
                SELECT 1 FROM inventory_serial_numbers 
                WHERE whse = @whse AND part_no = @partNo AND number = @serial 
                AND (committed_qty != 0 OR temp_qty != 0 OR onhand_qty != 0)
                LIMIT 1";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@whse", whse.ToUpper());
            cmd.Parameters.AddWithValue("@partNo", partNo.ToUpper());
            cmd.Parameters.AddWithValue("@serial", serial);

            await conn.OpenAsync();
            return await cmd.ExecuteScalarAsync() != null;
        }

        public async Task<ApiResposne> DoHardwareTransferAsync(DateTime transferDate)
        {
            var items = await _dbContext.tblTransferList
                .Where(i => string.IsNullOrEmpty(i.ValidationResult) && (i.TransferPosted == false || i.TransferPosted == null))
                .ToListAsync();

            if (items.Count == 0)
                return new ApiResposne { Success = false, Message = "No valid hardware items to transfer." };

            var groups = items.GroupBy(i => new { i.WarehouseCodeTransferFrom, i.WarehouseCodeTransferTo });
            int transferCount = 0;

            foreach (var group in groups)
            {
                string refNo = "SLT" + DateTime.Now.ToString("HHmmssf");

                var spireItems = new List<object>();

                // Add Hardware Items
                var partGroups = group.GroupBy(i => i.PartNo);
                foreach (var pg in partGroups)
                {
                    spireItems.Add(new
                    {
                        inventory = new { whse = group.Key.WarehouseCodeTransferFrom.ToUpper(), partNo = pg.Key.ToUpper() },
                        receiveQty = pg.Count(),
                        serials = pg.Select(s => new { serialNumber = s.IMEI, committedQty = 1 }).ToList()
                    });
                }

                // Add SIM Items 
                var simGroups = group.Where(i => !string.IsNullOrEmpty(i.SimPartNo))
                                     .GroupBy(i => i.SimPartNo);
                foreach (var sg in simGroups)
                {
                    spireItems.Add(new
                    {
                        inventory = new { whse = group.Key.WarehouseCodeTransferFrom.ToUpper(), partNo = sg.Key.ToUpper() },
                        receiveQty = sg.Count()
                    });
                }

                var payload = new
                {
                    sourceWhse = group.Key.WarehouseCodeTransferFrom.ToUpper(),
                    destinationWhse = group.Key.WarehouseCodeTransferTo.ToUpper(),
                    date = transferDate.ToString("yyyy-MM-dd"),
                    referenceNo = refNo,
                    items = spireItems
                };

                var json = JsonSerializer.Serialize(payload);
                var createResp = await SendSpireRequestAsync(HttpMethod.Post, "inventory/transfers/", json);

                if (!createResp.IsSuccessStatusCode)
                {
                    var errBody = await createResp.Content.ReadAsStringAsync();
                    return new ApiResposne
                    {
                        Success = false,
                        Message = $"Spire Validation fail for Hardware Transfer ({(int)createResp.StatusCode}): {errBody}"
                    };
                }

                // Get the Location header to post the transfer
                var location = createResp.Headers.Location?.ToString();
                if (string.IsNullOrWhiteSpace(location))
                    return new ApiResposne
                    {
                        Success = false,
                        Message = "Transfer Created but Location header missing; cannot post transfer."
                    };

                // Post the transfer to mark it as posted in Spire
                var postUrl = location.TrimEnd('/') + "/post"; // NO trailing slash
                var postResp = await SendSpireRequestAsync(HttpMethod.Post, postUrl, "{}"); // body required

                if (!postResp.IsSuccessStatusCode)
                {
                    var errBody = await postResp.Content.ReadAsStringAsync();
                    return new ApiResposne
                    {
                        Success = false,
                        Message = $"Transfer Created but Posting (Finalizing) Failed: {errBody}"
                    };
                }

                // If we reach here, transfer is successfully posted
                transferCount++;
                foreach (var item in group)
                {
                    item.TransferCreated = true;
                    item.TransferPosted = true;
                }
            }

            await _dbContext.SaveChangesAsync();
            return new ApiResposne
            {
                Success = true,
                Message = $"Transfer Complete. {transferCount} transfers processed and posted successfully."
            };
        }
        public async Task<ApiResposne> DoAccessoryTransferAsync(DateTime transferDate)
        {
            // Fetch all valid, unposted accessory items
            var items = await _dbContext.tblTransferListACC
                .Where(i => string.IsNullOrEmpty(i.ValidationResult) && (i.TransferPosted == false || i.TransferPosted == null))
                .ToListAsync();

            if (!items.Any())
                return new ApiResposne { Success = false, Message = "No valid accessory items to transfer." };

            // Group items by source and destination warehouse
            var groups = items.GroupBy(i => new { i.WarehouseCodeTransferFrom, i.WarehouseCodeTransferTo });
            int transferCount = 0;

            foreach (var group in groups)
            {
                string refNo = "SLT" + DateTime.Now.ToString("HHmmssf");

                // Build payload for transfer creation
                var payload = new
                {
                    sourceWhse = group.Key.WarehouseCodeTransferFrom.ToUpper(),
                    destinationWhse = group.Key.WarehouseCodeTransferTo.ToUpper(),
                    date = transferDate.ToString("yyyy-MM-dd"),
                    referenceNo = refNo,
                    items = group
                        .GroupBy(i => i.PartNo)
                        .Select(p => new
                        {
                            inventory = new { whse = group.Key.WarehouseCodeTransferFrom.ToUpper(), partNo = p.Key.ToUpper() },
                            receiveQty = p.Sum(x => x.Quantity)
                        })
                        .ToList()
                };

                var json = JsonSerializer.Serialize(payload);

                // Create transfer
                var createResp = await SendSpireRequestAsync(HttpMethod.Post, "inventory/transfers/", json);
                if (!createResp.IsSuccessStatusCode)
                {
                    var errBody = await createResp.Content.ReadAsStringAsync();
                    return new ApiResposne
                    {
                        Success = false,
                        Message = $"Spire Validation failed for Accessory Transfer ({(int)createResp.StatusCode}): {errBody}"
                    };
                }

                // Get Location header for posting
                var location = createResp.Headers.Location?.ToString();
                if (string.IsNullOrWhiteSpace(location))
                    return new ApiResposne
                    {
                        Success = false,
                        Message = "Transfer created but Location header missing, cannot post."
                    };

                // Commit/post transfer (body {} required by Spire)
                var postUrl = $"{location.TrimEnd('/')}/post";
                var postBody = JsonSerializer.Serialize(new { posted = true, transactionNo = (string?)null });

                var postResp = await SendSpireRequestAsync(HttpMethod.Post, postUrl, postBody);
                if (!postResp.IsSuccessStatusCode)
                {
                    var errBody = await postResp.Content.ReadAsStringAsync();
                    return new ApiResposne
                    {
                        Success = false,
                        Message = $"Accessory Transfer Created but Posting Failed: {errBody}"
                    };
                }

                // Update local DB
                foreach (var item in group)
                {
                    item.TransferCreated = true;
                    item.TransferPosted = true;
                }

                transferCount++;
            }

            await _dbContext.SaveChangesAsync();

            return new ApiResposne
            {
                Success = true,
                Message = $"Accessory Transfer Complete. {transferCount} transfers processed."
            };
        }


        public async Task<ApiResposne> GetTransferLogAsync(DateTime? startDate, DateTime? endDate, string? type)
        {
            var query = _dbContext.tblTransferLog.AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(x => x.TransferType == type);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.TransferDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.TransferDate <= endDate.Value);
            }

            var logs = await query.ToListAsync();

            return new ApiResposne
            {
                Success = true,
                Result = logs
            };
        }

        private async Task<bool> CheckWarehouseExistsAsync(string whse)
        {
            await using var conn = new NpgsqlConnection(_spire.PgConnString);
            string sql = "SELECT 1 FROM inventory_warehouses WHERE whse = @whse LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@whse", whse.ToUpper());
            await conn.OpenAsync();
            return await cmd.ExecuteScalarAsync() != null;
        }

        private async Task<HttpResponseMessage> SendSpireRequestAsync(HttpMethod method, string endpoint, string? jsonContent = null)
        {
            var baseUri = new Uri(_baseUrl);
            var requestUrl = endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? endpoint
                : (endpoint.StartsWith("/") && _baseUrl.EndsWith("/")
                    ? $"{baseUri.GetLeftPart(UriPartial.Authority)}{endpoint}"
                    : $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}");

            using var request = new HttpRequestMessage(method, requestUrl);

            var authBytes = Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(jsonContent))
            {
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var respText = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Spire API -> {method} {requestUrl} returned {(int)response.StatusCode} {response.ReasonPhrase} in {sw.ElapsedMilliseconds}ms");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Spire API Error Body: {respText}");
            }

            // Re-create the response to allow multiple reads of the content
            var clonedResponse = new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(respText, Encoding.UTF8, response.Content.Headers.ContentType?.MediaType ?? "application/json"),
                ReasonPhrase = response.ReasonPhrase,
                RequestMessage = response.RequestMessage,
                Version = response.Version
            };

            foreach (var header in response.Headers)
                clonedResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);

            foreach (var header in response.Content.Headers)
                clonedResponse.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clonedResponse;
        }
    }
}
