FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
ARG INFOVERSION="0.666"
ARG VERSION="1.0.666"
RUN dotnet publish FplBot -o /app/out -c Release \
    /p:Version=$VERSION /p:InformationalVersion=$INFOVERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=build /app/out .

FROM base AS web
CMD ["dotnet", "FplBot.dll", "--services", "WebApi"]

FROM base AS eventhandler
CMD ["dotnet", "FplBot.dll", "--services", "EventHandlers"]

FROM base AS eventpublisher
CMD ["dotnet", "FplBot.dll", "--services", "EventPublishers"]

FROM base AS indexer
CMD ["dotnet", "FplBot.dll", "--services", "SearchIndexer"]
