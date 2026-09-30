using System.Net;
using Microsoft.AspNetCore.Mvc;
using Orleans.Configuration;
using Orleans.Dashboard;
using SuperheroHQ.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Two ways to run this silo:
//   * under Aspire, which supplies clustering, storage and endpoints as configuration;
//   * standalone with "dotnet run -- --instance 2", which wires everything by hand.
var underAspire = builder.Configuration.GetSection("Orleans:Clustering").Exists();
var instance = builder.Configuration.GetValue("instance", 1);

if (underAspire)
{
    builder.AddServiceDefaults();
    builder.AddKeyedRedisClient("redis");
}
else
{
    builder.WebHost.UseUrls($"http://localhost:{5000 + instance}");
}

builder.UseOrleans(silo =>
{
    if (!underAspire)
    {
        silo.UseLocalhostClustering(
            siloPort: 11110 + instance,
            gatewayPort: 30000 + instance,
            primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111));

        silo.Configure<SiloOptions>(options => options.SiloName = $"silo-{instance}");

        // Grain state lives in memory for this demo. Under Aspire these same four
        // providers are backed by Redis without a single change to a grain.
        silo.AddMemoryGrainStorageAsDefault();
        silo.AddMemoryGrainStorage("heroes");
        silo.AddMemoryGrainStorage("villains");
        silo.AddMemoryGrainStorage("cities");
    }

    // The official Orleans dashboard (Orleans 10).
    silo.AddDashboard(options => options.CounterUpdateIntervalMs = 1000);
});

var app = builder.Build();

if (underAspire)
{
    app.MapDefaultEndpoints();
}

// http://localhost:500X/dashboard
app.MapOrleansDashboard(routePrefix: "/dashboard");
app.MapGet("/", () => Results.Redirect("/dashboard"));

app.MapGet("/heroes/{name}", (IGrainFactory grains, string name) =>
    grains.GetGrain<IHeroGrain>(name).GetProfileAsync());

app.MapPost("/heroes/{name}/train", (IGrainFactory grains, string name) =>
    grains.GetGrain<IHeroGrain>(name).TrainAsync());

app.MapPost("/heroes/{name}/fight/{villain}", (IGrainFactory grains, string name, string villain) =>
    grains.GetGrain<IHeroGrain>(name).FightAsync(villain));

app.MapGet("/villains/{name}", (IGrainFactory grains, string name) =>
    grains.GetGrain<IVillainGrain>(name).GetStatusAsync());

app.MapPost("/cities/{city}/assemble", (IGrainFactory grains, string city, [FromBody] string[] heroes) =>
    grains.GetGrain<ICityGrain>(city).AssembleAsync(heroes));

app.MapPost("/cities/{city}/mission/{villain}", (IGrainFactory grains, string city, string villain) =>
    grains.GetGrain<ICityGrain>(city).LaunchMissionAsync(villain));

// Activates a lot of heroes at once so you can watch the dashboard fill up.
app.MapPost("/stress/{count:int}", async (IGrainFactory grains, int count) =>
{
    await Parallel.ForEachAsync(Enumerable.Range(0, count), async (i, _) =>
        await grains.GetGrain<IHeroGrain>($"recruit-{i:D6}").TrainAsync());

    return Results.Ok(new { activated = count });
});

app.Run();
