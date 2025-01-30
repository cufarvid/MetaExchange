using MetaExchange.Core.Enums;
using MetaExchange.Core.Models;

namespace MetaExchange.Core.Services;

public class MetaExchangeService(IExchangeService exchangeService) : IMetaExchangeService
{
    #region Constants

    /// <summary>
    /// The maximum number of decimals for calculations.
    /// </summary>
    private const int MaxDecimals = 8;

    /// <summary>
    /// The number of decimals for EUR.
    /// </summary>
    private const int EurDecimals = 2;

    /// <summary>
    /// The minimum trade amount.
    /// </summary>
    private const decimal MinTradeAmount = 0.00001m;

    /// <summary>
    /// The maximum trade amount.
    /// </summary>
    private const decimal MaxTradeAmount = 1000m;

    #endregion

    /// <inheritdoc />
    public IReadOnlyList<ExecutedOrder> GetBestBuyExecutionPlan(decimal amount)
    {
        ValidateTradeAmount(amount);

        var asks = GetAsks();

        var executedOrders = new List<ExecutedOrder>();
        var remainingAmount = amount;

        foreach (var (order, exchange) in asks)
        {
            if (remainingAmount < MinTradeAmount)
            {
                break;
            }

            var buyAmount = CalculateBuyAmount(remainingAmount, order, exchange);
            if (buyAmount < MinTradeAmount)
            {
                continue;
            }

            var buyCost = decimal.Round(buyAmount * order.Price, EurDecimals);

            exchange.Balance.UpdateEUR(-buyCost);
            remainingAmount -= buyAmount;
            
            UpdateOrderAfterExecution(order, exchange.OrderBook.Asks, buyAmount);

            executedOrders.Add(new ExecutedOrder(exchange.Id, OrderType.Buy, buyAmount, order.Price));
        }

        ValidateRemainingAmount(remainingAmount);

        return executedOrders;
    }

    /// <inheritdoc />
    public IReadOnlyList<ExecutedOrder> GetBestSellExecutionPlan(decimal amount)
    {
        ValidateTradeAmount(amount);

        var bids = GetBids();

        var executedOrders = new List<ExecutedOrder>();

        var remainingAmount = amount;

        foreach (var (order, exchange) in bids)
        {
            if (remainingAmount < MinTradeAmount)
            {
                break;
            }

            var sellAmount = CalculateSellAmount(remainingAmount, order, exchange);
            if (sellAmount < MinTradeAmount)
            {
                continue;
            }

            exchange.Balance.UpdateBTC(-sellAmount);
            remainingAmount -= sellAmount;
            
            UpdateOrderAfterExecution(order, exchange.OrderBook.Bids, sellAmount);

            executedOrders.Add(new ExecutedOrder(exchange.Id, OrderType.Sell, sellAmount, order.Price));
        }

        ValidateRemainingAmount(remainingAmount);

        return executedOrders;
    }


    /// <summary>
    /// Calculate the amount to buy.
    /// </summary>
    /// <param name="remainingAmount">The remaining amount to buy.</param>
    /// <param name="order">The order to buy.</param>
    /// <param name="exchange">The exchange to buy from.</param>
    /// <returns>The amount to buy.</returns>
    private static decimal CalculateBuyAmount(decimal remainingAmount, Order order, Exchange exchange)
    {
        var amountByOrder = Math.Min(remainingAmount, order.Amount);
        var amountByBalance = decimal.Round(exchange.Balance.EUR / order.Price, MaxDecimals);
        var amount = Math.Min(amountByOrder, amountByBalance);

        return decimal.Round(amount, MaxDecimals);
    }

    /// <summary>
    /// Calculate the amount to sell.
    /// </summary>
    /// <param name="remainingAmount">The remaining amount to sell.</param>
    /// <param name="order">The order to sell.</param>
    /// <param name="exchange">The exchange to sell from.</param>
    /// <returns>The amount to sell.</returns>
    private static decimal CalculateSellAmount(decimal remainingAmount, Order order, Exchange exchange)
    {
        var amountByOrder = Math.Min(remainingAmount, order.Amount);
        var amountByBalance = exchange.Balance.BTC;
        var amount = Math.Min(amountByOrder, amountByBalance);

        return decimal.Round(amount, MaxDecimals);
    }
   
    /// <summary>
    /// Update the order after execution.
    /// </summary>
    /// <param name="order">The order to update.</param>
    /// <param name="orders">The list of orders.</param>
    /// <param name="executedAmount">The executed amount.</param>
    private static void UpdateOrderAfterExecution(Order order, IList<OrderWrapper> orders, decimal executedAmount)
    {
        var remainingAmount = order.Amount - executedAmount;
        
        if (remainingAmount <= MinTradeAmount)
        {
            var orderWrapper = orders.First(wrapper => wrapper.Order == order);
            orders.Remove(orderWrapper);
        }
        else
        {
            order.Amount = remainingAmount;
        }
    }

    /// <summary>
    /// Validate the trade amount.
    /// </summary>
    /// <param name="amount">The amount to trade.</param>
    /// <exception cref="ArgumentException">Thrown when the amount is invalid.</exception>
    private static void ValidateTradeAmount(decimal amount)
    {
        switch (amount)
        {
            case <= 0:
                throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
            case < MinTradeAmount:
                throw new ArgumentException($"Amount below minimum trade size of {MinTradeAmount} BTC.",
                    nameof(amount));
            case > MaxTradeAmount:
                throw new ArgumentException($"Amount exceeds maximum trade size of {MaxTradeAmount} BTC.",
                    nameof(amount));
        }
    }

    /// <summary>
    /// Validate the remaining amount.
    /// </summary>
    /// <param name="remainingAmount">The remaining amount.</param>
    /// <exception cref="InvalidOperationException">Thrown when the remaining amount is invalid.</exception>
    private static void ValidateRemainingAmount(decimal remainingAmount)
    {
        if (remainingAmount > MinTradeAmount)
        {
            throw new InvalidOperationException($"Insufficient liquidity. Unable to fill {remainingAmount} BTC.");
        }
    }

    /// <summary>
    /// Get all asks from all exchanges.
    /// </summary>
    /// <returns>A list of asks.</returns>
    private IReadOnlyList<OrderInfo> GetAsks()
    {
        var exchanges = exchangeService.GetExchanges();

        return exchanges
            .Where(exchange => exchange.Balance.EUR > 0)
            .SelectMany(exchange =>
                exchange.OrderBook.Asks.Select(orderWrapper => new OrderInfo(orderWrapper.Order, exchange)))
            .OrderBy(ask => ask.Order.Price)
            .ToList();
    }

    /// <summary>
    /// Get all bids from all exchanges.
    /// </summary>
    /// <returns>A list of bids.</returns>
    private IReadOnlyList<OrderInfo> GetBids()
    {
        var exchanges = exchangeService.GetExchanges();

        return exchanges
            .Where(exchange => exchange.Balance.BTC > 0)
            .SelectMany(exchange =>
                exchange.OrderBook.Bids.Select(orderWrapper => new OrderInfo(orderWrapper.Order, exchange)))
            .OrderByDescending(bid => bid.Order.Price)
            .ToList();
    }
}