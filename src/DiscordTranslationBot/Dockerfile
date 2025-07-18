###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:a4eb48407ea8a1a4af92ee6630ec91af216365fdf45e7f08e1b5f4ce602407f4 AS build

# Copy source files.
# The build should be run with the context at the root of the solution in order to copy necessary files.
WORKDIR /app
COPY .editorconfig Directory.Build.props ./
COPY ./src/DiscordTranslationBot ./src/DiscordTranslationBot 

# Publish app.
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:89a7a398c5acaa773642cfabd6456f33e29687c1529abfaf068929ff9991cb66

# Copy published build.
WORKDIR /app
COPY --from=build /app/out .

# Enable globalization.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache icu-data-full icu-libs

# Expose port.
EXPOSE 8080

# Configure healthcheck.
RUN apk add --no-cache curl
HEALTHCHECK --start-period=2m --start-interval=5s --interval=5m CMD curl --fail http://localhost:8080/_health || exit 1

# Configure running the app.
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
