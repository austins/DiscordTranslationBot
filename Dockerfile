###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:07cb8622ca6c4d7600b42b2eccba968dff4b37d41b43a9bf4bd800aa02fab117 AS build

# Publish app.
WORKDIR /app
COPY . .
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:09bd97b2f28a084168e6d4e2f1c32385753cd7166c8d87af71a0dba2919d7aa2

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
