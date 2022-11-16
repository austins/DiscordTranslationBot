# Discord Translation Bot

A Discord bot that allows translations of messages in a Discord server (guild) using country flags, powered by .NET
and [Discord.Net](https://github.com/discord-net/Discord.Net).

It supports the following translation providers that run in the following order:

1. [Azure Translator](https://azure.microsoft.com/en-us/services/cognitive-services/translator/) - This is optional. If
   it fails to provide a translation, the bot will use the next provider.
2. [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) - This is required as a fallback, such as when
   request limits have been reached for the previous providers.

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

### Run LibreTranslate

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
      "ApiUrl": "https://api.cognitive.microsofttranslator.com",
      "SecretKey": "",
      "Region": ""
    },
    "LibreTranslate": {
      "ApiUrl": "http://localhost:5000"
    }
  }
}
```

2. Make sure that you've created a Discord bot and run LibreTranslate using the steps above.

If you want to disable the Azure Translator provider, simply omit the config section for it
within `TranslationProviders` like so:

```json
  "TranslationProviders": {
    "LibreTranslate": {
      "ApiUrl": "http://localhost:5000"
    }
  }
```

## Deployment

1. Build a Docker image and create a container with the following required environment variables configured. Example
   below:

```
Discord__BotToken=
TranslationProviders__AzureTranslator__ApiUrl=https://api.cognitive.microsofttranslator.com
TranslationProviders__AzureTranslator__SecretKey=
TranslationProviders__AzureTranslator__Region=
TranslationProviders__LibreTranslate__ApiUrl=http://localhost:5000
```

2. Make sure that you've created a Discord bot and run LibreTranslate using the steps above.

If you want to disable the Azure Translator provider, don't set the environment variables
for `TranslationProviders__AzureTranslator`.

## License

See LICENSE file in this repo.

This app has been developed by Austin S.
