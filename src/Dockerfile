FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Directory.Build.targets Directory.Build.targets
COPY FplBot.sln ./FplBot.sln
COPY /FplBot.WebApi/FplBot.WebApi.csproj ./FplBot.WebApi/FplBot.WebApi.csproj
COPY /Fpl.Client/Fpl.Client.csproj ./Fpl.Client/Fpl.Client.csproj
COPY /Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj ./Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj
COPY /ext/Slackbot.Net.SlackClients.Http/Slackbot.Net.SlackClients.Http.csproj ./ext/Slackbot.Net.SlackClients.Http/Slackbot.Net.SlackClients.Http.csproj
COPY /ext/Slackbot.Net.Endpoints/Slackbot.Net.Endpoints.csproj ./ext/Slackbot.Net.Endpoints/Slackbot.Net.Endpoints.csproj
COPY /ext/Slackbot.Net.Shared/Slackbot.Net.Shared.csproj ./ext/Slackbot.Net.Shared/Slackbot.Net.Shared.csproj

RUN dotnet restore FplBot.WebApi
RUN dotnet restore Fpl.Client


# Copy everything else and build
COPY . ./
RUN dotnet publish FplBot.WebApi/FplBot.WebApi.csproj -c Release -o /app/out/fplbot

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot .
ENTRYPOINT ["dotnet", "FplBot.WebApi.dll"]