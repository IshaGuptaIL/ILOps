using Npgsql;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DAL.Common.Spire
{
    public class SpireDA
    {
        private readonly HttpClient _client;
        private readonly ILogger<SpireDA> _logger;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _pgConnString; // Add PG connection string here

        public SpireDA(HttpClient client, ILogger<SpireDA> logger, string user, string pass, string pgConnString)
        {
            _client = client;
            _logger = logger;
            _user = user;
            _pass = pass;
            _pgConnString = pgConnString;
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
                    "SELECT 1 FROM public.inventory WHERE part_no = @partNo AND whse = @whse LIMIT 1",
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

        // Expose PG connection string if needed
        public string PgConnString => _pgConnString;
    }
}
