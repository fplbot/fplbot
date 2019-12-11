# FPL bot
A slackbot for Blank FPL league

[![Build](https://github.com/blankoslo/fplbot/workflows/CI/badge.svg)](https://github.com/blankoslo/fplbot/actions)

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
The project is using [Nuke](http://www.nuke.build/) to run the build.   
Install: `$ dotnet tool install Nuke.GlobalTool -g`   
Run: `$ nuke`   



# Hosting
The Blank instance:

* Heroku: https://dashboard.heroku.com/apps/blank-fplbot/
* Slack-app: https://api.slack.com/apps/AREFP62B1
