###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS build

# Copy source files.
# The build should be run with the context at the root of the solution in order to copy necessary files.
WORKDIR /app
COPY --link .editorconfig nuget.config Directory.Build.props ./
COPY --link ./src/DiscordTranslationBot ./src/DiscordTranslationBot 

# Restore dependencies.
RUN dotnet restore ./src/DiscordTranslationBot

# Publish app.
# The commit SHA is passed in because .git isn't copied, so Source Link can't add it to the informational version.
ARG SOURCE_REVISION_ID
RUN dotnet publish ./src/DiscordTranslationBot --no-restore -c Release -o ./out -p:SourceRevisionId=$SOURCE_REVISION_ID

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine-extra@sha256:cd3d2caabadeef985cafe1cedd48f80357c2c72887ac3e74d48a1e70011277c9

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
HEALTHCHECK --start-period=2m --start-interval=5s --interval=5m CMD wget -q --spider http://localhost:8080/_health || exit 1

# Configure running the app.
USER $APP_UID
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
