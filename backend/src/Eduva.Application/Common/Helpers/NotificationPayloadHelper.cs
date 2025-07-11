using System.Text.Json;

namespace Eduva.Application.Common.Helpers
{
    public static class NotificationPayloadHelper
    {

        public static object DeserializePayload(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                return new { };
            }

            try
            {
                using var document = JsonDocument.Parse(jsonPayload);
                return ConvertJsonElementToObject(document.RootElement);
            }
            catch (JsonException ex)
            {
                return new
                {
                    error = "Invalid JSON payload",
                    originalPayload = jsonPayload,
                    parseError = ex.Message
                };
            }
        }

        private static object ConvertJsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ConvertJsonElementToObject(property.Value);
                    }
                    return obj;

                case JsonValueKind.Array:
                    return element.EnumerateArray()
                        .Select(ConvertJsonElementToObject)
                        .ToList();

                case JsonValueKind.String:
                    return element.GetString() ?? "";

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null!;

                default:
                    return element.ToString();
            }
        }
    }
}