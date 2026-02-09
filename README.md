# Discord Translation Bot

<img src="https://github.com/austins/DiscordTranslationBot/assets/1623983/96f1b58b-94f4-4df6-a81c-34e4f0342dc0" align="right" alt="Globe with flags" />

A Discord bot that allows translations of messages in a Discord server (guild) using country flags and `/translate`
command, powered by .NET
and [Discord.Net](https://github.com/discord-net/Discord.Net).

It supports the following translation providers, all of which are disabled by default, that run in the following order:

1. [Azure Translator](https://azure.microsoft.com/en-us/services/cognitive-services/translator/) (has a free tier)
2. [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) (free and open-source)

If any provider fails to provide a translation, the bot will use the next provider if any as a fallback. At least one
provider is required or else the app will exit with an error.

Which providers are enabled can be configured per the instructions below.

## Requirements

### Create a Discord Bot

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a new application with
   the name you want the bot to have.
2. Go to the "Bot" tab in the sidebar and create a bot. Take note of the bot token to use for development or production.
   Check the setting for whether you want it to be a Public Bot or not.
3. Enable "Message Content Intent" under _Privileged Gateway Intents_.
4. Go to the "OAuth2" -> "URL Generator" tab in the sidebar. Check the following scopes: `bot` and
   permissions: `Send Messages`, `Manage Messages`, and `Read Message History`.
5. Copy the Generated URL and open it in your browser to add the bot to your Discord server.

### Optional: Run LibreTranslate

1. See the [LibreTranslate repository](https://github.com/LibreTranslate/LibreTranslate) and their Docker Compose files for instructions on how to run a LibreTranslate Docker container.
2. Optionally, you can mount a named volume `/home/libretranslate/.local` to persist the language models and avoid redownloading them on startup.

## Development

1. Configure the user secrets file with the required environment variables. Example below:

```json
{
  "Discord": {
    "BotToken": ""
  },
  "TranslationProviders": {
    "AzureTranslator": {
      "Enabled": true,
      "ApiUrl": "https://api.cognitive.microsofttranslator.com", 
      "Region": "",
      "SecretKey": ""
    },
    "LibreTranslate": {
      "Enabled": true,
      "ApiUrl": "http://localhost:5000"
    }
  }, 
  "Telemetry": {
    "Enabled": true
  },
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:5434/ingest/otlp",
  "OTEL_EXPORTER_OTLP_PROTOCOL": "http/protobuf"
}
```

2. Make sure that you've created a Discord bot and have configured at least one translation provider using the steps
   above.

## Deployment

1. Build a Docker image with `docker build -t discordtranslationbot -f ./src/DiscordTranslationBot/Dockerfile .` in the directory that contains the `Dockerfile`.
2. Create and run a container with `docker run discordtranslationbot` and the following environment variables
   configured. Make sure that you've created a Discord bot and have configured at least one translation provider using
   the steps above. Example below:

```
Discord__BotToken=
TranslationProviders__AzureTranslator__Enabled=true
TranslationProviders__AzureTranslator__ApiUrl=https://api.cognitive.microsofttranslator.com
TranslationProviders__AzureTranslator__Region=
TranslationProviders__AzureTranslator__SecretKey=
TranslationProviders__LibreTranslate__Enabled=true
TranslationProviders__LibreTranslate__ApiUrl=http://localhost:5000
```

_All translation providers are disabled by default. Set the `TranslationProviders__ProviderName__Enabled` config setting
to `true` for those you which to
enable. When a provider is enabled, you must provide the related config settings for the provider or the app will exit
with an error._

## Telemetry

This app logs general information, warnings, and errors that may occur during runtime, along with metrics and traces for
performance and detection of any issues from code or external calls; the bot does not log contents of messages.

You can configure the app to persist logging, metric, and trace output using the OpenTelemetry protocol by enabling the
following option via an environment variable:

```
Telemetry__Enabled=true
```

Then you'll need to configure the OpenTelemetry Exporter. Here are some example environment variables for a global
endpoint:

```
OTEL_EXPORTER_OTLP_ENDPOINT=
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_HEADERS=
```

Refer to the [OpenTelemetry documentation](https://opentelemetry.io/docs/zero-code/net/configuration/#otlp) for other
environment variables.

## License

See LICENSE file in this repo.

This app has been developed by Austin S.
