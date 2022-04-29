using Discord;
using Discord.WebSocket;
using DiscordTranslationBot;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Services;
using MediatR;

try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(
            (hostBuilder, services) =>
            {
                services
                    .Configure<DiscordOptions>(hostBuilder.Configuration.GetRequiredSection(DiscordOptions.SectionName))
                    .AddMediatR(typeof(Program))
                    .AddSingleton(
                        new LibreTranslate.Net.LibreTranslate(hostBuilder.Configuration["LibreTranslate:ApiUrl"]))
                    .AddSingleton(new FlagEmojiService())
                    .AddSingleton(
                        new DiscordSocketClient(
                            new DiscordSocketConfig
                            {
                                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                                                 GatewayIntents.GuildMessageReactions,
                                MessageCacheSize = 100
                            }))
                    .AddSingleton<DiscordEventListener>()
                    .AddHostedService<Worker>();
            })
        .Build();

    await host.RunAsync();
}
catch (Exception ex) when ((ex is InvalidOperationException && ex.Message.Contains(DiscordOptions.SectionName)) ||
                           (ex is ArgumentNullException or UriFormatException &&
                            ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate)) == true))
{
    // The app is missing configuration options.
    Console.WriteLine(
        $"The {DiscordOptions.SectionName} and {nameof(LibreTranslate.Net.LibreTranslate)} options are not configured or set to an invalid format.");
}