# Discord Translation Bot

A Discord bot that allows translations of messages in a Discord server (guild) using country flags, powered by .NET
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

1. Follow the steps
   to [create and run a LibreTranslate Docker container](https://github.com/LibreTranslate/LibreTranslate#run-with-docker=).
2. Optionally, you can mount the volumes `/root/.local/share/LibreTranslate` and `/root/.local/share/argos-translate` to
   persist the language models. This is good for production.

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
      "SecretKey": "",
      "Region": ""
    },
    "LibreTranslate": {
      "Enabled": true,
      "ApiUrl": "http://localhost:5000"
    }
  }
}
```

2. Make sure that you've created a Discord bot and have configured at least one translation provider using the steps
   above.

## Deployment

1. Build a Docker image with `docker build -t DiscordTranslationBot .` in the directory that contains the `Dockerfile`.
2. Create and run a container with `docker run DiscordTranslationBot` and the following environment variables
   configured. Make sure that you've created a Discord bot and have configured at least one translation provider using
   the steps
   above. Example below:

```
Discord__BotToken=
TranslationProviders__AzureTranslator__Enabled=true
TranslationProviders__AzureTranslator__ApiUrl=https://api.cognitive.microsofttranslator.com
TranslationProviders__AzureTranslator__SecretKey=
TranslationProviders__AzureTranslator__Region=
TranslationProviders__LibreTranslate__Enabled=true
TranslationProviders__LibreTranslate__ApiUrl=http://localhost:5000
```

_All translation providers are disabled by default. Set the `TranslationProviders__ProviderName__Enabled` config setting
to `true` for those you which to
enable. When a provider is enabled, you must provide the related config settings for the provider or the app will exit
with an error._

## License

See LICENSE file in this repo.

This app has been developed by Austin S.
