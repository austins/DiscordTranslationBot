using DiscordTranslationBot.Jobs;
using Mediator;

namespace DiscordTranslationBot.Tests.Unit.Jobs;

public sealed class SchedulerTests
{
    private readonly Scheduler _sut;
    private readonly TimeProvider _timeProvider;

    public SchedulerTests()
    {
        _timeProvider = Substitute.For<TimeProvider>();

        _sut = new Scheduler(Substitute.For<ISender>(), _timeProvider, new LoggerFake<Scheduler>());
    }

    [Fact]
    public async Task ScheduleAsync_CommandWithExecuteAt_Success()
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

        var command = Substitute.For<ICommand>();
        var executeAt = DateTimeOffset.MinValue.AddDays(2);

        // Act
        await _sut.ScheduleAsync(command, executeAt, CancellationToken.None);

        // Assert
        _sut.Count.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ScheduleAsync_CommandWithExecuteAt_ThrowsIfInvalidTime(int seconds)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var command = Substitute.For<ICommand>();
        var executeAt = DateTimeOffset.MaxValue.AddSeconds(seconds);

        // Act + Assert
        await _sut
            .Invoking(x => x.ScheduleAsync(command, executeAt, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ScheduleAsync_CommandWithExecutionDelay_Success()
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

        var command = Substitute.For<ICommand>();
        var executionDelay = TimeSpan.FromHours(2);

        // Act
        await _sut.ScheduleAsync(command, executionDelay, CancellationToken.None);

        // Assert
        _sut.Count.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ScheduleAsync_CommandWithExecutionDelay_ThrowsIfInvalidTime(int seconds)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var command = Substitute.For<ICommand>();
        var executeAt = TimeSpan.FromSeconds(seconds);

        // Act + Assert
        await _sut
            .Invoking(x => x.ScheduleAsync(command, executeAt, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetNextJobDueAsync_Success(bool hasTaskScheduled)
    {
        // Arrange
        var executeAt1 = DateTimeOffset.MinValue.AddDays(2);
        var executeAt2 = DateTimeOffset.MinValue.AddDays(3);

        if (hasTaskScheduled)
        {
            // Allow command to be scheduled.
            _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

            var command1 = Substitute.For<ICommand>();
            await _sut.ScheduleAsync(command1, executeAt1, CancellationToken.None);

            var command2 = Substitute.For<ICommand>();
            await _sut.ScheduleAsync(command2, executeAt2, CancellationToken.None);
        }

        // Modify time so task is returned.
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var countBeforeGet = _sut.Count;

        // Act
        var job1 = await _sut.GetNextJobDueAsync(CancellationToken.None);
        var job2 = await _sut.GetNextJobDueAsync(CancellationToken.None);

        // Assert
        if (hasTaskScheduled)
        {
            job1.Should().NotBeNull();
            job1!.ExecuteAt.Should().Be(executeAt1);

            job2.Should().NotBeNull();
            job2!.ExecuteAt.Should().Be(executeAt2);

            countBeforeGet.Should().Be(2);
        }
        else
        {
            job1.Should().BeNull();
            job2.Should().BeNull();

            countBeforeGet.Should().Be(0);
        }

        _sut.Count.Should().Be(0);
    }
}
