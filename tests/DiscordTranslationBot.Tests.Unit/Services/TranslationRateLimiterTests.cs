using DiscordTranslationBot.Services;

namespace DiscordTranslationBot.Tests.Unit.Services;

public sealed class TranslationRateLimiterTests : IDisposable
{
    private readonly LoggerFake<TranslationRateLimiter> _logger;
    private readonly TranslationRateLimiter _sut;

    public TranslationRateLimiterTests()
    {
        _logger = new LoggerFake<TranslationRateLimiter>();
        _sut = new TranslationRateLimiter(_logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void TryAcquire_ReturnsFalse_WhenPermitLimitExceeded()
    {
        // Arrange
        const ulong userId = 1UL;

        for (var i = 0; i < TranslationRateLimiter.PermitLimit; i++)
        {
            _sut.TryAcquire(userId).Should().BeTrue();
        }

        // Act
        var result = _sut.TryAcquire(userId);

        // Assert
        result.Should().BeFalse();
        _logger.Entries.Should().ContainSingle();
        _logger.Entries[0].LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void TryAcquire_IsPartitionedByUser()
    {
        // Arrange
        for (var i = 0; i < TranslationRateLimiter.PermitLimit; i++)
        {
            _sut.TryAcquire(1UL);
        }

        // Act
        var result = _sut.TryAcquire(2UL);

        // Assert
        result.Should().BeTrue();
    }
}
