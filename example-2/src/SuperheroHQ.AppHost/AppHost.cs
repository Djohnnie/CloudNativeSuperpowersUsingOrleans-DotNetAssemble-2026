var builder = DistributedApplication.CreateBuilder(args);

// Redis backs the Orleans cluster membership table and the grain state.
// This is the only "infrastructure" the demo needs, and Aspire starts it for us.
var redis = builder.AddRedis("redis")
                   .WithRedisInsight();

// The Orleans cluster itself is not a container: Aspire models it as configuration
// that gets handed to every project that references it.
var orleans = builder.AddOrleans("superhero-hq")
                     .WithClustering(redis)
                     .WithGrainStorage("Default", redis)
                     .WithGrainStorage("heroes", redis)
                     .WithGrainStorage("villains", redis)
                     .WithGrainStorage("cities", redis);

// Each silo is its own resource with the same HTTP port as when started by hand.
// All three join the same Orleans cluster through Redis membership.
const int SiloCount = 3;
var silos = new List<IResourceBuilder<ProjectResource>>();

for (var instance = 1; instance <= SiloCount; instance++)
{
    var silo = builder.AddProject<Projects.SuperheroHQ_Silo>($"silo{instance}", launchProfileName: null)
                      .WithReference(orleans)
                      .WithArgs("--instance", $"{instance}")
                      .WithHttpEndpoint(port: 5000 + instance, name: "http", isProxied: false)
                      .WithExternalHttpEndpoints()
                      .WaitFor(redis)
                      .WithUrlForEndpoint("http", url =>
                      {
                          url.Url = "/dashboard";
                          url.DisplayText = "Orleans Dashboard";
                      });

    silos.Add(silo);
}

// Like example 1, the browser picks a silo; the web app proxies to it using
// Aspire service discovery rather than connecting to Orleans itself.
var web = builder.AddProject<Projects.SuperheroHQ_Web>("web", launchProfileName: null)
                 .WithHttpEndpoint(port: 5080, name: "http", isProxied: false)
                 .WithExternalHttpEndpoints()
                 .WaitFor(silos[0]);

foreach (var silo in silos)
{
    web.WithReference(silo);
}

// The guided console demo. It is started by hand from the Aspire dashboard so the
// story can be told at the right moment.
builder.AddProject<Projects.SuperheroHQ_Client>("demo")
       .WithReference(orleans.AsClient())
       .WaitFor(redis)
       .WithExplicitStart();

builder.Build().Run();
