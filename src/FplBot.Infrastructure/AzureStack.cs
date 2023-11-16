using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Resources;

public class AzureStack : Stack
{
    public AzureStack()
    {
        var stack = Pulumi.Deployment.Instance.StackName.ToLower(); // "test" or "prod"

        var resourceGroup = new ResourceGroup($"fplbot-{stack}");

        var storageAccount = new StorageAccount($"stfplbot{stack}", new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new Pulumi.AzureNative.Storage.Inputs.SkuArgs
            {
                Name = Pulumi.AzureNative.Storage.SkuName.Standard_LRS
            },
            Kind = Pulumi.AzureNative.Storage.Kind.StorageV2,
        });

        var appServicePlan = new AppServicePlan($"pl-{stack}-linux-asp", new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "Linux",
            Sku = new SkuDescriptionArgs
            {
                Tier = "Dynamic",
                Name = "Y1"
            }
        });

        var appInsights = new Component($"ai-{stack}-fplbot", new ComponentArgs
        {
            ApplicationType = ApplicationType.Web,
            Kind = "web",
            ResourceGroupName = resourceGroup.Name,
        });

        var storageAccountConnectionStr = GetConnectionString(resourceGroup.Name, storageAccount.Name);

        var config = new Config();

        var app = new WebApp($"fn-{stack}-fplbot", new WebAppArgs
        {
            Kind = "functionapp",
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs
                    {
                        Name = "FUNCTIONS_WORKER_RUNTIME",
                        Value = "dotnet-isolated",
                    },
                    new NameValuePairArgs
                    {
                        Name = "FUNCTIONS_EXTENSION_VERSION",
                        Value = "~4",
                    },
                    new NameValuePairArgs
                    {
                        Name = "linuxFxVersion",
                        Value= "DOTNET|8.0"
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITE_RUN_FROM_PACKAGE",
                        Value = "1",
                    },
                    new NameValuePairArgs
                    {
                        Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                        Value = Output.Format($"InstrumentationKey={appInsights.InstrumentationKey}"),
                    },
                    new NameValuePairArgs
                    {
                        Name = "AzureWebJobsStorage",
                        Value = storageAccountConnectionStr
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING",
                        Value = storageAccountConnectionStr
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITE_CONTENTSHARE",
                        Value = $"fn-{stack}-fplbot"
                    },
                    new NameValuePairArgs
                    {
                        Name = "DOTNET_ENVIRONMENT",
                        Value = stack == "prod" ? "Production" : "Test"
                    },
                    new NameValuePairArgs
                    {
                        Name = "NSB_LICENSE",
                        Value = config.RequireSecret("NSB_LICENSE")
                    },
                    new NameValuePairArgs
                    {
                        Name = "ENDPOINT_NAME",
                        Value = stack == "prod" ? "fplbot.functions.production" : "fplbot.functions.test"
                    },
                    new NameValuePairArgs
                    {
                        Name = "AzureWebJobsServiceBus",
                        Value = config.RequireSecret("ASB_CONNECTIONSTRING")
                    },
                    new NameValuePairArgs
                    {
                        Name = "SlackWebHookUrl",
                        Value = config.RequireSecret("SlackWebHookUrl")
                    },
                    new NameValuePairArgs()
                    {
                        Name ="SlackToken_FplBot_Workspace",
                        Value = config.RequireSecret("SlackToken_FplBot_Workspace")
                    },
                    new NameValuePairArgs
                    {
                        Name = "AZURE_STORAGE_CONNECTIONSTRING",
                        Value = storageAccountConnectionStr
                    }
                }
            },
        });

        Endpoint = Output.Format($"https://{app.DefaultHostName}/api/");
        ResourceGroupName = Output.Format($"{resourceGroup.Name}");
        FunctionName = Output.Format($"{app.Name}");
    }

    private static Output<string> GetConnectionString(Input<string> resourceGroupName, Input<string> accountName)
    {
        var storageAccountKeys = Output.All(resourceGroupName, accountName).Apply(t => ListStorageAccountKeys.InvokeAsync(new ListStorageAccountKeysArgs {ResourceGroupName = t[0], AccountName = t[1]}));
        return storageAccountKeys.Apply(keys => Output.Format($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={keys.Keys[0].Value};EndpointSuffix=core.windows.net"));
    }

    [Output] public Output<string> Endpoint { get; set; }
    [Output] public Output<string> ResourceGroupName { get; set; }
    [Output] public Output<string> FunctionName { get; set; }
}
