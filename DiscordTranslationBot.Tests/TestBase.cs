namespace DiscordTranslationBot.Tests;

public abstract class TestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    protected TestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected ICacheLogger<T> CreateLogger<T>(LogLevel logLevel = LogLevel.Information)
        where T : class
    {
        return _testOutputHelper.BuildLoggerFor<T>(logLevel);
    }
}
