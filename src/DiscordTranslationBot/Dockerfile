###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:48b443d87fe7426ce6a20554e87fdd6189143dc4aa5f43b501acf5fef6d42740 AS build

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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:55fa85c0075d8b48b4854af502ffbb305470564e9125c227da28b52cfec6dc92

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
