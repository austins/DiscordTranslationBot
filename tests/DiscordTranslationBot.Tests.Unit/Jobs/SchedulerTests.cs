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

    [Test]
    public async Task ScheduleAsync_CommandWithExecuteAt_Success(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

        var command = Substitute.For<ICommand>();
        var executeAt = DateTimeOffset.MinValue.AddDays(2);

        // Act
        await _sut.ScheduleAsync(command, executeAt, cancellationToken);

        // Assert
        _sut.Count.ShouldBe(1);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task ScheduleAsync_CommandWithExecuteAt_ThrowsIfInvalidTime(
        int seconds,
        CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var command = Substitute.For<ICommand>();
        var executeAt = DateTimeOffset.MaxValue.AddSeconds(seconds);

        // Act + Assert
        await Should.ThrowAsync<Exception>(() => _sut.ScheduleAsync(command, executeAt, cancellationToken));
    }

    [Test]
    public async Task ScheduleAsync_CommandWithExecutionDelay_Success(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

        var command = Substitute.For<ICommand>();
        var executionDelay = TimeSpan.FromHours(2);

        // Act
        await _sut.ScheduleAsync(command, executionDelay, cancellationToken);

        // Assert
        _sut.Count.ShouldBe(1);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task ScheduleAsync_CommandWithExecutionDelay_ThrowsIfInvalidTime(
        int seconds,
        CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var command = Substitute.For<ICommand>();
        var executeAt = TimeSpan.FromSeconds(seconds);

        // Act + Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.ScheduleAsync(command, executeAt, cancellationToken));
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task GetNextJobDueAsync_Success(bool hasTaskScheduled, CancellationToken cancellationToken)
    {
        // Arrange
        var executeAt1 = DateTimeOffset.MinValue.AddDays(2);
        var executeAt2 = DateTimeOffset.MinValue.AddDays(3);

        if (hasTaskScheduled)
        {
            // Allow command to be scheduled.
            _timeProvider.GetUtcNow().Returns(DateTimeOffset.MinValue);

            var command1 = Substitute.For<ICommand>();
            await _sut.ScheduleAsync(command1, executeAt1, cancellationToken);

            var command2 = Substitute.For<ICommand>();
            await _sut.ScheduleAsync(command2, executeAt2, cancellationToken);
        }

        // Modify time so task is returned.
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        var countBeforeGet = _sut.Count;

        // Act
        var job1 = await _sut.GetNextJobDueAsync(cancellationToken);
        var job2 = await _sut.GetNextJobDueAsync(cancellationToken);

        // Assert
        if (hasTaskScheduled)
        {
            job1.ShouldNotBeNull();
            job1.ExecuteAt.ShouldBe(executeAt1);

            job2.ShouldNotBeNull();
            job2.ExecuteAt.ShouldBe(executeAt2);

            countBeforeGet.ShouldBe(2);
        }
        else
        {
            job1.ShouldBeNull();
            job2.ShouldBeNull();

            countBeforeGet.ShouldBe(0);
        }

        _sut.Count.ShouldBe(0);
    }
}
