using FluentAssertions;
using MetaExchange.Core.Services;

namespace MetaExchange.Tests;

public class FileExchangeServiceTests
{
    [Fact]
    public void FileExists_LoadsExchanges()
    {
        // Arrange
        const string testFilePath = "TestData/order-books-valid";

        // Act
        var service = new FileExchangeService(testFilePath);
        var exchanges = service.GetExchanges();

        // Assert
        exchanges.Should().HaveCount(2);

        var exchange1 = exchanges[0];
        exchange1.Id.Should().Be("Exchange0");
        exchange1.Balance.Should().NotBeNull();
        exchange1.OrderBook.Should().NotBeNull();
        exchange1.OrderBook.Asks.Should().HaveCount(5);
        exchange1.OrderBook.Bids.Should().HaveCount(5);

        var exchange2 = exchanges[1];
        exchange2.Id.Should().Be("Exchange1");
        exchange2.Balance.Should().NotBeNull();
        exchange2.OrderBook.Should().NotBeNull();
        exchange2.OrderBook.Asks.Should().HaveCount(5);
        exchange2.OrderBook.Bids.Should().HaveCount(5);
    }

    [Fact]
    public void InvalidLine_ShouldSkipLine()
    {
        // Arrange
        const string testFilePath = "TestData/order-books-invalid";

        // Act
        var service = new FileExchangeService(testFilePath);
        var exchanges = service.GetExchanges();

        // Assert
        exchanges.Should().BeEmpty();
    }
    
    [Fact]
    public void InvalidFile_ShouldThrowException()
    {
        // Arrange
        const string testFilePath = "TestData/non-existent-file";

        // Act
        Action act = () => new FileExchangeService(testFilePath);

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("Exchanges file not found")
            .And.FileName.Should().Be(testFilePath);
    }
}