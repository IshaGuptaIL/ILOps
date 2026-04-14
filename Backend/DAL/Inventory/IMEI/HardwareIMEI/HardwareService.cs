using ClosedXML.Excel;
using DAL.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DAL.Inventory.IMEI.HardwareIMEI
{
    public class HardwareService : IHardwareService
    {
        private readonly ISpireClient _spireClient;
        private readonly AppDBContext _dbContext;
        private readonly ILogger<HardwareService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts =
           new()
           {
               PropertyNameCaseInsensitive = true,
               NumberHandling = JsonNumberHandling.AllowReadingFromString
           };
        public HardwareService(ISpireClient spireClient, AppDBContext dbContext, ILogger<HardwareService> logger)
        {
            _spireClient = spireClient;
            _dbContext = dbContext;
            _logger = logger;
        }


        private static int GetIntSafe(JsonElement el) =>
    el.ValueKind == JsonValueKind.Number
        ? el.GetInt32()
        : int.TryParse(el.GetString(), out var v) ? v : 0;

        private static long GetLongSafe(JsonElement el) =>
            el.ValueKind == JsonValueKind.Number
                ? el.GetInt64()
                : long.TryParse(el.GetString(), out var v) ? v : 0;

        private static decimal GetDecimalSafe(JsonElement el) =>
            el.ValueKind == JsonValueKind.Number
                ? el.GetDecimal()
                : decimal.TryParse(el.GetString(), out var v) ? v : 0;

        private static string GetStringSafe(JsonElement el) =>
            el.ValueKind == JsonValueKind.String
                ? el.GetString() ?? ""
                : el.GetRawText(); // handles numbers

        // ─────────────────────────────────────────────────────────────────────
        // GetPurchaseOrders — maps to qryPOItemSelect2
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<List<PurchaseOrderListItem>>> GetPurchaseOrdersAsync()
        {
            try
            {
                // Step 1: Get PO headers
                var json = await _spireClient.GetPurchaseOrdersAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var list = new List<PurchaseOrderListItem>();

                // Handle different possible response formats
                JsonElement ordersArray = default;
                if (root.TryGetProperty("records", out var rec) && rec.ValueKind == JsonValueKind.Array)
                    ordersArray = rec;
                else if (root.TryGetProperty("value", out var val) && val.ValueKind == JsonValueKind.Array)
                    ordersArray = val;
                else if (root.ValueKind == JsonValueKind.Array)
                    ordersArray = root;

                if (ordersArray.ValueKind != JsonValueKind.Array)
                    return new ApiResponse<List<PurchaseOrderListItem>> { Success = true, Data = list };

                // Step 2: Process each PO header
                foreach (var po in ordersArray.EnumerateArray())
                {
                    var status = po.TryGetProperty("status", out var st) ? GetStringSafe(st) : "";
                    if (status != "I" && status != "R") continue;

                    var poId = po.TryGetProperty("id", out var pid) ? GetLongSafe(pid) : 0;
                    var poNumber = po.TryGetProperty("number", out var pn) ? GetStringSafe(pn) : "";

                    var vendorNo = "";
                    if (po.TryGetProperty("vendor", out var vendorObj) && vendorObj.ValueKind == JsonValueKind.Object)
                        vendorNo = vendorObj.TryGetProperty("vendorNo", out var vn) ? GetStringSafe(vn) : "";

                    // Step 3: Try to get PO detail
                    try
                    {
                        var detailJson = await _spireClient.GetPurchaseOrderAsync(poId);
                        using var detailDoc = JsonDocument.Parse(detailJson);

                        if (detailDoc.RootElement.TryGetProperty("items", out var lines) && lines.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var line in lines.EnumerateArray())
                            {
                                var partNo = line.TryGetProperty("partNo", out var pt) ? GetStringSafe(pt) : "";
                                if (string.IsNullOrWhiteSpace(partNo)) continue;

                                list.Add(new PurchaseOrderListItem
                                {
                                    PurchaseOrderId = poId,
                                    PoNumber = poNumber,
                                    Vendor = vendorNo,
                                    Status = status,

                                    Id = line.TryGetProperty("id", out var lid) ? GetStringSafe(lid) : "0",
                                    Sequence = line.TryGetProperty("sequence", out var seq) ? GetIntSafe(seq) : 0,
                                    Whse = line.TryGetProperty("whse", out var wh) ? GetStringSafe(wh) : "",
                                    PartNo = partNo,
                                    Description = line.TryGetProperty("description", out var desc) ? GetStringSafe(desc) : "",
                                    Guid = line.TryGetProperty("guid", out var g) ? GetStringSafe(g) : "",
                                    OrderQty = line.TryGetProperty("orderQty", out var oq) ? GetDecimalSafe(oq) : 0,
                                    ReceivedQty = line.TryGetProperty("receiveQty", out var rq) ? GetDecimalSafe(rq) : 0,
                                    UnitCost = line.TryGetProperty("unitPrice", out var up) ? GetDecimalSafe(up) : 0,
                                    Location = line.TryGetProperty("whseLocation", out var loc) ? GetStringSafe(loc) : "",
                                });
                            }
                        }
                        else
                        {
                            // If no items, still add header-only entry
                            list.Add(new PurchaseOrderListItem
                            {
                                PurchaseOrderId = poId,
                                PoNumber = poNumber,
                                Vendor = vendorNo,
                                Status = status
                            });
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // Log warning and add header-only
                        _logger.LogWarning(ex, "Failed to get PO detail for PO {POId}. Adding header only.", poId);
                        list.Add(new PurchaseOrderListItem
                        {
                            PurchaseOrderId = poId,
                            PoNumber = poNumber,
                            Vendor = vendorNo,
                            Status = status
                        });
                    }
                }

                return new ApiResponse<List<PurchaseOrderListItem>>
                {
                    Success = true,
                    Data = list
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch POs from Spire");
                return new ApiResponse<List<PurchaseOrderListItem>>
                {
                    Success = false,
                    Message = "Spire API Error: " + ex.Message
                };
            }
        }
        // ─────────────────────────────────────────────────────────────────────
        // ParseExcel — cmdImportPackingSlip_Click / cmdImportScanList_Click
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<List<string>>> ParseExcelImeisAsync(Stream fileStream)
        {
            try
            {
                var imeis = new List<string>();
                using (var workbook = new XLWorkbook(fileStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed()?.RowsUsed();
                    if (rows != null)
                        foreach (var row in rows)
                        {
                            var val = row.Cell(1).Value.ToString().Trim().ToUpper();
                            if (!string.IsNullOrEmpty(val)) imeis.Add(val);
                        }
                }
                return await Task.FromResult(new ApiResponse<List<string>> { Success = true, Data = imeis });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel parsing failed");
                return await Task.FromResult(new ApiResponse<List<string>> { Success = false, Message = "Failed to parse Excel: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CheckErrors — maps to CheckErrors() Sub in frmReceive
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<CheckErrorsResponse>> CheckErrorsAsync(CheckErrorsRequest request)
        {
            var response = new CheckErrorsResponse
            {
                PackingSlipCount = request.PackingSlipImeis.Length,
                ScanListCount = request.ScanListImeis.Length
            };

            try
            {
                // ── GUARD: Must have packing slip and scan list ───────────────
                if (request.PackingSlipImeis.Length == 0)
                {
                    response.Errors.Add("You must import packing slip data.");
                    return Ok(response);
                }
                if (request.ScanListImeis.Length == 0)
                {
                    response.Errors.Add("You must import scan list data.");
                    return Ok(response);
                }

                // ── 1. IMEI FORMAT VALIDATION ─────────────────────────────────
                // Legacy: VerifySerial() — 15 digit numeric (default)
                var imeiRegex = new Regex(@"^\d{15}$");

                var invalidScan = request.ScanListImeis.Where(i => !imeiRegex.IsMatch(i)).ToList();
                var invalidPack = request.PackingSlipImeis.Where(i => !imeiRegex.IsMatch(i)).ToList();

                response.InvalidScanImeis = invalidScan;
                response.InvalidPackImeis = invalidPack;
                response.InvalidScanCount = invalidScan.Count;
                response.InvalidPackCount = invalidPack.Count;

                if (invalidScan.Count > 0)
                    response.Errors.Add($"There are {invalidScan.Count} invalid entries on the Scan List");
                if (invalidPack.Count > 0)
                    response.Errors.Add($"There are {invalidPack.Count} invalid entries on the Packing Slip");

                // ── 2. DUPLICATES ─────────────────────────────────────────────
                var psDups = request.PackingSlipImeis.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                var slDups = request.ScanListImeis.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

                response.PackDupeCount = psDups.Count;
                response.ScanDupeCount = slDups.Count;

                if (slDups.Count > 0) response.Errors.Add("There are duplicates in the Scan List");
                if (psDups.Count > 0) response.Errors.Add("There are duplicates on the Packing Slip");

                // ── 3. CROSS-LIST COMPARISON ──────────────────────────────────
                var psSet = new HashSet<string>(request.PackingSlipImeis);
                var slSet = new HashSet<string>(request.ScanListImeis);

                response.Matches = slSet.Intersect(psSet).ToList();
                response.ScanNoPack = slSet.Except(psSet).ToList();
                response.PackNoScan = psSet.Except(slSet).ToList();

                if (response.ScanNoPack.Count > 0)
                    response.Errors.Add("There are entries on the Scan List that are not on the Packing Slip");
                if (response.PackNoScan.Count > 0)
                    response.Errors.Add("There are entries on the Packing Slip that are not on the Scan List");

                // ── 4. QUANTITY CHECK ─────────────────────────────────────────
                // Receipt mode:  OrdQty - RcvdQty >= ScanListCount
                // Reversal mode: RcvdQty >= ScanListCount
                var scanCount = request.ScanListImeis.Length;
                if (!request.IsReversal)
                {
                    var remaining = request.OrderQty - request.ReceivedQty;
                    if (scanCount > (double)remaining)
                        response.Errors.Add($"Quantity to receive ({scanCount}) is greater than quantity remaining on PO ({remaining})");
                }
                else
                {
                    if (scanCount > (double)request.ReceivedQty)
                        response.Errors.Add($"Quantity to receive ({scanCount}) is greater than quantity already received on PO ({request.ReceivedQty})");
                }

                // ── 5. ALREADY IN SPIRE CHECK ─────────────────────────────────
                // Maps to: SELECT from wwserialtemp WHERE IsAllocated=0 (receipt)
                //          or IsAllocated=1 (reversal)
                if (!string.IsNullOrWhiteSpace(request.Whse) && request.ScanListImeis.Length > 0)
                {
                    try
                    {
                        // Get PO detail to know the part number
                        var poJson = await _spireClient.GetPurchaseOrderAsync(request.PurchaseOrderId);
                        if (!string.IsNullOrEmpty(poJson))
                        {
                            var po = JsonSerializer.Deserialize<PurchaseOrder>(poJson, _jsonOpts);
                            var lineItem = po?.Items.FirstOrDefault(i => i.Id == request.PurchaseOrderLineId);
                            if (lineItem != null && !string.IsNullOrEmpty(lineItem.PartNo))
                            {
                                var serialJson = await _spireClient.GetSerialNumbersAsync(request.Whse, lineItem.PartNo);
                                using var sDoc = JsonDocument.Parse(serialJson);
                                var sRoot = sDoc.RootElement;

                                JsonElement serialsArray = default;
                                if (sRoot.TryGetProperty("records", out var srec) && srec.ValueKind == JsonValueKind.Array)
                                    serialsArray = srec;
                                else if (sRoot.TryGetProperty("value", out var sval) && sval.ValueKind == JsonValueKind.Array)
                                    serialsArray = sval;
                                else if (sRoot.ValueKind == JsonValueKind.Array)
                                    serialsArray = sRoot;

                                if (serialsArray.ValueKind == JsonValueKind.Array)
                                {
                                    // Build set of IMEIs that are onhand and NOT allocated (free stock)
                                    var onhandFreeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                    foreach (var s in serialsArray.EnumerateArray())
                                    {
                                        var num = s.TryGetProperty("number", out var n) ? n.GetString() ?? "" : "";
                                        var onhand = s.TryGetProperty("onhandQty", out var oq) ? oq.GetDecimal() : 0;
                                        var temp = s.TryGetProperty("tempQty", out var tq) ? tq.GetDecimal() : 0;
                                        var committed = s.TryGetProperty("committedQty", out var cq) ? cq.GetDecimal() : 0;

                                        // IsAllocated = 0 means free/onhand (available)
                                        bool isAllocated = onhand == 0 || temp != 0 || committed != 0;
                                        if (!isAllocated) onhandFreeSet.Add(num);
                                    }

                                    if (!request.IsReversal)
                                    {
                                        // Receipt mode: scanned IMEI must NOT already be onhand
                                        response.AlreadyInInventory = request.ScanListImeis
                                            .Where(imei => onhandFreeSet.Contains(imei))
                                            .ToList();
                                        if (response.AlreadyInInventory.Count > 0)
                                            response.Errors.Add("There are entries in the Scan List that are already onhand in Spire");
                                    }
                                    else
                                    {
                                        // Reversal mode: scanned IMEI should be onhand (to reverse it)
                                        var notOnhand = request.ScanListImeis
                                            .Where(imei => !onhandFreeSet.Contains(imei))
                                            .ToList();
                                        response.AlreadyInInventory = notOnhand; // reusing field for "not in Spire"
                                        if (notOnhand.Count > 0)
                                            response.Errors.Add("There are entries in the Scan List that are not onhand in Spire");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not fetch serials from Spire for IMEI check — skipping");
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckErrors failed");
                return new ApiResponse<CheckErrorsResponse> { Success = false, Message = ex.Message };
            }
        }

        public class DecimalStringConverter : JsonConverter<decimal>
        {
            public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                    return reader.GetDecimal();
                if (reader.TokenType == JsonTokenType.String)
                    return decimal.Parse(reader.GetString() ?? "0", CultureInfo.InvariantCulture);

                throw new JsonException($"Unexpected token parsing decimal: {reader.TokenType}");
            }

            public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ReceiveImei — cmdPostReceipts_Click → ReceivePOIMEI() VBA function
        // ─────────────────────────────────────────────────────────────────────
        public async Task<ApiResponse<string>> ReceiveImeiAsync(ReceiveImeiRequest request)
        {
            try
            {
                // ── GUARD: CMO must be provided ───────────────────────────────
                if (string.IsNullOrWhiteSpace(request.CmoNumber))
                    return new ApiResponse<string> { Success = false, Message = "You must enter a CMO number" };

                // ── GUARD: Scan list must not be empty ────────────────────────
                if (request.Imeis.Length == 0)
                    return new ApiResponse<string> { Success = false, Message = "There are no IMEIs in the Scan List" };

                // ── STEP 1: GET the PO (matches VBA line 175) ─────────────────
                var poJson = await _spireClient.GetPurchaseOrderAsync(request.PurchaseOrderId);
                if (string.IsNullOrEmpty(poJson))
                    return new ApiResponse<string> { Success = false, Message = "PO not found in Spire" };

                var po = JsonSerializer.Deserialize<PurchaseOrder>(poJson, _jsonOpts);
                var lineItem = po?.Items.FirstOrDefault(i => i.Id == request.PurchaseOrderLineId);
                if (lineItem == null)
                    return new ApiResponse<string> { Success = false, Message = "PO line item not found" };

                lineItem.Serials ??= new List<SerialNumber>();

                // ── STEP 2: Add/remove serials on the line item ──
                int count = 0;
                foreach (var imei in request.Imeis)
                {
                    lineItem.Serials.Add(new SerialNumber
                    {
                        Number = imei,
                        Whse = lineItem.Whse,
                        PartNo = lineItem.PartNo,
                        CommittedQty = request.IsReversal ? -1 : 1
                    });
                    count++;
                }

                // Update receiveQty
                if (request.IsReversal)
                    lineItem.ReceiveQty -= count;
                else
                    lineItem.ReceiveQty += count;

                // ── STEP 3: Prepare safe DTO for Spire ──
                var updateDto = new
                {
                    items = po.Items.Select(i => new UpdatePoLineDto
                    {
                        Id = i.Id,
                        ReceiveQty = (int)i.ReceiveQty,
                        Serials = i.Serials
                    }).ToList()
                };

                var updatedJson = JsonSerializer.Serialize(updateDto, _jsonOpts);
                var putOk = await _spireClient.UpdatePurchaseOrderAsync(request.PurchaseOrderId, updatedJson);
                if (!putOk)
                    return new ApiResponse<string> { Success = false, Message = "Failed to update PO in Spire (PUT)" };

                // ── STEP 4: POST /receive to finalise ──
                string receiptNo = null;

                if (request.PostReceipt)
                {
                    receiptNo = await _spireClient.PostReceiptAsync(request.PurchaseOrderId, "");

                    if (receiptNo == null)
                        return new ApiResponse<string> { Success = false, Message = "Failed to POST receipt to Spire" };
                }

                // ── STEP 5: Log to Local EF Database ──
                var receiptDate = DateTime.Now;
                var vendor = po?.VendorNo ?? "Unknown";
                var part = lineItem.PartNo ?? "Unknown";
                var unitCost = lineItem.UnitPrice;
                var poNum = po?.OrderNo ?? "Unknown";
                var bvReceiptNo = receiptNo;

                foreach (var imei in request.Imeis)
                {
                    _dbContext.hardwarereceived.Add(new hardwarereceived
                    {
                        Vendor = vendor,
                        BVReceiptNo = bvReceiptNo,
                        BVReceiptDate = receiptDate,
                        CMO = request.CmoNumber,
                        PO = poNum,
                        Part = part,
                        Qty = request.IsReversal ? -1 : 1,
                        ReceiptUnitCost = (double)unitCost,
                        IMEI = imei,
                        ItemType = "HDW"
                    });
                }
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Processed {Count} IMEIs for PO {POId} (Reversal={Rev}) and saved to Local DB.", count, request.PurchaseOrderId, request.IsReversal);

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "OK",
                    Data = $"Processed {count} IMEIs."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReceiveImei failed");
                return new ApiResponse<string> { Success = false, Message = ex.Message };
            }
        }

        // helper
        private static ApiResponse<CheckErrorsResponse> Ok(CheckErrorsResponse r) =>
            new() { Success = true, Message = r.HasErrors ? "Errors found" : "OK", Data = r };
    }
}


