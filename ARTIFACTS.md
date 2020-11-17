## Artifacts

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
 -e fpl__login=fpl account username/email \
 -e fpl__password=fpl account password \
 -e REDIS_URL=redisconnstr \
 -e CLIENT_ID=slack app client_id \
 -e CLIENT_SECRET=slack app client_secret \
  fplbot/fplbot
 ```