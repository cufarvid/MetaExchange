using MetaExchange.Core.Models;

namespace MetaExchange.Core.Services;

public interface IMetaExchangeService
{
    /// <summary>
    /// Get best buy execution plan for the given amount.
    /// </summary>
    /// <param name="amount">The amount to buy.</param>
    /// <returns>A list of executed orders.</returns>
    public IReadOnlyList<ExecutedOrder> GetBestBuyExecutionPlan(decimal amount);
    
    /// <summary>
    /// Get best sell execution plan for the given amount.
    /// </summary>
    /// <param name="amount">The amount to sell.</param>
    /// <returns>A list of executed orders.</returns>
    public IReadOnlyList<ExecutedOrder> GetBestSellExecutionPlan(decimal amount);
}