FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY FplBot.sln ./FplBot.sln

COPY /FplBot.ConsoleApps/FplBot.ConsoleApps.csproj ./FplBot.ConsoleApps/FplBot.ConsoleApps.csproj
COPY /Fpl.Client/Fpl.Client.csproj ./Fpl.Client/Fpl.Client.csproj


RUN dotnet restore FplBot.ConsoleApps
RUN dotnet restore Fpl.Client


# Copy everything else and build
COPY . ./
RUN dotnet publish FplBot.ConsoleApps/FplBot.ConsoleApps.csproj -c Release -o /app/out/fplbot

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /fplbot
COPY --from=build-env /app/out/fplbot .
ENTRYPOINT ["dotnet", "FplBot.ConsoleApps.dll"]