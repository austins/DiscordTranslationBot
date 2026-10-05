# Discord Translation Bot

<img src="https://github.com/austins/DiscordTranslationBot/assets/1623983/96f1b58b-94f4-4df6-a81c-34e4f0342dc0" align="right" alt="Globe with flags" />

A Discord bot that translates messages in a Discord server (guild) using country flag reactions, the `/translate`
command, and message commands, powered by .NET and [Discord.Net](https://github.com/discord-net/Discord.Net).

It supports the following translation providers, all of which are disabled by default, in this order of priority:

1. [Azure Translator](https://azure.microsoft.com/en-us/products/ai-foundry/tools/translator) (has a free tier)
2. [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) (free and open-source)

If a provider fails to translate, the bot falls back to the next enabled provider. At least one provider must be
enabled or the app will exit.

## Features

### Country flag reactions

React to a message with a country flag emoji to translate it to that country's language. The bot replies to the
message with the translation, then deletes the reply and removes your reaction after 90 seconds. If the translation
fails, the bot posts an error reply that is deleted after 15 seconds.

### `/translate` slash command

Translate any text and post the result in the channel. The `to` and `text` options are required. If you leave out
`from`, the source language is detected automatically.

### `Translate (Auto)` message command

Right-click a message (or long-press on mobile), then select _Apps_ > _Translate (Auto)_ to translate it to the language
set in your Discord settings. Only you can see the translation.

### `Translate To...` message command

Right-click a message (or long-press on mobile), then select _Apps_ > _Translate To..._ and pick a language in the form
that opens. Only you see the translation unless you tick _Share in channel_, which posts it as a reply to the original
message.

### Limitations

* The `/translate` and `Translate To...` commands only use the first enabled translation provider. Their language
  choices are limited to 25 of that provider's supported languages because of Discord's limit on the number of
  choices.
* Messages sent by the bot can't be translated.
* Each user can request up to 5 translations every 30 seconds. Flag reactions beyond this limit are ignored.

## Requirements

### Create a Discord bot

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a new application with
   the name you want the bot to have.
2. Go to the "Bot" page in the sidebar and click "Reset Token" to get the bot token. Save it somewhere safe, as you
   can't view it again. Turn off "Public Bot" if you don't want other people to add the bot to their servers.
3. On the same page, enable "Message Content Intent" under _Privileged Gateway Intents_.
4. Go to the "OAuth2" page in the sidebar and use the URL generator. Select the `bot` scope and the `Send Messages`,
   `Manage Messages`, and `Read Message History` permissions.
5. Open the generated URL in your browser to add the bot to your Discord server.

### Optional: Run LibreTranslate

1. See the [LibreTranslate repository](https://github.com/LibreTranslate/LibreTranslate) for how to run it with Docker.
2. To keep the language models between restarts, mount a named volume at `/home/libretranslate/.local`.

## Development

1. Configure the [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) file for
   `src/DiscordTranslationBot` with the required settings. Example below:

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

1. Build a Docker image by running `docker build -t discordtranslationbot -f ./src/DiscordTranslationBot/Dockerfile .`
   from the repository root.
2. Run a container with `docker run --env-file <file> discordtranslationbot`, where the file sets the following
   environment variables:

```
Discord__BotToken=
TranslationProviders__AzureTranslator__Enabled=true
TranslationProviders__AzureTranslator__ApiUrl=https://api.cognitive.microsofttranslator.com
TranslationProviders__AzureTranslator__Region=
TranslationProviders__AzureTranslator__SecretKey=
TranslationProviders__LibreTranslate__Enabled=true
TranslationProviders__LibreTranslate__ApiUrl=http://libretranslate:5000
```

_Set `TranslationProviders__<ProviderName>__Enabled` to `true` for each provider you want to enable. An enabled
provider must have all of its settings configured or the app will exit with an error._

## Telemetry

The app logs information, warnings, and errors, and collects metrics and traces for performance and troubleshooting. It
doesn't log the contents of messages.

To export logs, metrics, and traces using the OpenTelemetry protocol, enable the following setting:

```
Telemetry__Enabled=true
```

Then configure the OpenTelemetry exporter. Example environment variables:

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
