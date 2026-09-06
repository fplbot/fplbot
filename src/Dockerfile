FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . .
ARG INFOVERSION="202609.06.0+local"
RUN dotnet publish FplBot -o /app/out -c Release \
    /p:InformationalVersion=$INFOVERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "FplBot.dll"]
