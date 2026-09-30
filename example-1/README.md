# Example 1 - Parcel Tracker

The dead-simple Orleans example: **one grain type, many instances, scaled over a
cluster**. There is nothing else in here. No database, no Redis, no container
runtime, no grain-to-grain calls, no streams, no reminders.

If you can follow a parcel through a depot, you can follow this example.

> Ready for more? [Example 2 - Superhero HQ](../example-2/README.md) adds several
> grain types, grain-to-grain calls, Redis clustering and real persistence.

## The model

| Grain | Key | What it is |
| --- | --- | --- |
| `IPackageGrain` | tracking number, e.g. `PKG-000001` | one parcel: where it is, which stage it is in, how often it was scanned |

That is the whole domain. One interface, one implementation, three methods:

```csharp
Task<PackageStatus> ScanAsync(string location, DeliveryStage stage);
Task<PackageStatus> GetStatusAsync();
Task<IReadOnlyList<ScanEvent>> GetHistoryAsync();
```

The tracking number *is* the identity. You never construct a parcel, never look
one up and never dispose one. You ask for `PKG-000001` and start calling it.

## Running it with Aspire (recommended)

```powershell
dotnet run --project src/ParcelTracker.AppHost
```

No container runtime needed - there is nothing to containerise. The Aspire
dashboard opens automatically and starts five resources: three silos and the web
control panel.

| Resource | URL | What it is |
| --- | --- | --- |
| `silo1` | <http://localhost:5001/dashboard> | primary silo + Orleans Dashboard |
| `silo2` | <http://localhost:5002/dashboard> | silo + Orleans Dashboard |
| `silo3` | <http://localhost:5003/dashboard> | silo + Orleans Dashboard |
| `web` | <http://localhost:5080> | the control panel you drive the demo from |

Ports are pinned on purpose, so the URLs on your slides keep working.

## Running it without Aspire

The same three silos, started by hand, each in its own terminal:

```powershell
dotnet run --project src/ParcelTracker.Silo -- --instance 1
dotnet run --project src/ParcelTracker.Silo -- --instance 2
dotnet run --project src/ParcelTracker.Silo -- --instance 3
```

Instance `N` listens on HTTP port `500N`, silo port `1111N` and gateway port
`3000N`. Instance 1 is the primary silo and must be started first.

```powershell
dotnet run --project src/ParcelTracker.Web
```

Aspire and the by-hand route give the silos the exact same ports, so everything
below works either way.

## The demo, step by step

Open the control panel on <http://localhost:5080> next to the Orleans Dashboard on
<http://localhost:5001/dashboard>.

1. **A grain is virtual.** Press *Status* for `PKG-000001`. Nothing created it;
   the call itself brought it to life. Watch the activation appear in the
   dashboard.
2. **Identity is the key.** Press *Record scan*, then *Status* again. Same
   tracking number, same parcel, same state - no repository, no `new`.
3. **Location is not your problem.** The panel has a *Talk to* selector at the
   top. Call silo 3 and read the `silo` field in the answer: the parcel often
   lives somewhere else entirely, and Orleans routed the call for you.
4. **State is per grain.** Change the tracking number to `PKG-000002` and press
   *Status*. A completely independent parcel, with its own state.
5. **One call at a time.** `ScanAsync` reads the scan counter, awaits, and only
   then writes it back - without a lock. Run the test below: 100 parallel scans
   still end at exactly 100.
6. **Scale out.** Press *Scan them all* for 1000 parcels. The answer counts how
   many parcels landed on each silo, roughly evenly across the three.

### Showing the cluster grow and shrink

Press *Which silos are up?* to list active silos. Then, in the Aspire dashboard,
**Stop** `silo3` and press it again: the cluster is down to two. Start it back up
and it rejoins. Re-run *Scan them all* after each change and watch the spread
follow the cluster. Not one line of grain code knows any of this happened.

To change the cluster size permanently, edit one number:

```csharp
const int SiloCount = 3;   // src/ParcelTracker.AppHost/AppHost.cs
```

## HTTP endpoints

The silo exposes the endpoints the control panel calls, so you can also drive the
demo with curl:

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/packages/{trackingNumber}` | current status |
| `POST` | `/packages/{trackingNumber}/scan` | body: `{"location":"...","stage":"InTransit"}` |
| `GET` | `/packages/{trackingNumber}/history` | the last 10 scans |
| `GET` | `/cluster` | which silos are active right now |
| `GET` | `/silo` | which silo answered this HTTP call |
| `POST` | `/fleet/{count}` | scan `count` parcels and count them per silo |
| `GET` | `/dashboard` | the Orleans Dashboard |

## Tests

An in-process `TestCluster` with two silos verifies the same behaviour:

```powershell
dotnet test
dotnet test --filter-method "*Parallel_scans_never_lose_an_update*"
```

## Notes, and the honest bits

- **Everything is in memory**, both cluster membership (`UseLocalhostClustering`)
  and grain state (`AddMemoryGrainStorageAsDefault`). Stop the silos and the
  parcels are gone. That is exactly what keeps this example runnable anywhere, and
  it is the first thing you would replace in production - see example 2.
- Memory grain storage is still **cluster-wide**: the provider keeps state inside
  grains, so a parcel keeps its state even when Orleans activates it on another
  silo. Verified by reading the same parcel through all three silos.
- This AppHost deliberately does **not** call `builder.AddOrleans()`. Aspire's
  Orleans integration is excellent, but its `WithDevelopmentClustering()` never
  sets `Orleans:Clustering:PrimarySiloEndPoint`, so each silo would silently fall
  back to being the primary of its own one-silo cluster - three "clusters" of one,
  which looks broken on stage. Modelling the silos as three resources and letting
  Orleans' own localhost clustering point at a fixed primary gives one real
  cluster with no infrastructure. Example 2 shows the full Aspire integration,
  backed by Redis.
- `PackageGrain` is marked `[RandomPlacement]`. Orleans' default placement prefers
  the calling silo when the cluster is evenly loaded, which is the right choice in
  production but makes one silo look like it does all the work in a demo.
- `ScanAsync` reads, awaits and then writes without a lock **on purpose**. That is
  the single-threaded execution model demo - please do not "fix" it.
- The web app is **not** an Orleans client. It proxies `/api/{**path}` to the silo
  you picked, which keeps the browser free of CORS and makes "the grain lives
  somewhere else" visible.
- The Orleans Dashboard (`Microsoft.Orleans.Dashboard`) is the official package as
  of Orleans 10 and is still in preview. Each silo hosts its own.
- `ParcelTracker.ServiceDefaults` adds the `Microsoft.Orleans` meter and the
  `Microsoft.Orleans.Runtime` / `Microsoft.Orleans.Application` trace sources, so
  grain calls show up as distributed traces in the Aspire dashboard.
