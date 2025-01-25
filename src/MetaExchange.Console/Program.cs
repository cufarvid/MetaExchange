using MetaExchange.Core.Services;

var filePath = args.Length > 0
    ? args[0]
    : throw new ArgumentException("Order book file path not provided");

var fileExchangeService = new FileExchangeService(filePath);

var metaExchangeService = new MetaExchangeService(fileExchangeService);

var buyExecutionPlan = metaExchangeService.GetBestBuyExecutionPlan(1m);
var sellExecutionPlan = metaExchangeService.GetBestSellExecutionPlan(1m);
