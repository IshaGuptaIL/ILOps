using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DAL.Inventory.IMEI.HardwareIMEI
{
    public class SpireClient : ISpireClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SpireClient> _logger;
        private readonly string _baseUrl;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _sqlConn;

        public SpireClient(
            HttpClient client,
            IConfiguration config,
            ILogger<SpireClient> logger)
        {
            _httpClient = client;
            _logger = logger;
            _sqlConn = config.GetConnectionString("bvactivation_Connection");

            var section = config.GetSection("SpireApi");

            _baseUrl = (section["BaseUrl"] ?? "").TrimEnd('/') + "/";
            _user = section["UserName"] ?? "";
            _pass = section["Password"] ?? "";

            if (string.IsNullOrWhiteSpace(_baseUrl))
                throw new InvalidOperationException("SpireApi:BaseUrl is missing.");

            if (string.IsNullOrWhiteSpace(_user) || string.IsNullOrWhiteSpace(_pass))
                throw new InvalidOperationException("SpireApi username/password missing.");
        }

        private async Task LogApiCallAsync(string callType, string endpoint, string sendString, string responseString, int httpStatus, string httpStatusText, long responseTime)
        {
            try
            {
                await using var conn = new SqlConnection(_sqlConn);
                await conn.OpenAsync();

                await using var checkCmd = new SqlCommand("SELECT TOP 1 ISNULL(LoggingEnabled, 0) FROM tblSettingsApi", conn);
                checkCmd.CommandTimeout = 600;
                var enabled = Convert.ToBoolean(await checkCmd.ExecuteScalarAsync());
                if (!enabled) return;

                await using var cmd = new SqlCommand(@"
                    INSERT INTO tblAPILog (CallType, Endpoint, SendString, ResponseString, HTTPStatus, HTTPStatusText, ResponseTime, LogDateTime)
                    VALUES (@CallType, @Endpoint, @SendString, @ResponseString, @HTTPStatus, @HTTPStatusText, @ResponseTime, @LogDateTime)", conn);
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@CallType", callType ?? "");
                cmd.Parameters.AddWithValue("@Endpoint", endpoint ?? "");
                cmd.Parameters.AddWithValue("@SendString", sendString ?? "");
                cmd.Parameters.AddWithValue("@ResponseString", responseString ?? "");
                cmd.Parameters.AddWithValue("@HTTPStatus", httpStatus);
                cmd.Parameters.AddWithValue("@HTTPStatusText", httpStatusText ?? "");
                cmd.Parameters.AddWithValue("@ResponseTime", responseTime);
                cmd.Parameters.AddWithValue("@LogDateTime", DateTime.Now);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error logging API call to tblAPILog: {Msg}", ex.Message);
            }
        }

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

        public async Task<string> GetPurchaseOrdersAsync()
        {
            var endpoint = $"{_baseUrl}purchasing/orders/?limit=10";
            using var request = CreateRequest(HttpMethod.Get, endpoint);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var respText = await response.Content.ReadAsStringAsync();

            await LogApiCallAsync("GET", endpoint, "", respText, (int)response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);

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

        public async Task<string> GetPurchaseOrderAsync(long id)
        {
            var endpoint = $"{_baseUrl}purchasing/orders/{id}";
            using var request = CreateRequest(HttpMethod.Get, endpoint);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var respText = await response.Content.ReadAsStringAsync();

            await LogApiCallAsync("GET", endpoint, "", respText, (int)response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);

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

        public async Task<bool> UpdatePurchaseOrderAsync(long id, string json)
        {
            var url = $"{_baseUrl}purchasing/orders/{id}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = CreateRequest(HttpMethod.Put, url, content);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync();

            await LogApiCallAsync("PUT", url, json, body, (int)response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);

            _logger.LogInformation("Spire PUT {Url} returned {StatusCode}", url, (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("UpdatePurchaseOrderAsync failed. Status: {Status}, Body: {Body}",
                    (int)response.StatusCode, body);
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<string> PostReceiptAsync(long id, string sendJson = "")
        {
            var url = $"{_baseUrl}purchasing/orders/{id}/receive";

            HttpContent? content = null;
            if (!string.IsNullOrWhiteSpace(sendJson))
                content = new StringContent(sendJson, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, url, content);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync();

            await LogApiCallAsync("POST", url, sendJson, body, (int)response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return null;

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
            }

            return null;
        }

        public Task<string> GetLastReceiptIdAsync(long orderId, string guid)
        {
            return Task.FromResult("LIVE-SYNC");
        }

        public async Task<string> GetSerialNumbersAsync(string whse, string partNo)
        {
            var filter = Uri.EscapeDataString($"{{\"whse\":{{\"eq\":\"{whse}\"}},\"partNo\":{{\"eq\":\"{partNo}\"}}}}");
            var url = $"{_baseUrl}inventory/serials?filter={filter}&limit=5000";
            using var request = CreateRequest(HttpMethod.Get, url);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync();

            await LogApiCallAsync("GET", url, "", body, (int)response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode) return "[]";
            return body;
        }
    }
}
