using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Orleans.Configuration;
using Orleans.Dashboard;
using Orleans.Runtime;
using ParcelTracker.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Every silo in this cluster runs this exact program. The only thing that differs
// is "--instance N", and that single number decides all three ports. Instance 1 is
// the primary silo: it is the one that holds the in-memory membership table the
// other silos join.
var instance = builder.Configuration.GetValue("instance", 1);

builder.AddServiceDefaults();

// Stages travel as "InTransit" rather than 1, which keeps the demo readable in
// the browser and in the Aspire logs.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Under Aspire the HTTP port is handed to us through ASPNETCORE_URLS.
// Started by hand, we pick port 500N ourselves.
if (string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_URLS"]))
{
    builder.WebHost.UseUrls($"http://localhost:{5000 + instance}");
}

builder.UseOrleans(silo =>
{
    // "Localhost clustering" keeps cluster membership in memory on the primary
    // silo, so silos find each other with nothing installed: no Redis, no Azure
    // Table, no SQL, no container. That is the whole point of example 1 - and it
    // is also the one thing you must replace before going to production.
    silo.UseLocalhostClustering(
        siloPort: 11110 + instance,
        gatewayPort: 30000 + instance,
        primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111));

    // A readable name, so "which silo is this parcel on?" has a nice answer.
    silo.Configure<SiloOptions>(options => options.SiloName = $"silo-{instance}");

    // Grain state lives in memory too. It is still shared across the whole
    // cluster: the memory provider stores state inside grains, so a parcel keeps
    // its state even when Orleans activates it on a different silo.
    silo.AddMemoryGrainStorageAsDefault();

    // The official Orleans dashboard (preview in Orleans 10).
    silo.AddDashboard(options => options.CounterUpdateIntervalMs = 1000);
});

var app = builder.Build();

app.MapDefaultEndpoints();

// http://localhost:500N/dashboard
app.MapOrleansDashboard(routePrefix: "/dashboard");
app.MapGet("/", () => Results.Redirect("/dashboard"));

// Which silo am I? Useful to prove that any silo can serve any parcel.
app.MapGet("/silo", (ILocalSiloDetails silo) => Results.Ok(new { silo = silo.Name }));

// Who is in the cluster right now? Start or stop a silo and watch this change.
app.MapGet("/cluster", async (IGrainFactory grains) =>
{
    var hosts = await grains.GetGrain<IManagementGrain>(0).GetDetailedHosts(onlyActive: true);

    return Results.Ok(hosts
        .OrderBy(host => host.SiloName, StringComparer.Ordinal)
        .Select(host => new
        {
            silo = host.SiloName,
            address = host.SiloAddress.ToString(),
            status = host.Status.ToString()
        }));
});

// The three calls the whole demo is built on.
app.MapGet("/packages/{trackingNumber}", (IGrainFactory grains, string trackingNumber) =>
    grains.GetGrain<IPackageGrain>(trackingNumber).GetStatusAsync());

app.MapPost("/packages/{trackingNumber}/scan", (IGrainFactory grains, string trackingNumber, [FromBody] ScanRequest scan) =>
    grains.GetGrain<IPackageGrain>(trackingNumber).ScanAsync(scan.Location, scan.Stage));

app.MapGet("/packages/{trackingNumber}/history", (IGrainFactory grains, string trackingNumber) =>
    grains.GetGrain<IPackageGrain>(trackingNumber).GetHistoryAsync());

// Scans a whole batch of parcels at once and reports where they ended up.
// This is the scaling story: add a silo, run it again, and the same call spreads
// over more of them without a line of code changing.
app.MapPost("/fleet/{count:int}", async (IGrainFactory grains, int count) =>
{
    var perSilo = new ConcurrentDictionary<string, int>();

    await Parallel.ForEachAsync(Enumerable.Range(1, count), async (number, cancellationToken) =>
    {
        var status = await grains.GetGrain<IPackageGrain>($"PKG-{number:D6}")
                                 .ScanAsync("sorting centre", DeliveryStage.InTransit);

        perSilo.AddOrUpdate(status.Silo, 1, (_, current) => current + 1);
    });

    return Results.Ok(new
    {
        scanned = count,
        perSilo = perSilo.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                         .ToDictionary(entry => entry.Key, entry => entry.Value)
    });
});

app.Run();

internal sealed record ScanRequest(string Location, DeliveryStage Stage);
