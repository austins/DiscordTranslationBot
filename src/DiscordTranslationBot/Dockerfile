###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:f271ed7d0fd9c5a7ed0acafed8a2bc978bb65c19dcd2eeea0415adef142ffc87 AS build

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
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:5e8dca92553951e42caed00f2568771b0620679f419a28b1335da366477d7f98

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
