FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY /src/FplBot.WebApi/FplBot.WebApi.csproj FplBot.WebApi/FplBot.WebApi.csproj

RUN dotnet restore FplBot.WebApi

# Copy everything else
COPY /src/FplBot.WebApi/ FplBot.WebApi/

# Publish
RUN dotnet publish FplBot.WebApi/FplBot.WebApi.csproj -c Release -o /app/out/fplbot-webapi

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot-webapi . 