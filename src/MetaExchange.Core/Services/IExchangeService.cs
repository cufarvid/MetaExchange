using MetaExchange.Core.Models;

namespace MetaExchange.Core.Services;

public interface IExchangeService
{
    /// <summary>
    /// Get all exchanges.
    /// </summary>
    /// <returns>A list of exchanges.</returns>
    public IReadOnlyList<Exchange> GetExchanges();
}