###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:6e8997576d16a6d7b4e6ba7ac0956d3ae46cb7a376581c40eabd20fbc5c28b8d AS build

# Publish app.
WORKDIR /app
COPY . .
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:e22d879fb9a0fba3b2a6b15c88f005192c239e189061534c86643347b4630140

# Copy published build.
WORKDIR /app
COPY --from=build /app/out .

# Enable globalization.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache icu-data-full icu-libs

# Configure healthcheck.
RUN apk add --no-cache curl
HEALTHCHECK CMD curl --fail http://localhost:8080/_health || exit 1

# Configure running the app.
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
