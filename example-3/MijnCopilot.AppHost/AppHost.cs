var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
                     .RunAsEmulator();
var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

var auth0Domain = builder.AddParameter("auth0-domain");
var auth0ClientId = builder.AddParameter("auth0-client-id");
var auth0ClientSecret = builder.AddParameter("auth0-client-secret", secret: true);

var openAiEndpoint = builder.AddParameter("azure-openai-endpoint");
var openAiDeployment = builder.AddParameter("azure-openai-deployment");
var openAiKey = builder.AddParameter("azure-openai-key", secret: true);

var orleans = builder.AddProject<Projects.MijnCopilot_Orleans_Host>("orleans", launchProfileName: null)
                .WithReference(blobs)
                .WithReference(tables)
                .WaitFor(storage)
                .WithHttpEndpoint(port: 5010, name: "http", isProxied: false)
                .WithExternalHttpEndpoints()
                .WithUrlForEndpoint("http", url =>
                {
                    url.Url = "/dashboard";
                    url.DisplayText = "Orleans dashboard";
                })
                .WithEnvironment("AZUREOPENAI_ENDPOINT", openAiEndpoint)
                .WithEnvironment("AZUREOPENAI_DEPLOYMENT", openAiDeployment)
                .WithEnvironment("AZUREOPENAI_KEY", openAiKey);

var web = builder.AddProject<Projects.MijnCopilot_Web>("web", launchProfileName: null)
            .WaitFor(orleans)
            .WithReference(tables)
            .WaitFor(storage)
            .WithHttpEndpoint(port: 5080, name: "http", isProxied: false)
            .WithExternalHttpEndpoints()
            .WithEnvironment("AUTH0_DOMAIN", auth0Domain)
            .WithEnvironment("AUTH0_CLIENTID", auth0ClientId)
            .WithEnvironment("AUTH0_CLIENTSECRET", auth0ClientSecret);

if (builder.Configuration["MEDIATR_LICENSEKEY"] is { Length: > 0 } licenseKey)
{
    web.WithEnvironment("MEDIATR_LICENSEKEY", licenseKey);
}

foreach (var setting in new[] { "MIJNTHUIS_MCP", "MIJNSAUNA_MCP", "PHOTOCAROUSEL_MCP" })
{
    if (builder.Configuration[setting] is { Length: > 0 } value)
    {
        orleans.WithEnvironment(setting, value);
    }
}

builder.Build().Run();
