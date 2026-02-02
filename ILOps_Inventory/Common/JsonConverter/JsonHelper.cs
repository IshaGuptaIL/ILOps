using System.Text.Json;

namespace ILOps_Inventory.Common.JsonConverter
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public static T? FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public static string ToJson(object value, bool indented = true)
        {
            var opts = indented ? _options : new JsonSerializerOptions(_options) { WriteIndented = false };
            return JsonSerializer.Serialize(value, opts);
        }
    }
}
