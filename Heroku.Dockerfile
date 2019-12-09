FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/FplBot.sln FplBot.sln
COPY /src/FplBot.ConsoleApps/FplBot.ConsoleApps.csproj FplBot.ConsoleApps/FplBot.ConsoleApps.csproj
COPY /src/FplBot.Tests/FplBot.Tests.csproj FplBot.Tests/FplBot.Tests.csproj

RUN dotnet restore FplBot.sln

# Copy everything else
COPY /src/FplBot.ConsoleApps/ FplBot.ConsoleApps/

# Publish
RUN dotnet publish FplBot.ConsoleApps/FplBot.ConsoleApps.csproj -c Release -o /app/out/fplbot

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot .