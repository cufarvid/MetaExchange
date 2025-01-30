using MetaExchange.Core.Enums;

namespace MetaExchange.Core.Models;

public sealed class Order(int? id, DateTime time, OrderType type, decimal amount, decimal price)
{
    public int? Id { get; } = id;
    public DateTime Time { get; } = time;
    public OrderType Type { get; } = type;
    public decimal Amount { get; set; } = amount;
    public decimal Price { get; } = price;
}