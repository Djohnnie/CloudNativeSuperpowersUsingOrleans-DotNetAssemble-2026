# Example 2 - Superhero HQ

> Coming from [example 1](../example-1/README.md)? That one has a single grain type and
> nothing but memory. This one adds persistence, real Redis clustering, grain-to-grain
> calls and a guided console demo.

A small Orleans 10 application that demonstrates the concepts from slides 6 to 10:
grains as virtual actors, grain identity, behaviour and state, silos, placement and
the single-threaded execution model. It is orchestrated with **.NET Aspire** and hosts
the official **Orleans Dashboard**.

## The model

| Grain | Key | What it is |
| --- | --- | --- |
| `IHeroGrain` | hero name, e.g. `ironman` | trains, fights villains, keeps experience and energy |
| `IVillainGrain` | villain name, e.g. `thanos` | absorbs hits from many heroes at once |
| `ICityGrain` | city name, e.g. `new-york` | keeps a roster and sends every hero on a mission |

Everything is addressed by name. Nothing is ever constructed, looked up or disposed.

## Running it with Aspire (recommended)

The Aspire AppHost starts Redis, three silos and the demo client in one go, and gives you
one dashboard for logs, traces and metrics of the whole cluster.

```powershell
dotnet run --project src/SuperheroHQ.AppHost
```

This needs a container runtime (Docker Desktop or Podman) for Redis. The Aspire dashboard
opens automatically:

- `silo` runs with **3 replicas**, forming one Orleans cluster over Redis membership.
  Each replica exposes the Orleans Dashboard on its own HTTP endpoint at `/dashboard`.
- `web` is the control panel: a button per silo HTTP endpoint, handy for poking the
  cluster live without leaving the browser.
- `demo` is configured with **explicit start**: press *Start* on the resource when you
  are ready to tell the story.
- Change `.WithReplicas(3)` in `AppHost.cs` to scale the cluster; nothing in the grains
  changes.

Aspire also swaps the providers without touching a single grain: standalone, grain state
lives in memory and clustering is localhost-only; under Aspire, both are backed by Redis.

## Running it without Aspire

Start the first silo (this is also the primary silo of the cluster):

```powershell
dotnet run --project src/SuperheroHQ.Silo -- --instance 1
```

Open the dashboard at <http://localhost:5001/dashboard>.

Start more silos in their own terminals to grow the cluster:

```powershell
dotnet run --project src/SuperheroHQ.Silo -- --instance 2
dotnet run --project src/SuperheroHQ.Silo -- --instance 3
```

Instance `N` listens on HTTP port `500N`, silo port `1111N` and gateway port `3000N`.

Then run the guided demo, which connects as an Orleans client:

```powershell
dotnet run --project src/SuperheroHQ.Client
```

The client renders its output with [Spectre.Console](https://spectreconsole.net): a Figlet
banner, a rule per step, tables for grain state and mission reports, a live progress bar for
the 100 parallel calls and a bar chart for grain placement. Run it in a real terminal (not a
piped or redirected one) to see the spinners and progress bar animate.

Everything is drawn with **pure ASCII** (`TableBorder.Ascii`, `BoxBorder.Ascii`, a `#` bar
chart, a custom `#`/`-` progress column and the `Line` spinner) so the output stays readable
in the Aspire dashboard's console log viewer, which does not render box-drawing characters,
block glyphs or emoji.

## What the demo shows

1. **A grain is virtual.** Calling `hero/ironman` activates it; you never create it.
2. **Identity is everything.** A fresh grain reference finds the same activation and state.
3. **Single-threaded execution.** 100 parallel `TrainAsync()` calls all survive, even though
   the method reads, awaits and then writes without a lock.
4. **Grain-to-grain calls.** A city grain sends its whole roster after one villain grain.
5. **Placement and scale.** 60 new heroes are spread across the silos that happen to be running.
   Start another silo and run it again: the spread changes, the code does not.

## HTTP endpoints

The silo also exposes a few endpoints so you can drive the demo from a browser or curl:

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/heroes/{name}` | hero profile |
| `POST` | `/heroes/{name}/train` | train a hero |
| `POST` | `/heroes/{name}/fight/{villain}` | attack a villain |
| `GET` | `/villains/{name}` | villain status |
| `POST` | `/cities/{city}/assemble` | JSON array of hero names |
| `POST` | `/cities/{city}/mission/{villain}` | send the roster on a mission |
| `POST` | `/stress/{count}` | activate `count` heroes to fill the dashboard |

The console demo does **not** use these: it is a real Orleans client and talks to the
cluster over the gateway port. The endpoints exist so you can drive the grains by hand.

## The web control panel

`SuperheroHQ.Web` is a small ASP.NET Core app that puts a button on every endpoint above,
so you can train a hero, launch a mission or flood the cluster from the browser while the
Orleans Dashboard is open next to it.

```powershell
dotnet run --project src/SuperheroHQ.Web
```

Open <http://localhost:5080>. Under Aspire it is the `web` resource and needs no
configuration.

It is deliberately *not* an Orleans client. A single catch-all route proxies
`/api/{**path}` to the silo server-side, which keeps the browser free of CORS and means
the panel only depends on the silo's HTTP surface. Under Aspire the proxy target is
`http://silo`, so service discovery spreads the calls over the three replicas and you can
watch the `silo` field in the responses change; standalone it targets instance 1 on
`http://localhost:5001`.

## Tests

An in-process `TestCluster` with two silos verifies the same behaviour:

```powershell
dotnet test
dotnet test --filter-method "*Parallel_calls_never_lose_an_update*"
```

## Notes

- Standalone, state uses memory grain storage so the demo needs nothing installed. Under
  Aspire the exact same four providers (`Default`, `heroes`, `villains`, `cities`) are
  backed by Redis, configured in `AppHost.cs` instead of in the silo.
- `UseLocalhostClustering` is a development clustering provider. Production clusters use
  Azure Storage, Redis, ADO.NET or Kubernetes based membership; the Aspire path already
  uses real Redis membership.
- The silo detects Aspire by looking for the `Orleans:Clustering` configuration section
  that the AppHost injects, so one `Program.cs` serves both ways of running.
- The Orleans Dashboard is in preview in Orleans 10.
- `SuperheroHQ.ServiceDefaults` adds the `Microsoft.Orleans` meter and the
  `Microsoft.Orleans.Runtime` / `Microsoft.Orleans.Application` trace sources, so grain
  calls appear as distributed traces in the Aspire dashboard.
