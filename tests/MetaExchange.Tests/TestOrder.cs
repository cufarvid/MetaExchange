using MetaExchange.Core.Enums;
using MetaExchange.Core.Models;

namespace MetaExchange.Tests;

public static class TestOrder
{
    public static Order Buy(decimal amount, decimal price) => new(null, DateTime.Now, OrderType.Buy, amount, price);
    public static Order Sell(decimal amount, decimal price) => new(null, DateTime.Now, OrderType.Sell, amount, price);
}