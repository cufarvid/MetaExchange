using System.Text.Json;
using MetaExchange.Core.Models;

namespace MetaExchange.Core.Services;

public sealed class FileExchangeService : IExchangeService
{
    private readonly IReadOnlyList<Exchange> _exchanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileExchangeService"/> class.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public FileExchangeService(string filePath)
    {
        _exchanges = ReadExchangesFromFile(filePath);
    }

    /// <inheritdoc /> 
    public IReadOnlyList<Exchange> GetExchanges() => _exchanges;

    /// <summary>
    /// Read exchanges from a file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>A list of exchanges.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    private List<Exchange> ReadExchangesFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Exchanges file not found", filePath);
        }

        var exchanges = new List<Exchange>();

        foreach (var line in File.ReadLines(filePath))
        {
            var data = line.Split();

            if (data is not [_, var orderBookJson])
            {
                continue;
            }

            try
            {
                var orderBook = JsonSerializer.Deserialize<OrderBook>(orderBookJson);

                if (orderBook is null)
                {
                    continue;
                }

                var exchange = CreateExchange(exchanges.Count, orderBook);

                exchanges.Add(exchange);
            }
            catch (JsonException e)
            {
                Console.WriteLine(e);
            }
        }

        return exchanges;
    }

    /// <summary>
    /// Create an exchange with a random balance.
    /// </summary>
    /// <param name="id">Id of the exchange.</param>
    /// <param name="orderBook">The order book.</param>
    /// <returns>An exchange.</returns>
    private static Exchange CreateExchange(int id, OrderBook orderBook)
    {
        var balance = new Balance(
            Random.Shared.Next(5, 51),
            Random.Shared.Next(50_000, 500_001));

        return new Exchange($"Exchange{id}", balance, orderBook);
    }
}