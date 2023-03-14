FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env
WORKDIR /app

ARG PROJECT_NAME=DiscordTranslationBot

# Copy what we need
COPY . ./Directory.Build.props
COPY . ./$PROJECT_NAME
# Restore as distinct layers
RUN dotnet restore $PROJECT_NAME
# Build and publish a release
RUN dotnet publish $PROJECT_NAME -c Release -o ./publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine

# Install cultures
RUN apk add --no-cache icu-libs
# Disable the invariant mode
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY --from=build-env /app/publish .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
