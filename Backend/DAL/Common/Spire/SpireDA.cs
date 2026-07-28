using Npgsql;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Common.Spire
{
    public class SpireDA
    {
        private readonly HttpClient _client;
        private readonly ILogger<SpireDA> _logger;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _pgConnString; // Add PG connection string here
        private readonly AppDBContext _dbContext;

        public SpireDA(HttpClient client, ILogger<SpireDA> logger, string user, string pass, string pgConnString, AppDBContext dbContext)
        {
            _client = client;
            _logger = logger;
            _user = user;
            _pass = pass;
            _pgConnString = pgConnString;
            _dbContext = dbContext;
        }

        // === API call to Spire ===
        public async Task<SpireResponse> SendInventoryItemAsync(SpireInventoryItemRequest item, string endpoint)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(item);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            // Basic Auth
            var authBytes = System.Text.Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _client.SendAsync(request);
            sw.Stop();

            var respText = await response.Content.ReadAsStringAsync();

            var spireResp = new SpireResponse
            {
                HttpStatus = (int)response.StatusCode,
                HttpStatusText = response.ReasonPhrase,
                HeaderResponse = $"{response.Headers}{Environment.NewLine}{response.Content.Headers}",
                HeaderLocation = response.Headers.Location?.ToString(),
                HeaderContentLength = response.Content.Headers.ContentLength ?? 0,
                ResponseTime = sw.ElapsedMilliseconds
            };

            // Parse key from Location header
            if (!string.IsNullOrEmpty(spireResp.HeaderLocation))
            {
                var idx = spireResp.HeaderLocation.LastIndexOf('/');
                if (idx > -1 && long.TryParse(spireResp.HeaderLocation[(idx + 1)..], out var key))
                    spireResp.HeaderKey = key;
            }

            _logger.LogInformation("Spire API call → {Url} returned {Status} ({Time}ms)", endpoint, spireResp.HttpStatus, spireResp.ResponseTime);

            try
            {
                var settings = await _dbContext.tblSettings.FirstOrDefaultAsync();
                bool loggingEnabled = settings?.LoggingEnabled ?? true;

                if (loggingEnabled)
                {
                    int maxLen = settings?.LogResponseMaxSize ?? 4000;
                    bool logResp = settings?.LogResponseData ?? true;
                    string respString = logResp ? (respText.Length > maxLen ? "Truncated:" + respText[..maxLen] : respText) : "";

                    var apiLog = new tblAPILog
                    {
                        ServerID = 1,
                        CompanyID = 1,
                        CallType = "POST",
                        Endpoint = endpoint,
                        KeyValue = spireResp.HeaderKey.HasValue ? (int)spireResp.HeaderKey.Value : 0,
                        SendString = json,
                        Parameters = "",
                        ResponseString = respString,
                        FullURLPassed = endpoint,
                        FullURLUsed = _client.BaseAddress?.ToString() + endpoint,
                        HTTPStatus = spireResp.HttpStatus,
                        HTTPStatusText = spireResp.HttpStatusText,
                        HeaderResponse = spireResp.HeaderResponse?.Length > 255 ? spireResp.HeaderResponse[..255] : spireResp.HeaderResponse,
                        HeaderResponseKey = spireResp.HeaderKey?.ToString(),
                        HeaderResponseLocation = spireResp.HeaderLocation,
                        ResponseTime = spireResp.ResponseTime,
                        LogDateTime = DateTime.Now
                    };

                    _dbContext.tblAPILog.Add(apiLog);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to log API call to tblAPILog");
            }

            return spireResp;
        }

        // === Check if part exists in Postgres inventory ===
        public async Task<bool> PartExistsAsync(string partNo, string whse)
        {
            if (string.IsNullOrWhiteSpace(partNo) || string.IsNullOrWhiteSpace(whse))
                throw new ArgumentException("PartNo or Warehouse cannot be empty");

            try
            {
                await using var conn = new NpgsqlConnection(_pgConnString);
                await using var cmd = new NpgsqlCommand(
                    "SELECT 1 FROM inventory WHERE part_no = @partNo AND whse = @whse LIMIT 1",
                    conn);

                cmd.Parameters.AddWithValue("@partNo", partNo.ToUpper());
                cmd.Parameters.AddWithValue("@whse", whse);

                await conn.OpenAsync();
                return await cmd.ExecuteScalarAsync() != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PartExistsAsync failed for {PartNo}/{Whse}", partNo, whse);
                throw;
            }
        }
        public string BaseUrl => _client.BaseAddress?.ToString() ?? "";

        public async Task<HttpResponseMessage>ExecuteRequestAsync(HttpMethod method, string endpoint, string? jsonContent = null)
        {
            var request = new HttpRequestMessage(method,endpoint);
            if(!string.IsNullOrEmpty(jsonContent))
            {
                request.Content = new StringContent(jsonContent,System.Text.Encoding.UTF8,"application/json");
            }

            var authBytes = System.Text.Encoding.UTF8.GetBytes($"{_user}:{_pass}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return await _client.SendAsync(request);
        }

        // Expose PG connection string if needed
        public string PgConnString => _pgConnString;
    }
}
