# Example 3 - Mijn Copilot

## Run with Aspire

From this directory, start the AppHost:

```powershell
dotnet run --project MijnCopilot.AppHost
```

Aspire starts an Azurite storage emulator, the Orleans host and the web application.
The Aspire dashboard is at <https://localhost:17203>; the Orleans dashboard is at
<http://localhost:5010/dashboard>, and the web application is at
<http://localhost:5080>. From the CLI, use the Aspire login URL printed in the
terminal; in Visual Studio, set `MijnCopilot.AppHost` as the startup project to
open it in a browser. A running container runtime such as Docker Desktop or
Podman is required for Azurite.

Fill in the six `Parameters__*` entries in
`MijnCopilot.AppHost/Properties/launchSettings.json` for Auth0 and Azure OpenAI
before running. The three MCP endpoint entries and `MEDIATR_LICENSEKEY` are optional.
These settings are forwarded to the web app and Orleans host as their expected
environment variables. **Do not commit real credentials in `launchSettings.json`.**
For a safer alternative, remove the six blank `Parameters__*` entries from the
launch profile and configure AppHost user secrets instead (empty environment
variables would override the secrets):

```powershell
dotnet user-secrets set "Parameters:auth0-domain" "<your Auth0 domain>" --project MijnCopilot.AppHost
dotnet user-secrets set "Parameters:auth0-client-id" "<your Auth0 client ID>" --project MijnCopilot.AppHost
dotnet user-secrets set "Parameters:auth0-client-secret" "<your Auth0 client secret>" --project MijnCopilot.AppHost
dotnet user-secrets set "Parameters:azure-openai-endpoint" "<your Azure OpenAI endpoint>" --project MijnCopilot.AppHost
dotnet user-secrets set "Parameters:azure-openai-deployment" "<your deployment name>" --project MijnCopilot.AppHost
dotnet user-secrets set "Parameters:azure-openai-key" "<your Azure OpenAI key>" --project MijnCopilot.AppHost
```

Optional MCP endpoints (`MIJNTHUIS_MCP`, `MIJNSAUNA_MCP` and `PHOTOCAROUSEL_MCP`)
can also be supplied through the AppHost environment or user secrets.

The host and web app use Aspire's `ConnectionStrings:tables` and
`ConnectionStrings:blobs` settings for clustering and persistence. When started
directly instead, they continue to accept `BLOB_CONNECTION_STRING` as before.
