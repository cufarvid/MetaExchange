namespace MetaExchange.Core.Models;

public sealed record OrderBook(IReadOnlyList<OrderWrapper> Bids, IReadOnlyList<OrderWrapper> Asks);