###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:a9d99ca1f16b123abd916a9e95b4304c3d3e03e86781e24ca565119badd5cad6 AS build

# Publish app.
WORKDIR /app
COPY . .
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:5b2b4d6ef37864167055e4e215eb10bd77217148068f7c055c900ca22d14c945

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
HEALTHCHECK --interval=2m CMD curl --fail http://localhost:8080/_health || exit 1

# Configure running the app.
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
