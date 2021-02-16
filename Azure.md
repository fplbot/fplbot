## Azure deploy details

The following has been run to be able to change infrastructure in Azure:
1) Create a service principal
2) Provide contributor role in the fplbot resource groups, `fplbot-test` / `fplbot-prod`


- _subId_:  `6dbdcf53-4893-4cde-b62c-d51aadb063e0`
- _nameOfPrincipal_ : `fplbot-deployer`

```shell
$ az ad sp create-for-rbac --name {nameOfPrincipal} --sdk-auth --role contributor --scopes /subscriptions/{subId}/resourceGroups/fplbot-test
```

Disregard credentials from output, as they will be invalid from the next command:

```shell
az ad sp create-for-rbac --name {nameOfPrincipal} --sdk-auth --role contributor --scopes /subscriptions/{subId}/resourceGroups/fplbot-prod
```

The output of the last run has been added as `AZURE_CREDENTIALS` secret in GitHub Secrets.



## The FplBot Azure Function

### Local dev

To run the function locally, install the _[Azure Function Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=macos%2Ccsharp%2Cbash#v2)_ via
 ```
$ brew tap azure/functions
$ brew install azure-functions-core-tools@3
```

Create a `local.settings.json` file in the `FplBot.Functions` directory:

```json
{
    "IsEncrypted": false,
    "Values": {
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "ASB_CONNECTIONSTRING" : "<test-connectionstring>",
        "NSB_LICENSE" : "<license>(optional)",
        "DOTNET_ENVIRONMENT": "Development",
        "SlackWebHookUrl" : "<slack webhook-url>",
        "QueueName": "fplbot.functions.development.<your-machinename>"
    },
    "bindings": [
        {
            "queueName": "%QueueName%"
        }
    ]
}
```

### Endpoints & subscriptions

Azure Functions do not work with NServiceBus auto-install features (auto subscribe, auto create endpoints), so endpoints and subscriptions need to be handled manually using the Particular ASB tool (`asb-transport`, a .NET commandline tool).


```bash
# Create a new local dev endpoint
$ asb-transport endpoint create FplBot.Functions.Development.<your-machinename> -c "$ASB_CONNECTIONSTRING"
```


```bash
# Add subscribtion of a new event to the new local dev endpoint
$ asb-transport endpoint subscribe FplBot.Functions.Development.<your-machinename> SomeNewEvent -c "$ASB_CONNECTIONSTRING"
```

Then run `func start host` in the `FplBot.Functions` directory.


**NB!** Remember to setup subscriptions for test and production endpoints before release!