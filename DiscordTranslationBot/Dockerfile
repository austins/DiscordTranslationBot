FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine

# Install cultures
RUN apk add --no-cache icu-libs
# Disable the invariant mode
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY --from=build-env /app/out .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "DiscordTranslationBot.dll"]
