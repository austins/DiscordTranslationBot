###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:2a2b9514e527951629ce5517b3169eee9d5af771bc52ea978e4662d9057b43e0 AS build

# Publish app.
WORKDIR /app
COPY . .
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:6188cf0bb2827d98424f9fa2d411686f95b54fb71d7ce4358df1e9044c29f083

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
