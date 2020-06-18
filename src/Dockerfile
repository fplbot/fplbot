FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Directory.Build.targets Directory.Build.targets
COPY FplBot.sln ./FplBot.sln
COPY /FplBot.WebApi/FplBot.WebApi.csproj ./FplBot.WebApi/FplBot.WebApi.csproj
COPY /Fpl.Client/Fpl.Client.csproj ./Fpl.Client/Fpl.Client.csproj
COPY /Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj ./Slackbot.Net.Extensions.FplBot/Slackbot.Net.Extensions.FplBot.csproj

RUN dotnet restore FplBot.WebApi
RUN dotnet restore Fpl.Client


# Copy everything else and build
COPY . ./
RUN dotnet publish FplBot.WebApi/FplBot.WebApi.csproj -c Release -o /app/out/fplbot

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot .
ENTRYPOINT ["dotnet", "FplBot.WebApi.dll"]