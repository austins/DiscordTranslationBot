# Build and publish.
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /app
COPY . .
RUN dotnet publish DiscordTranslationBot -c Release

# Create runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

# Install cultures and disable the invariant mode.
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Configure healthcheck.
RUN apk add --no-cache curl
HEALTHCHECK CMD curl --fail http://localhost:8080/_health || exit 1

WORKDIR /app
COPY --from=build-env /app/DiscordTranslationBot/bin/Release/net8.0/publish .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
