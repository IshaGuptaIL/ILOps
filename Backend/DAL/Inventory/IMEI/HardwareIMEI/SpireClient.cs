using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DAL.Inventory.IMEI.HardwareIMEI
{
    public class SpireClient: ISpireClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SpireClient> _logger;
        private readonly string _baseUrl;
        private readonly string _user;
        private readonly string _pass;

        public SpireClient(
            HttpClient client,
            IConfiguration config,
            ILogger<SpireClient> logger)
        {
            _httpClient = client;
            _logger = logger;

            var section = config.GetSection("SpireApi");

            _baseUrl = (section["BaseUrl"] ?? "").TrimEnd('/') + "/";
            _user = section["UserName"] ?? "";
            _pass = section["Password"] ?? "";

            if (string.IsNullOrWhiteSpace(_baseUrl))
                throw new InvalidOperationException("SpireApi:BaseUrl is missing.");

            if (string.IsNullOrWhiteSpace(_user) || string.IsNullOrWhiteSpace(_pass))
                throw new InvalidOperationException("SpireApi username/password missing.");
        }

        public async Task<string> GetPurchaseOrdersAsync()
        {
            var endpoint = $"{_baseUrl}purchasing/orders/?limit=10";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var authBytes = Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var respText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Spire PO API → {Url} returned {StatusCode} in {Time}ms",
                endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Spire PO API failed. Status: {Status}, Reason: {Reason}, Body: {Body}",
                    (int)response.StatusCode, response.ReasonPhrase, respText);

                throw new HttpRequestException(
                    $"Spire PO API failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={respText}");
            }

            return respText;
        }




        /// <summary>
        /// GET single PO by ID — used before PUT/receive to load serials
        /// Matches: ret = CallSpire(0, 0, "GET", "purchasing/orders", lngPOID, ...)
        /// </summary>
        public async Task<string> GetPurchaseOrderAsync(long id)
        {
            var endpoint = $"{_baseUrl}purchasing/orders/{id}";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            var authBytes = Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var respText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Spire PO Detail API → {Url} returned {StatusCode} in {Time}ms",
                endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Spire PO Detail API failed. Status: {Status}, Reason: {Reason}, Body: {Body}",
                    (int)response.StatusCode, response.ReasonPhrase, respText);

                throw new HttpRequestException(
                    $"Spire PO Detail API failed. Status={(int)response.StatusCode} {response.ReasonPhrase}. Body={respText}");
            }

            return respText;
        }

        /// <summary>
        /// PUT PO — update serials and receiveQty on the line item
        /// Matches: ret = CallSpire(0, 0, "PUT", "purchasing/orders", lngPOID, SendString, ...)
        /// </summary>
        /// 
        private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url);

            var authBytes = Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (content != null)
                request.Content = content;

            return request;
        }

        public async Task<bool> UpdatePurchaseOrderAsync(long id, string json)
        {
            var url = $"{_baseUrl}purchasing/orders/{id}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = CreateRequest(HttpMethod.Put, url, content);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Spire PUT {Url} returned {StatusCode}", url, (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("UpdatePurchaseOrderAsync failed. Status: {Status}, Body: {Body}",
                    (int)response.StatusCode, body);
            }

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// POST receive — finalises the receipt in Spire
        /// Matches: ret = CallSpire(0, 0, "POST", "purchasing/orders/{id}/receive", ...)
        /// </summary>
        public async Task<string> PostReceiptAsync(long id, string sendJson = "")
        {
            var url = $"{_baseUrl}purchasing/orders/{id}/receive";

            HttpContent? content = null;
            if (!string.IsNullOrWhiteSpace(sendJson))
                content = new StringContent(sendJson, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, url, content);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("number", out var receiptId))
                {
                    return receiptId.GetString();
                }
            }
            catch (JsonException)
            {
                // handle parse error if needed
            }

            return null;
        }

        /// <summary>
        /// GET receipt after posting — matches the post-receive query on public_purchase_receipts
        /// </summary>
        public Task<string> GetLastReceiptIdAsync(long orderId, string guid)
        {
            // In live mode the receipt ID comes from Spire's Location header
            // after POST /receive. Keeping as placeholder for correlation logging.
            return Task.FromResult("LIVE-SYNC");
        }

        // ─────────────────────────────────────────────────────────────────
        // Inventory Serial Numbers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// GET serial numbers for a specific whse + partNo.
        /// Maps to: INSERT INTO wwserialtemp FROM public_inventory_serial_numbers
        /// WHERE whse='CO' AND part_no='...'
        /// Used to check if scanned IMEIs already exist (AlreadyInInventory logic).
        /// </summary>
        public async Task<string> GetSerialNumbersAsync(string whse, string partNo)
        {
            // Spire filter format: ?filter={"whse":{"eq":"CO"},"partNo":{"eq":"IPHONE16"}}
            var filter = Uri.EscapeDataString($"{{\"whse\":{{\"eq\":\"{whse}\"}},\"partNo\":{{\"eq\":\"{partNo}\"}}}}");
            var url = $"{_baseUrl}inventory/serials?filter={filter}&limit=5000";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return "[]";
            return await response.Content.ReadAsStringAsync();
        }
    }
}

