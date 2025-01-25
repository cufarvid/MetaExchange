namespace MetaExchange.Core.Models;

public sealed class Balance(decimal btc, decimal eur)
{
    public decimal BTC { get; private set; } = btc;

    public decimal EUR { get; private set; } = eur;

    /// <summary>
    /// Updates the BTC balance by the given amount.
    /// </summary>
    /// <param name="amountDelta">The amount to add to the BTC balance.</param>
    /// <exception cref="InvalidOperationException">Thrown when the resulting BTC balance would be negative.</exception>
    public void UpdateBTC(decimal amountDelta)
    {
        if (BTC + amountDelta < 0)
        {
            throw new InvalidOperationException("BTC balance cannot be negative");
        }

        BTC += amountDelta;
    }

    /// <summary>
    /// Updates the EUR balance by the given amount.
    /// </summary>
    /// <param name="amountDelta">The amount to add to the EUR balance.</param>
    /// <exception cref="InvalidOperationException">Thrown when the resulting EUR balance would be negative.</exception>
    public void UpdateEUR(decimal amountDelta)
    {
        if (EUR + amountDelta < 0)
        {
            throw new InvalidOperationException("EUR balance cannot be negative");
        }

        EUR += amountDelta;
    }
}