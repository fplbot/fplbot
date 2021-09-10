# Contributing
### Requirements:
- .NET Core 5.0 SDK (5.0.100 or above): https://dotnet.microsoft.com/download/dotnet/5.0
- Or Docker

### Using docker:
- See `/src/buildimage.sh` using the .NET docker images

### Dev configuration

_Using files_

Add a `appsettings.Local.json` file (is in gitignore) with contents:

```json
{
  "CLIENT_ID" : "A slack app clientId",
  "CLIENT_SECRET" : "A slack app clientSecret",
  "REDIS_URL" : "A connection string to a Redis instance",
  "fpl" : {
    "login" : "username",
    "password" : "password"
  }
}
```

_Using ENV variables_
Set the same config using environment variables. A special mention for .NET config: _double underscores mimic nested props in the json config_:

```
export fpl__login=username
export fpl__password=pwd
```

# Slack app setup

* Create a Slack app
* Provide the following granuar scopes:
  * _TODO_: list all scopes
* Enable event subscriptions, and subscribe to:
  * _TODO_: list all subscriptions
* Enable interactive payloads
  * For app_home features, sending a app_home view, receiving fplbot config payloads from the app_home


# Environments

### Test
* Heroku: https://dashboard.heroku.com/apps/blank-fplbot-test/
* Slack-app: https://api.slack.com/apps/ATDD4SFQ9/

### Production:

* Heroku: https://dashboard.heroku.com/apps/blank-fplbot/
* Slack-app: https://api.slack.com/apps/AREFP62B1
