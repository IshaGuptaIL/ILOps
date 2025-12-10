using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace ILOps_Inventory.Common.Spire
{
    public class SpireApiHelper
    {

        private readonly HttpClient _client;

        public SpireApiHelper(HttpClient client)
        {
            _client = client;
        }


        public async Task<(bool Success, string ApiResponseText, SpireResponse SpireResponse)>
            CallSpireAsync(
                string baseUrl,         
                HttpMethod callType,     
                string endpoint,       
                long keyValue,          
                string parameters,      
                string sendJson,        
                string userName,
                string password)
        {
            // ---------------- URL build ----------------
            var urlBuilder = new StringBuilder();
            urlBuilder.Append(baseUrl?.TrimEnd('/'));

            if (!string.IsNullOrEmpty(endpoint))
                urlBuilder.Append(endpoint);

            if (keyValue != 0)
                urlBuilder.Append("/").Append(keyValue);

            if (!string.IsNullOrEmpty(parameters))
                urlBuilder.Append(parameters); // assume ? ke sath aayega

            var finalUrl = urlBuilder.ToString();

            // -------------- Basic Auth set --------------
            _client.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                var bytes = Encoding.ASCII.GetBytes($"{userName}:{password}");
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
            }

            using var request = new HttpRequestMessage(callType, finalUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(sendJson) &&
                (callType == HttpMethod.Post || callType == HttpMethod.Put || callType.Method == "PATCH"))
            {
                request.Content = new StringContent(sendJson, Encoding.UTF8, "application/json");
            }

            // -------------- Call API --------------------
            var sw = Stopwatch.StartNew();
            using var response = await _client.SendAsync(request);
            sw.Stop();

            var apiResponseText = await response.Content.ReadAsStringAsync();

            var spireResp = new SpireResponse
            {
                HttpStatus = (int)response.StatusCode,
                HttpStatusText = response.ReasonPhrase,
                HeaderResponse = response.Headers + Environment.NewLine + response.Content.Headers,
                HeaderLocation = response.Headers.Location?.ToString(),
                HeaderContentLength = response.Content.Headers.ContentLength ?? 0,
                ResponseTime = sw.ElapsedMilliseconds,
                Allow = response.Content.Headers.Allow?.ToString()
            };

            // HeaderLocation se key nikalna (VBA wale HeaderKey jaisa)
            if (!string.IsNullOrEmpty(spireResp.HeaderLocation))
            {
                var lastSlash = spireResp.HeaderLocation.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < spireResp.HeaderLocation.Length - 1 &&
                    long.TryParse(spireResp.HeaderLocation[(lastSlash + 1)..], out var key))
                {
                    spireResp.HeaderKey = key;
                }
            }

            var success = response.IsSuccessStatusCode;
            return (success, apiResponseText, spireResp);
        }
    }
    }


