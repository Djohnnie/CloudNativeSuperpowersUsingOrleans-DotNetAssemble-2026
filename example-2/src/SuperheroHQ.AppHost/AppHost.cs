var builder = DistributedApplication.CreateBuilder(args);

// Redis backs the Orleans cluster membership table and the grain state.
// This is the only "infrastructure" the demo needs, and Aspire starts it for us.
var redis = builder.AddRedis("redis")
                   .WithRedisInsight();

// No container runtime? Replace the Redis lines below with
//     .WithDevelopmentClustering().WithMemoryGrainStorage("Default")
// and drop the replica count to 1.

// The Orleans cluster itself is not a container: Aspire models it as configuration
// that gets handed to every project that references it.
var orleans = builder.AddOrleans("superhero-hq")
                     .WithClustering(redis)
                     .WithGrainStorage("Default", redis)
                     .WithGrainStorage("heroes", redis)
                     .WithGrainStorage("villains", redis)
                     .WithGrainStorage("cities", redis);

// Three silos in one cluster. Aspire gives each replica its own silo and gateway
// port, so scaling the cluster up or down is a single number on this line.
var silo = builder.AddProject<Projects.SuperheroHQ_Silo>("silo")
                  .WithReference(orleans)
                  .WithReplicas(3)
                  .WithExternalHttpEndpoints();

// A small web control panel with a button per silo HTTP endpoint. It is not an
// Orleans client: it proxies to the silo, so service discovery spreads the calls
// over the replicas.
builder.AddProject<Projects.SuperheroHQ_Web>("web")
       .WithReference(silo)
       .WaitFor(silo)
       .WithExternalHttpEndpoints();

// The guided console demo. It is started by hand from the Aspire dashboard so the
// story can be told at the right moment.
builder.AddProject<Projects.SuperheroHQ_Client>("demo")
       .WithReference(orleans.AsClient())
       .WaitFor(redis)
       .WithExplicitStart();

builder.Build().Run();
