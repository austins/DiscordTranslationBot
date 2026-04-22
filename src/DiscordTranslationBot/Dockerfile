###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:0191ff386e93923edf795d363ea0ae0669ce467ada4010b370644b670fa495c1 AS build

# Copy source files.
# The build should be run with the context at the root of the solution in order to copy necessary files.
WORKDIR /app
COPY .editorconfig Directory.Build.props ./
COPY ./src/DiscordTranslationBot ./src/DiscordTranslationBot 

# Restore dependencies.
RUN dotnet restore ./src/DiscordTranslationBot

# Publish app.
RUN dotnet publish ./src/DiscordTranslationBot --no-restore -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:60eb031b554df75a4b9f358290a2fa15d8961a3bc79b47bb34a00e31f7b78c69

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
