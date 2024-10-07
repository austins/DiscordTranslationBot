using DiscordTranslationBot.Jobs;
using Mediator;

namespace DiscordTranslationBot.Tests.Unit.Jobs;

public sealed class SchedulerTests
{
    private readonly IMediator _mediator;
    private readonly Scheduler _sut;
    private readonly TimeProvider _timeProvider;

    public SchedulerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _timeProvider = Substitute.For<TimeProvider>();

        _sut = new Scheduler(_mediator, _timeProvider, new LoggerFake<Scheduler>());
    }

    [Fact]
    public void Schedule_CommandWithExecuteAt_Success()
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

        var command = Substitute.For<ICommand>();
        var executeAt = DateTimeOffset.MinValue.AddDays(2);

        // Act
        _sut.Schedule(command, executeAt);

        // Assert
        _sut.Count.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Schedule_CommandWithExecuteAt_ThrowsIfInvalidTime(int seconds)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var command = Substitute.For<ICommand>();
        var executeAt = DateTimeOffset.MaxValue.AddSeconds(seconds);

        // Act + Assert
        _sut.Invoking(x => x.Schedule(command, executeAt)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Schedule_CommandWithExecutionDelay_Success()
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

        var command = Substitute.For<ICommand>();
        var executionDelay = TimeSpan.FromHours(2);

        // Act
        _sut.Schedule(command, executionDelay);

        // Assert
        _sut.Count.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Schedule_CommandWithExecutionDelay_ThrowsIfInvalidTime(int seconds)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var command = Substitute.For<ICommand>();
        var executeAt = TimeSpan.FromSeconds(seconds);

        // Act + Assert
        _sut.Invoking(x => x.Schedule(command, executeAt)).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryGetNextTask_Success(bool hasTaskScheduled)
    {
        // Arrange
        if (hasTaskScheduled)
        {
            // Allow command to be scheduled.
            _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

            var command = Substitute.For<ICommand>();
            var executeAt = DateTimeOffset.MinValue.AddDays(2);
            _sut.Schedule(command, executeAt);
        }

        // Modify time so task is returned.
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var countBeforeGet = _sut.Count;

        // Act
        var result = _sut.TryGetNextTask(out var task);

        // Assert
        if (hasTaskScheduled)
        {
            result.Should().BeTrue();
            task.Should().NotBeNull();
            countBeforeGet.Should().Be(1);
        }
        else
        {
            result.Should().BeFalse();
            task.Should().BeNull();
            countBeforeGet.Should().Be(0);
        }

        _sut.Count.Should().Be(0);
    }
}
