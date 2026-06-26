###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:940f919ae84dd92ccd4aab7686fa5b777870b006c9360351039e16bcaad73d89 AS build

# Copy source files.
# The build should be run with the context at the root of the solution in order to copy necessary files.
WORKDIR /app
COPY --link .editorconfig nuget.config Directory.Build.props ./
COPY --link ./src/DiscordTranslationBot ./src/DiscordTranslationBot 

# Restore dependencies.
RUN dotnet restore ./src/DiscordTranslationBot --locked-mode

# Publish app.
RUN dotnet publish ./src/DiscordTranslationBot --no-restore -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra@sha256:c6cc46344890c33281193591723756c6827db7e17987beed6ff2dd2a5279a5bc

# Enable globalization.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

# Copy published build.
WORKDIR /app
COPY --link --from=build /app/out .

# Expose port.
EXPOSE 8080

# Configure healthcheck.
RUN apk add --no-cache curl
HEALTHCHECK --start-period=2m --start-interval=5s --interval=5m CMD curl --fail http://localhost:8080/_health || exit 1

# Configure running the app.
USER $APP_UID
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
