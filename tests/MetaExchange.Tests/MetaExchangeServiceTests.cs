using FluentAssertions;
using MetaExchange.Core.Enums;
using MetaExchange.Core.Models;
using MetaExchange.Core.Services;
using Moq;

namespace MetaExchange.Tests;

public class MetaExchangeServiceTests
{
    private readonly Mock<IExchangeService> _exchangeServiceMock = new();
    private readonly MetaExchangeService _metaExchangeService;

    public MetaExchangeServiceTests()
    {
        _metaExchangeService = new MetaExchangeService(_exchangeServiceMock.Object);
    }

    #region Buy

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetBestBuyExecutionPlan_InvalidAmount_ThrowsArgumentException(decimal amount)
    {
        // Act
        var act = () => _metaExchangeService.GetBestBuyExecutionPlan(amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount must be greater than 0.*");
    }

    [Fact]
    public void GetBestBuyExecutionPlan_ValidAmount_ReturnsExecutionPlan()
    {
        // Arrange
        var asks1 = new List<Order>
        {
            // Best price
            TestOrder.Sell(1m, 1000m),
        };
        var asks2 = new List<Order>
        {
            TestOrder.Sell(2m, 1001m)
        };

        var exchange1 = TestExchange.CreateExchange("Exchange1", new Balance(0m, 100_000m), [], asks1);
        var exchange2 = TestExchange.CreateExchange("Exchange2", new Balance(0m, 100_000m), [], asks2);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange1, exchange2 });

        // Act
        var executionPlan = _metaExchangeService.GetBestBuyExecutionPlan(1);

        // Assert
        executionPlan.Should().HaveCount(1);
        executionPlan[0].ExchangeId.Should().Be("Exchange1");
        executionPlan[0].Type.Should().Be(OrderType.Buy);
        executionPlan[0].Amount.Should().Be(1);
        executionPlan[0].Price.Should().Be(1000);
    }

    [Fact]
    public void GetBestBuyExecutionPlan_OrderLargerThanSingleExchange_FillsAcrossMultipleExchanges()
    {
        // Arrange
        var asks1 = new List<Order>
        {
            TestOrder.Sell(1m, 1000m),
            TestOrder.Sell(1m, 1001m)
        };
        var asks2 = new List<Order>
        {
            TestOrder.Sell(1m, 1002m)
        };

        var exchange1 = TestExchange.CreateExchange("Exchange1", new Balance(0m, 100_000m), [], asks1);
        var exchange2 = TestExchange.CreateExchange("Exchange2", new Balance(0m, 100_000m), [], asks2);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange1, exchange2 });

        // Act
        var result = _metaExchangeService.GetBestBuyExecutionPlan(2.5m);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().BeEquivalentTo(new { ExchangeId = "Exchange1", Amount = 1m, Price = 1000m });
        result[1].Should().BeEquivalentTo(new { ExchangeId = "Exchange1", Amount = 1m, Price = 1001m });
        result[2].Should().BeEquivalentTo(new { ExchangeId = "Exchange2", Amount = 0.5m, Price = 1002m });
    }

    [Fact]
    public void GetBestBuyExecutionPlan_InsufficientExchangeBalance_SkipsToNextExchange()
    {
        // Arrange
        var asks1 = new List<Order>
        {
            TestOrder.Sell(1m, 1000m)
        };
        var asks2 = new List<Order>
        {
            TestOrder.Sell(1m, 1001m)
        };


        // Only enough EUR for 0.5 BTC
        var exchange1 = TestExchange.CreateExchange("Exchange1", new Balance(0m, 500m), [], asks1);
        var exchange2 = TestExchange.CreateExchange("Exchange2", new Balance(0m, 100_000m), [], asks2);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange1, exchange2 });

        // Act
        var result = _metaExchangeService.GetBestBuyExecutionPlan(1m);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { ExchangeId = "Exchange1", Amount = 0.5m, Price = 1000m });
        result[1].Should().BeEquivalentTo(new { ExchangeId = "Exchange2", Amount = 0.5m, Price = 1001m });
    }

    [Fact]
    public void GetBestBuyExecutionPlan_InsufficientLiquidity_ThrowsException()
    {
        // Arrange
        var asks = new List<Order>
        {
            TestOrder.Sell(0.5m, 1000m)
        };
        var exchange = TestExchange.CreateExchange("Exchange1", new Balance(10m, 1_000m), [], asks);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange });

        // Act
        var act = () => _metaExchangeService.GetBestBuyExecutionPlan(1m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient liquidity*");
    }

    [Fact]
    public void GetBestBuyExecutionPlan_OrdersSortedByPrice_TakesBestPriceFirst()
    {
        // Arrange
        var asks = new List<Order>
        {
            TestOrder.Sell(1m, 1002m),
            // Best price but not first in list
            TestOrder.Sell(1m, 1000m),
            TestOrder.Sell(1m, 1001m)
        };

        var exchange = TestExchange.CreateExchange("Exchange1", new Balance(0m, 100_000m), [], asks);
        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange });

        // Act
        var result = _metaExchangeService.GetBestBuyExecutionPlan(2m);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { Price = 1000m, Amount = 1m });
        result[1].Should().BeEquivalentTo(new { Price = 1001m, Amount = 1m });
    }

    [Fact]
    public void GetBestBuyExecutionPlan_MultiplePrices_CorrectlyCalculatesTotalCost()
    {
        // Arrange
        var asks = new List<Order>
        {
            // Cost: 500 EUR
            TestOrder.Sell(0.5m, 1000m),
            // Cost: 550 EUR
            TestOrder.Sell(0.5m, 1100m)
        };

        var exchange = TestExchange.CreateExchange("Exchange1", new Balance(0m, 1050m), [], asks);
        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange });

        // Act
        var result = _metaExchangeService.GetBestBuyExecutionPlan(1m);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new
        {
            ExchangeId = "Exchange1",
            Type = OrderType.Buy,
            Amount = 0.5m,
            Price = 1000m
        });
        result[1].Should().BeEquivalentTo(new
        {
            ExchangeId = "Exchange1",
            Type = OrderType.Buy,
            Amount = 0.5m,
            Price = 1100m
        });
    }

    #endregion

    #region Sell

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetBestSellExecutionPlan_InvalidAmount_ThrowsArgumentException(decimal amount)
    {
        var act = () => _metaExchangeService.GetBestSellExecutionPlan(amount);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount must be greater than 0.*");
    }

    [Fact]
    public void GetBestSellExecutionPlan_ValidAmount_ReturnsExecutionPlan()
    {
        // Arrange
        var bids1 = new List<Order>
        {
            // Best price
            TestOrder.Buy(1m, 1000m),
        };
        var bids2 = new List<Order>
        {
            // Lower price
            TestOrder.Buy(2m, 999m)
        };

        var exchange1 = TestExchange.CreateExchange("Exchange1", new Balance(10m, 0m), bids1, []);
        var exchange2 = TestExchange.CreateExchange("Exchange2", new Balance(10m, 0m), bids2, []);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange1, exchange2 });

        // Act
        var executionPlan = _metaExchangeService.GetBestSellExecutionPlan(1);

        // Assert
        executionPlan.Should().HaveCount(1);
        executionPlan[0].Should().BeEquivalentTo(new
        {
            ExchangeId = "Exchange1",
            Type = OrderType.Sell,
            Amount = 1m,
            Price = 1000m
        });
    }

    [Fact]
    public void GetBestSellExecutionPlan_OrderLargerThanSingleExchange_FillsAcrossMultipleExchanges()
    {
        // Arrange
        var bids1 = new List<Order>
        {
            TestOrder.Buy(1m, 1000m),
            TestOrder.Buy(1m, 999m)
        };
        var bids2 = new List<Order>
        {
            TestOrder.Buy(1m, 998m)
        };

        var exchange1 = TestExchange.CreateExchange("Exchange1", new Balance(2m, 0m), bids1, []);
        var exchange2 = TestExchange.CreateExchange("Exchange2", new Balance(1m, 0m), bids2, []);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange1, exchange2 });

        // Act
        var result = _metaExchangeService.GetBestSellExecutionPlan(2.5m);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().BeEquivalentTo(new { ExchangeId = "Exchange1", Amount = 1m, Price = 1000m });
        result[1].Should().BeEquivalentTo(new { ExchangeId = "Exchange1", Amount = 1m, Price = 999m });
        result[2].Should().BeEquivalentTo(new { ExchangeId = "Exchange2", Amount = 0.5m, Price = 998m });
    }

    [Fact]
    public void GetBestSellExecutionPlan_InsufficientBtcBalance_SkipsToNextExchange()
    {
        // Arrange
        var bids1 = new List<Order>
        {
            TestOrder.Buy(1m, 1000m)
        };
        var bids2 = new List<Order>
        {
            TestOrder.Buy(1m, 999m)
        };

        // Only enough BTC for 0.5
        var exchange1 =
            TestExchange.CreateExchange("Exchange1", new Balance(0.5m, 0m), bids1, []);
        var exchange2 = TestExchange.CreateExchange("Exchange2", new Balance(1m, 0m), bids2, []);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange1, exchange2 });

        // Act
        var result = _metaExchangeService.GetBestSellExecutionPlan(1m);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { ExchangeId = "Exchange1", Amount = 0.5m, Price = 1000m });
        result[1].Should().BeEquivalentTo(new { ExchangeId = "Exchange2", Amount = 0.5m, Price = 999m });
    }

    [Fact]
    public void GetBestSellExecutionPlan_InsufficientLiquidity_ThrowsException()
    {
        // Arrange
        var bids = new List<Order>
        {
            TestOrder.Buy(0.5m, 1000m)
        };
        var exchange = TestExchange.CreateExchange("Exchange1", new Balance(1m, 1_000m), bids, []);

        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange });

        // Act
        var act = () => _metaExchangeService.GetBestSellExecutionPlan(1m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient liquidity*");
    }

    [Fact]
    public void GetBestSellExecutionPlan_OrdersSortedByPrice_TakesBestPriceFirst()
    {
        // Arrange
        var bids = new List<Order>
        {
            TestOrder.Buy(1m, 998m),
            // Best price but not first in list
            TestOrder.Buy(1m, 1000m),
            TestOrder.Buy(1m, 999m)
        };

        var exchange = TestExchange.CreateExchange("Exchange1", new Balance(3m, 0m), bids, []);
        _exchangeServiceMock.Setup(service => service.GetExchanges())
            .Returns(new List<Exchange> { exchange });

        // Act
        var result = _metaExchangeService.GetBestSellExecutionPlan(2m);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { Price = 1000m, Amount = 1m });
        result[1].Should().BeEquivalentTo(new { Price = 999m, Amount = 1m });
    }

    #endregion
}