// Example 1 runs on nothing at all: no database, no Redis, no container runtime.
// Cluster membership and grain state both live in memory, so the whole demo is a
// single "dotnet run". Aspire is here purely to start the processes and to give
// one place for logs, traces and metrics.
//
// Note that this AppHost does NOT call builder.AddOrleans(). Aspire's Orleans
// integration is great, but its development clustering never tells a silo where
// the primary silo is, so every replica would quietly become the primary of its
// own one-silo cluster. Example 2 shows the real integration, backed by Redis.
// Here we model three silos as three resources and let Orleans' own localhost
// clustering - with an explicit, fixed primary - form a single cluster.

// Silos are numbered 1..SiloCount. Silo 1 is the primary. Change this number to
// scale the cluster; nothing in the grains changes.
const int SiloCount = 3;

var builder = DistributedApplication.CreateBuilder(args);

var silos = new List<IResourceBuilder<ProjectResource>>();

for (var instance = 1; instance <= SiloCount; instance++)
{
    // launchProfileName: null keeps the project's launchSettings ports out of the
    // way, so the same project can be started several times side by side.
    // The HTTP port is pinned to 500N, exactly like running the silo by hand, so
    // the dashboard of silo N is always on http://localhost:500N/dashboard.
    var silo = builder.AddProject<Projects.ParcelTracker_Silo>($"silo{instance}", launchProfileName: null)
                      .WithArgs("--instance", $"{instance}")
                      .WithHttpEndpoint(port: 5000 + instance, name: "http", isProxied: false)
                      .WithExternalHttpEndpoints()
                      .WithUrlForEndpoint("http", url =>
                      {
                          url.Url = "/dashboard";
                          url.DisplayText = $"Orleans dashboard (silo-{instance})";
                      });

    // The primary silo owns the in-memory membership table, so it has to be up
    // before the others can join it.
    if (instance > 1)
    {
        silo.WaitFor(silos[0]);
    }

    silos.Add(silo);
}

// The control panel. It is not an Orleans client: it proxies to whichever silo
// you pick in the browser, which is exactly what makes "the grain lives
// somewhere else" visible.
var web = builder.AddProject<Projects.ParcelTracker_Web>("web", launchProfileName: null)
                 .WithHttpEndpoint(port: 5080, name: "http", isProxied: false)
                 .WaitFor(silos[0])
                 .WithExternalHttpEndpoints();

foreach (var silo in silos)
{
    web.WithReference(silo);
}

builder.Build().Run();
