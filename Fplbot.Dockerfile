FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/FplBot.sln FplBot.sln
COPY /src/FplBot.ConsoleApps/FplBot.ConsoleApps.csproj FplBot.ConsoleApps/FplBot.ConsoleApps.csproj
COPY /src/Fpl.Client/Fpl.Client.csproj ./Fpl.Client/Fpl.Client.csproj
COPY /src/FplBot.WebApi/FplBot.WebApi.csproj FplBot.WebApi/FplBot.WebApi.csproj
COPY /src/Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj

COPY /src/FplBot.Tests/FplBot.Tests.csproj FplBot.Tests/FplBot.Tests.csproj
COPY /src/FplBot.WebApi.Tests/FplBot.WebApi.Tests.csproj FplBot.WebApi.Tests/FplBot.WebApi.Tests.csproj

RUN dotnet restore FplBot.sln

# Copy everything else
COPY /src/Slackbot.Net.Extensions.FplBot/ Slackbot.Net.Extensions.FplBot/
COPY /src/FplBot.ConsoleApps/ FplBot.ConsoleApps/
COPY /src/Fpl.Client/ Fpl.Client/

# Publish
RUN dotnet publish FplBot.ConsoleApps/FplBot.ConsoleApps.csproj -c Release -o /app/out/fplbot

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot . 
FROM fplbot/fplbot 