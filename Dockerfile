###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:0062dac82deeedd731b8b709c4e7fc6ce1060d376b00edabcb937fa27c2add1e AS build

# Publish app.
WORKDIR /app
COPY . .
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:c45e218d64af2d1f223b2ff0fd3ccb59f2c1a91232c4932bd4e70d2aadf5dc7c

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
HEALTHCHECK --interval=2m CMD curl --fail http://localhost:8080/_health || exit 1

# Configure running the app.
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
