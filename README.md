# FPL bot
A slackbot for Blank FPL league

[![Build](https://github.com/fplbot/fplbot/workflows/CI/badge.svg)](https://github.com/fplbot/fplbot/actions)

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

### <img src="https://raw.githubusercontent.com/fplbot/fplbot/fbde22a8f0093ed3a91c972841bbb7ef7eaf90b6/src/Fpl.Client/images/fpl.png" height="14" width="14" > `Fpl.Client`

![Nuget](https://img.shields.io/nuget/v/Fpl.Client?style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/Fpl.Client?style=for-the-badge)


A separate library for integration to the Fantasy Premier League APIs



<hr>

_FplBot is a [Blank.no](https://blank.no) project_