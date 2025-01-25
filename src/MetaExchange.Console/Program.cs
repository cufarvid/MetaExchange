using MetaExchange.Core.Services;

if (args.Length == 0)
{
    Console.WriteLine("Usage: MetaExchange.Console <filepath> [type=buy|sell] [amount=1.0]");
    return;
}

try
{
    var filePath = args[0] ?? throw new ArgumentException("Order book file path not provided");
    var type = args.Length > 1 ? args[1].ToLower() : "buy";
    var amount = args.Length > 2 ? decimal.Parse(args[2]) : 1.0m;

    if (!new[] { "buy", "sell" }.Contains(type.ToLowerInvariant()))
    {
        Console.WriteLine("Invalid type. Must be either 'buy' or 'sell'");
        return;
    }

    var fileExchangeService = new FileExchangeService(filePath);
    var metaExchangeService = new MetaExchangeService(fileExchangeService);

    var executionPlan =
        type.Equals("buy", StringComparison.InvariantCultureIgnoreCase)
            ? metaExchangeService.GetBestBuyExecutionPlan(amount)
            : metaExchangeService.GetBestSellExecutionPlan(amount);

    Console.WriteLine($"\nExecution plan for {type} {amount} BTC:");
    foreach (var order in executionPlan)
    {
        Console.WriteLine($"Exchange: {order.ExchangeId}, Amount: {order.Amount} BTC, Price: {order.Price}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}