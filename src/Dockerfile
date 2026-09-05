FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
ARG INFOVERSION="0.666"
ARG VERSION="1.0.666"
RUN dotnet publish FplBot -o /app/out -c Release \
    /p:Version=$VERSION /p:InformationalVersion=$INFOVERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "FplBot.dll"]
