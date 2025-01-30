namespace MetaExchange.Core.Models;

public sealed record OrderBook(IList<OrderWrapper> Bids, IList<OrderWrapper> Asks);