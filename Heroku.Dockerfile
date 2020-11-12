FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/Directory.Build.targets Directory.Build.targets
COPY /src/FplBot.WebApi/FplBot.WebApi.csproj FplBot.WebApi/FplBot.WebApi.csproj
COPY /src/Fpl.Client/Fpl.Client.csproj ./Fpl.Client/Fpl.Client.csproj
COPY /src/Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj
COPY /src/ext/Slackbot.Net.SlackClients.Http/Slackbot.Net.SlackClients.Http.csproj ./ext/Slackbot.Net.SlackClients.Http/Slackbot.Net.SlackClients.Http.csproj
COPY /src/ext/Slackbot.Net.Endpoints/Slackbot.Net.Endpoints.csproj ./ext/Slackbot.Net.Endpoints/Slackbot.Net.Endpoints.csproj

RUN dotnet restore FplBot.WebApi
RUN dotnet restore Fpl.Client
RUN dotnet restore Slackbot.Net.Extensions.FplBot

# Copy everything else
COPY /src/FplBot.WebApi/ FplBot.WebApi/
COPY /src/Fpl.Client/ Fpl.Client/
COPY /src/Slackbot.Net.Extensions.FplBot/ Slackbot.Net.Extensions.FplBot/
COPY /src/ext/Slackbot.Net.SlackClients.Http/ ./ext/Slackbot.Net.SlackClients.Http/
COPY /src/ext/Slackbot.Net.Endpoints/ ./ext/Slackbot.Net.Endpoints/

# Publish
RUN dotnet publish FplBot.WebApi/FplBot.WebApi.csproj -c Release -o /app/out/fplbot-webapi

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot-webapi . 