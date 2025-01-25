using MetaExchange.Core.Enums;

namespace MetaExchange.Core.Models;

public sealed record ExecutedOrder(string ExchangeId, OrderType Type, decimal Amount, decimal Price);