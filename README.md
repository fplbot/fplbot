# FPL bot
A slackbot for Blank FPL league

[![Build](https://github.com/blankoslo/fplbot/workflows/CI/badge.svg)](https://github.com/blankoslo/fplbot/actions)

# Contributing
Requirements: .NET Core 3.1 SDK: https://dotnet.microsoft.com/download/dotnet-core/3.1

..or using docker:
- see `/src/buildimage.sh` and `runcontainer.sh` using the .NET Core docker images

Local dev time configuration:
    
Add a appsettings.Local.json file (is in gitignore) with contents:

``` 
{
  "Slackbot_SlackApiKey_SlackApp": "token1",
  "Slackbot_SlackApiKey_BotUser": "token2",
  
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



# Hosting
The Blank instance is hosted in Heroku as a docker container. See `heroku.yml` for details.
Heroku project: https://dashboard.heroku.com/apps/blank-fplbot/