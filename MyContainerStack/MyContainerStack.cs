using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.App;
using Pulumi.Docker;
using Pulumi.AzureNative.OperationalInsights;

class MyContainerStack : Stack
{
    public MyContainerStack()
    {
        // 1. Setup Resource Group and Log Analytics Workspace (Required for ACA)
        var resourceGroup = new ResourceGroup("rg-container");

        var workspace = new Workspace("log-workspace", new WorkspaceArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new WorkspaceSkuArgs { Name = WorkspaceSkuNameEnum.PerGB2018 },
        });

        // 2. Create the Azure Container Apps Environment
        var containerAppEnv = new ManagedEnvironment("aca-env", new ManagedEnvironmentArgs
        {
            ResourceGroupName = resourceGroup.Name,
            // Link to the Log Analytics Workspace
            AppLogsConfiguration = new AppLogsConfigurationArgs
            {
                Destination = "log-analytics",
                LogAnalyticsConfiguration = new LogAnalyticsConfigurationArgs
                {
                    CustomerId = workspace.CustomerId,
                    SharedKey = workspace.PrimarySharedKey,
                }
            }
        });

        // 3. Create Azure Container Registry (ACR) and Credentials
        var registry = new Registry("acr", new RegistryArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs { Name = "Basic" },
            AdminUserEnabled = true, // Enable admin user for Pulumi access
        });

        var credentials = Output.Tuple(resourceGroup.Name, registry.Name)
            .Apply(t => ListRegistryCredentials.InvokeAsync(new ListRegistryCredentialsArgs
            {
                ResourceGroupName = t.Item1,
                RegistryName = t.Item2
            }));

        // 4. Build and Push the Docker Image
        var myImage = new Image("node-app-image", new ImageArgs
        {
            ImageName = Output.Format($"{registry.LoginServer}/{registry.Name}:v1.0.0"),
            Build = new DockerBuildArgs
            {
                Context = "./app", // Path to the folder containing your Dockerfile and JS code
            },
            Registry = new RegistryArgs
            {
                Server = registry.LoginServer,
                Username = credentials.Apply(c => c.Username!),
                Password = credentials.Apply(c => c.Passwords[0].Value!)
            }
        });

        // 5. Deploy to Azure Container App (ACA)
        var containerApp = new ContainerApp("node-app", new ContainerAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            ManagedEnvironmentId = containerAppEnv.Id,
            Configuration = new ConfigurationArgs
            {
                Ingress = new IngressArgs // Enable public access to the app
                {
                    External = true,
                    TargetPort = 8080, // Port defined in server.js and Dockerfile
                    Transport = "Auto",
                }
            },
            Template = new TemplateArgs
            {
                Containers =
                {
                    new ContainerArgs
                    {
                        Name = "node-container",
                        Image = myImage.ImageName,
                        Cpu = 0.5,
                        Memory = "1.0Gi"
                    }
                }
            }
        });

        // Export the public URL
        this.Url = containerApp.Configuration.Apply(c => $"https://{c.Ingress.Fqdn}");
    }

    [Output]
    public Output<string> Url { get; set; }
}
