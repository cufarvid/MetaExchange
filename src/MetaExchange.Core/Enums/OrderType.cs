using System.Text.Json.Serialization;

namespace MetaExchange.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderType
{
    Buy,
    Sell
}