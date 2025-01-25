namespace MetaExchange.Core.Models;

public sealed record Order(int? Id, DateTime Time, string Type, string Kind, decimal Amount, decimal Price);
