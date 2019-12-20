# FPL bot
A slackbot for Blank FPL league

[![Build](https://github.com/fplbot/fplbot/workflows/CI/badge.svg)](https://github.com/fplbot/fplbot/actions)

[![Release](https://github.com/fplbot/fplbot/workflows/Release/badge.svg)](https://github.com/fplbot/fplbot/actions)

# Contributing
Requirements: .NET Core 3.1 SDK: https://dotnet.microsoft.com/download/dotnet-core/3.1

..or using docker:
- see `/src/buildimage.sh` and `runcontainer.sh` using the .NET Core docker images

*Configuration*   
Add a appsettings.Local.json file (is in gitignore) with contents:

``` 
{
  "Slackbot_SlackApiKey_SlackApp": "token1",
  "Slackbot_SlackApiKey_BotUser": "token2",
  h
  "fpl" : {
    "login" : "username",
    "password : "pwd"
  }
}
```

            
Or: Add environment variables (double underscores mimic nested props in the json config):

```
export Slackbot_SlackApiKey_SlackApp=token1
export Slackbot_SlackApiKey_BotUser=token2
export fpl__login=username
export fpl__password=pwd 
```

*Build scripts*   
The project is using [Nuke](http://www.nuke.build/) to setup builds.

It can be run via the build scripts: `/build/build.sh` or `/build/build.ps1`   

or using the Nuke CLI:    

Install: `$ dotnet tool install Nuke.GlobalTool -g`   
Run: `$ nuke`   


# Hosting
The Blank instance:

* Heroku: https://dashboard.heroku.com/apps/blank-fplbot/
* Slack-app: https://api.slack.com/apps/AREFP62B1

# Artifacts

### Fpl.Client

<img src="https://raw.githubusercontent.com/fplbot/fplbot/fbde22a8f0093ed3a91c972841bbb7ef7eaf90b6/src/Fpl.Client/images/fpl.png" height="25" width="25" align="left">&nbsp; A .NET Standard 2.0 library for integration to the Fantasy Premier League APIs

![Nuget](https://img.shields.io/nuget/v/Fpl.Client?style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/Fpl.Client?style=for-the-badge)

Install:
```
$ dotnet add package Fpl.Client
```

Register via DI providing credentials to a user of the fantasy premier league:
```csharp
services.AddFplApiClient(c =>
{
    c.Login = "youremail@premierleague.com";
    c.Password = "yourpassword@premierleague.com"
});
```

Or if you have the credentials in any other registered .NET Core configuration provider:
```csharp
services.AddFplApiClient(hostContext.Configuration);
```

### Fplbot docker image

[![dockeri.co](https://dockeri.co/image/fplbot/fplbot)](https://hub.docker.com/r/fplbot/fplbot)

Install:
```
 docker pull fplbot/fplbot:latest
```

Run:
```
 docker run --rm \
 -e fpl__login=$fpl__login \
 -e fpl__password=$fpl__password \
 -e Slackbot_SlackApiKey_SlackApp=$Slackbot_SlackApiKey_SlackApp \
 -e Slackbot_SlackApiKey_BotUser=$Slackbot_SlackApiKey_BotUser \
 fplbot/fplbot
 ```


# Slack app setup

* Create a Slack app
* Provide the following scopes:
  * `chat:write:bot` ("Send messages as fplbot")
* Add a bot user, which will lead to the `bot` scope being added:
  * `bot` ("Add the ability for people to direct message or mention @fplbot")


<hr>

_FplBot is a [Blank.no](https://blank.no) project_