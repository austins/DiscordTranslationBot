###################################################
## Build stage.
###################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine@sha256:80bcbcdd7b0f66932911c1b3df5dd0eaecab3acc1fd37688cc3e81704d7422df AS build

# Publish app.
WORKDIR /app
COPY . .
RUN dotnet publish ./src/DiscordTranslationBot -c Release -o ./out

###################################################
## Runtime image creation stage.
###################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:2659ae38d78e034da815755ff15ba634ef180885c3d43b5512e75cd451a0a1ae

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
