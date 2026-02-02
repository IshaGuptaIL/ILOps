using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ILOps_Inventory.Common.Spire
{
    public class SpireApiHelper
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _config;
        private readonly ILogger<SpireApiHelper> _logger;

        private readonly string _connString;
        private readonly string _pgConnString;
        private readonly string _baseUrl;
        private readonly string _user;
        private readonly string _pass;

        public SpireApiHelper(HttpClient client, IConfiguration config, ILogger<SpireApiHelper> logger)
        {
            _client = client;
            _config = config;
            _logger = logger;

            _connString = _config.GetConnectionString("bvactivation_Connection")
                ?? throw new InvalidOperationException("bvactivation_Connection missing");

            _pgConnString = _config.GetConnectionString("spire_Connection")
                ?? throw new InvalidOperationException("spire_Connection missing");

            _baseUrl = _config["SpireApi:BaseUrl"]
                ?? throw new InvalidOperationException("SpireApi:BaseUrl missing");

            if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
                throw new InvalidOperationException("Invalid Spire BaseUrl");

            _client.BaseAddress = baseUri;

            _user = _config["SpireApi:UserName"]
                ?? throw new InvalidOperationException("SpireApi:UserName missing");

            _pass = _config["SpireApi:Password"]
                ?? throw new InvalidOperationException("SpireApi:Password missing");
        }

        // =====================================================
        // PUBLIC METHOD USED BY CONTROLLER
        // =====================================================
        public async Task<(bool Success, string ApiResponseText, SpireResponse SpireResponse)>
            CallSpireAsync(HttpMethod method, string endpoint, long keyValue, string? sendJson, string? parameters = null)
        {
            var sb = new StringBuilder();
            sb.Append(endpoint.TrimStart('/'));

            if (keyValue != 0)
                sb.Append('/').Append(keyValue);

            if (!string.IsNullOrWhiteSpace(parameters))
                sb.Append(parameters);

            var relativeUrl = sb.ToString();
            _logger.LogInformation("Spire call → {Url}", relativeUrl);

            // ---- BASIC AUTH (same as Postman)
            var authBytes = Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            var authBase64 = Convert.ToBase64String(authBytes);

            using var request = new HttpRequestMessage(method, relativeUrl);

            // 🔥 CRITICAL: force HTTP/1.1
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", authBase64);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(sendJson) &&
                (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
            {
                request.Content = new StringContent(sendJson, Encoding.UTF8, "application/json");
            }

            var sw = Stopwatch.StartNew();
            
            using var response = await _client.SendAsync(request).ConfigureAwait(false);
            sw.Stop();

            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var spireResp = new SpireResponse
            {
                HttpStatus = (int)response.StatusCode,
                HttpStatusText = response.ReasonPhrase,
                HeaderResponse = $"{response.Headers}{Environment.NewLine}{response.Content.Headers}",
                HeaderLocation = response.Headers.Location?.ToString(),
                HeaderContentLength = response.Content.Headers.ContentLength ?? 0,
                ResponseTime = sw.ElapsedMilliseconds,
                Allow = response.Content.Headers.Allow?.ToString()
            };

            // Parse key from Location header
            if (!string.IsNullOrEmpty(spireResp.HeaderLocation))
            {
                var idx = spireResp.HeaderLocation.LastIndexOf('/');
                if (idx > -1 &&
                    long.TryParse(spireResp.HeaderLocation[(idx + 1)..], out var key))
                {
                    spireResp.HeaderKey = key;
                }
            }

            _logger.LogInformation(
                "Spire Result → {Status} {Reason} ({Time}ms)",
                spireResp.HttpStatus,
                spireResp.HttpStatusText,
                spireResp.ResponseTime);

            await LogApiToTblAPILogAsync(
                serverID:0,
    companyId: 0,
    callType: "SpireAPI",
    endPoint: relativeUrl,
    keyValue: keyValue.ToString(),
    parameters: parameters,
    responseString: responseText,
    spireResp: spireResp);


            return (response.IsSuccessStatusCode, responseText, spireResp);
        }

        // =====================================================
        // DEBUG (USED BY CONTROLLER)
        // =====================================================
        public void DebugSpireResponse(SpireResponse response, string json, string? sendJson = null, bool prettify = true)
        {
            if (!string.IsNullOrWhiteSpace(sendJson))
                _logger.LogInformation("SENT:\n{Json}", prettify ? PrettyJson(sendJson) : sendJson);

            _logger.LogInformation("RESPONSE:\n{Json}", prettify ? PrettyJson(json) : json);

            _logger.LogInformation(
                "STATUS={Status} TIME={Time}ms LOCATION={Location}",
                response.HttpStatus,
                response.ResponseTime,
                response.HeaderLocation);
        }

        private static string PrettyJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return json;
            }
        }

        // =====================================================
        // API LOGGING
        // =====================================================

        public async Task LogApiToTblAPILogAsync(int serverID,int companyId, string callType, string endPoint,
    string keyValue, string parameters, string responseString, SpireResponse spireResp)
        {
            try
            {
                using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(@"
INSERT INTO tblAPILog (
ServerID,
    CompanyID, CallType, EndPoint, KeyValue, Parameters, ResponseString, 
    CallDateTime, ResponseTime, FullURLPassed, FullURLUsed, 
    HTTPStatus, HTTPStatusText, HeaderResponseLocation, HeaderResponseKey
) VALUES (
    @CompanyID, @CallType, @EndPoint, @KeyValue, @Parameters, @ResponseString,
    SYSDATETIME(), @ResponseTime, @FullURLPassed, @FullURLUsed,
    @HTTPStatus, @HTTPStatusText, @HeaderResponseLocation, @HeaderResponseKey)", conn);
                cmd.Parameters.AddWithValue("@ServerID", serverID);

                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@CallType", callType);
                cmd.Parameters.AddWithValue("@EndPoint", endPoint ?? "");
                cmd.Parameters.AddWithValue("@KeyValue", keyValue ?? "");

                // ✅ SAFE: Long text truncate
                cmd.Parameters.AddWithValue("@Parameters", parameters?.Length > 4000 ? parameters.Substring(0, 4000) : parameters ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ResponseString", responseString?.Length > 4000 ? responseString.Substring(0, 4000) : responseString);
                cmd.Parameters.AddWithValue("@ResponseTime", spireResp.ResponseTime);
                cmd.Parameters.AddWithValue("@FullURLPassed", (_client.BaseAddress + endPoint)?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@FullURLUsed", spireResp.HeaderLocation ?? (_client.BaseAddress + endPoint)?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@HTTPStatus", spireResp.HttpStatus);
                cmd.Parameters.AddWithValue("@HTTPStatusText", spireResp.HttpStatusText ?? "");
                cmd.Parameters.AddWithValue("@HeaderResponseLocation", spireResp.HeaderLocation ?? (object)DBNull.Value);

                // 🔥 FIXED: HeaderResponseKey safe parsing
                cmd.Parameters.AddWithValue("@HeaderResponseKey",
                    spireResp.HeaderKey.HasValue ? (object)spireResp.HeaderKey.Value : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("✅ API Log saved: {EndPoint}", endPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogApiToTblAPILogAsync failed for {EndPoint}", endPoint);
            }
        }




        // =====================================================
        // POSTGRES SAVE (USED BY CONTROLLER)
        // =====================================================
        public async Task<bool> SaveInventoryToPostgresAsync(string jsonData)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_pgConnString);
                await conn.OpenAsync();

                using var doc = JsonDocument.Parse(jsonData);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    using var cmd = new NpgsqlCommand(@"
INSERT INTO public.inventory
(whse, part_no, description, current_cost, average_cost,
 sales_dept, serialized, user_def1, allow_backorders, created_at, updated_at)
VALUES
(@whse, @part_no, @description, @current_cost, @average_cost,
 @sales_dept, @serialized, @user_def1, @allow_backorders, NOW(), NOW())
ON CONFLICT (whse, part_no)
DO UPDATE SET
 description = EXCLUDED.description,
 current_cost = EXCLUDED.current_cost,
 average_cost = EXCLUDED.average_cost,
 sales_dept = EXCLUDED.sales_dept,
 serialized = EXCLUDED.serialized,
 user_def1 = EXCLUDED.user_def1,
 allow_backorders = EXCLUDED.allow_backorders,
 updated_at = NOW();", conn);

                    cmd.Parameters.AddWithValue("@whse", el.GetProperty("whse").GetString()!);
                    cmd.Parameters.AddWithValue("@part_no", el.GetProperty("partNo").GetString()!);
                    cmd.Parameters.AddWithValue("@description", el.GetProperty("description").GetString()!);
                    cmd.Parameters.AddWithValue("@current_cost", el.GetProperty("currentCost").GetDecimal());
                    cmd.Parameters.AddWithValue("@average_cost", el.GetProperty("averageCost").GetDecimal());
                    cmd.Parameters.AddWithValue("@sales_dept", el.GetProperty("salesDept").GetInt32());
                    cmd.Parameters.AddWithValue("@serialized", el.GetProperty("serialized").GetBoolean());
                    cmd.Parameters.AddWithValue("@user_def1",
                        el.TryGetProperty("userDef1", out var u) ? u.GetString() ?? (object)DBNull.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@allow_backorders", el.GetProperty("allowBackorders").GetBoolean());

                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveInventoryToPostgresAsync failed");
                return false;
            }
        }
    }
}
