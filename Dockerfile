FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/ .
RUN dotnet restore FplBot.WebApi

# Publish
ARG INFOVERSION="0.666"
ARG VERSION="1.0.666"
RUN echo "Infoversion: $INFOVERSION"

RUN dotnet publish FplBot.WebApi/FplBot.WebApi.csproj -c Release /p:Version=$VERSION /p:InformationalVersion=$INFOVERSION -o /app/out/fplbot-webapi

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot-webapi . 
ENTRYPOINT ["dotnet", "FplBot.WebApi.dll"]