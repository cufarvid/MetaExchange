using MetaExchange.Core.Enums;

namespace MetaExchange.Core.Models;

public sealed record Order(int? Id, DateTime Time, OrderType Type, decimal Amount, decimal Price);