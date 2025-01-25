using MetaExchange.Core.Models;

namespace MetaExchange.Tests;

public static class TestExchange
{
    public static Exchange CreateExchange(string id, Balance balance, IEnumerable<Order> bids,
        IEnumerable<Order> asks) =>
        new(id,
            balance,
            new OrderBook(
                bids.Select(o => new OrderWrapper(o)).ToList(),
                asks.Select(o => new OrderWrapper(o)).ToList()));
}