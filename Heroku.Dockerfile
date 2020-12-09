FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/Directory.Build.targets Directory.Build.targets
COPY /src/FplBot.WebApi/FplBot.WebApi.csproj FplBot.WebApi/FplBot.WebApi.csproj
COPY /src/Fpl.Client/Fpl.Client.csproj ./Fpl.Client/Fpl.Client.csproj
COPY /src/Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj
COPY /src/FplBot.Messaging.Contracts/FplBot.Messaging.Contracts.csproj FplBot.Messaging.Contracts/FplBot.Messaging.Contracts.csproj
COPY /src/ext/Slackbot.Net.SlackClients.Http/Slackbot.Net.SlackClients.Http.csproj ./ext/Slackbot.Net.SlackClients.Http/Slackbot.Net.SlackClients.Http.csproj
COPY /src/ext/Slackbot.Net.Endpoints/Slackbot.Net.Endpoints.csproj ./ext/Slackbot.Net.Endpoints/Slackbot.Net.Endpoints.csproj
COPY /src/ext/Slackbot.Net.Shared/Slackbot.Net.Shared.csproj ./ext/Slackbot.Net.Shared/Slackbot.Net.Shared.csproj


RUN dotnet restore FplBot.WebApi

# Copy everything else
COPY /src/FplBot.WebApi/ FplBot.WebApi/
COPY /src/Fpl.Client/ Fpl.Client/
COPY /src/Slackbot.Net.Extensions.FplBot/ Slackbot.Net.Extensions.FplBot/
COPY /src/FplBot.Messaging.Contracts/ FplBot.Messaging.Contracts/
COPY /src/ext/Slackbot.Net.SlackClients.Http/ ./ext/Slackbot.Net.SlackClients.Http/
COPY /src/ext/Slackbot.Net.Endpoints/ ./ext/Slackbot.Net.Endpoints/
COPY /src/ext/Slackbot.Net.Shared/ ./ext/Slackbot.Net.Shared/

# Publish
RUN dotnet publish FplBot.WebApi/FplBot.WebApi.csproj -c Release -o /app/out/fplbot-webapi

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot-webapi . 