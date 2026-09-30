# Copilot instructions

## What this repository is

Conference-session material for **"Cloud-Native Superpowers with Microsoft Orleans"** (.NET Assemble! 2026, by Johnny Hooyberghs).

The repository holds two kinds of content: hand-authored SVG presentation slides, and runnable Orleans demo applications under `example-N/`.

## Layout

- `README.md` — the narrated deck: every slide is embedded as an image plus a short description, followed by an index of the examples.
- `_slides/` — hand-authored SVG slides and shared assets.
- `example-1/` — "Parcel Tracker", the dead-simple Orleans 10 demo: a single grain type (`IPackageGrain`, one instance per tracking number), Aspire AppHost, three silo hosts + dashboard, web control panel, TestCluster tests. No external infrastructure at all.
- `example-2/` — "Superhero HQ", an Orleans 10 demo (Aspire AppHost, silo host + dashboard, console client, web control panel, TestCluster tests), backed by Redis clustering and persistence.

## Demo applications

Each `example-N/` folder is a self-contained solution with its own `global.json`, `README.md` and `.slnx`. Build and test from that folder:

```powershell
dotnet build
dotnet test
dotnet test --filter-method "*Parallel_scans_never_lose_an_update*"
```

Examples are ordered by difficulty: `example-1` is the "you can follow this" opener with one
grain type and nothing installed, `example-2` is the fuller story. Conventions that every
example follows:

- Projects split as `src/*.Abstractions` (grain interfaces and `[GenerateSerializer]` records shared with clients), `src/*.Grains` (grain implementations), `src/*.Silo` (ASP.NET Core silo host), `src/*.Web` (browser control panel), optionally `src/*.Client` (console demo), `src/*.AppHost` (Aspire orchestration), `src/*.ServiceDefaults`, `tests/*.Tests`.
- Aspire is the primary way to run a demo (`dotnet run --project src/<Name>.AppHost`), but every host must keep working when started by hand.
- Two ways to model a cluster in the AppHost, and they are not interchangeable:
  - With external membership (example 2): `AddOrleans(...).WithClustering(redis).WithGrainStorage(...)` and `WithReplicas(n)`.
  - Without any infrastructure (example 1): do **not** use `AddOrleans().WithDevelopmentClustering()` together with `WithReplicas(n)`. Aspire never sets `Orleans:Clustering:PrimarySiloEndPoint`, so every replica falls back to being its own primary and you silently get N one-silo clusters. Instead add the silo project as N separate resources with `--instance N` and let the silo call `UseLocalhostClustering` with an explicit fixed primary endpoint.
- Multiple silos of one cluster are started from the same host project with `--instance N`; that number derives the HTTP port (`500N`), silo port (`1111N`) and gateway port (`3000N`), with instance 1 acting as the primary silo. The AppHost pins the same ports so slide URLs keep working either way.
- The grain project needs `Microsoft.Orleans.Runtime`, not just `Microsoft.Orleans.Sdk`: in Orleans 10, `IPersistentState<T>`, `PersistentStateAttribute` and `ILocalSiloDetails` are not in the Sdk package.
- Orleans packages are pinned to the same version across all projects (currently `10.3.1`).
- The demos are deliberately written so each step maps to a slide (virtual actor, identity/behaviour/state, grain-to-grain calls, placement, single-threaded turns). `PackageGrain.ScanAsync` and `HeroGrain.TrainAsync` read, await and write without a lock on purpose — that is the single-threading demo, so do not "fix" it.
- Grain state uses memory storage so the demo runs with nothing installed. Memory storage is cluster-wide, because the provider keeps state inside grains.
- Demo grains are marked `[RandomPlacement]`. Orleans' default placement prefers the calling silo, which makes one silo look like it does all the work when demonstrating scale-out.
- The `*.Web` control panel is never an Orleans client: it proxies `/api/{**path}` to a silo server-side, which avoids CORS and keeps "the grain lives on another silo" visible.
- The dashboard comes from `Microsoft.Orleans.Dashboard` (`silo.AddDashboard()` plus `app.MapOrleansDashboard(routePrefix: "/dashboard")`), which is the official package as of Orleans 10 and still in preview.


## Slides are hand-written SVG, not exported from a slide tool

Edit the SVG source directly; do not regenerate it from PowerPoint/Keynote.

`_slides/slide-template.svg` is the canonical starting point for a new slide. Copy it and keep its structure and comment markers intact:

```
<!-- Background: ... radial gradient ... -->   full-bleed <rect> with url(#bgGradient)
<!-- Slide title, top left -->                 <text x="120" y="220">
<!-- Logo: net-assemble-logo.svg, top right --> inlined <svg x="3290" y="90" width="450" height="228.4">
<!-- Slide index number, bottom right -->      <text x="3740" y="2080" text-anchor="end">NN</text>
```

Conventions that must stay consistent across slides:

- Canvas is `3840 x 2160` with `viewBox="0 0 3840 2160"` (4K, 16:9). All coordinates are absolute in that space.
- Font is always `Arial, Helvetica, sans-serif`. Sizes in use: title `110`, speaker name `150` (italic, weight 800), subtitle `64`, body/bullets `~64-70` with `dy="86"` between wrapped `<tspan>` lines, slide number `70`, footer `46`.
- Palette: near-black text `#1D1D1B`, grey `#706F6F`, accent orange `#CC6600`, background gradient `#FFF3CC` → `#FFD9A6`.
- Titles and the slide number use a white outline via `stroke="#FFFFFF"` + `paint-order="stroke"` so they stay readable over artwork.
- Left content margin is `x="120"` for headings and `x="210"` for bullet text.
- Optional footer, centred at `x="1920" y="2100"`, is the repository URL at `fill-opacity="0.45"`.
- The slide index (`01`, `02`, …) is zero-padded and must match the file name `slide-NN.svg`.

Assets are **inlined**, not referenced: the logo is a copied `<svg>` subtree and photos/backgrounds are `<image href="data:image/...;base64,...">`. That is why some slide files are multi-megabyte — large diffs on `_slides/*.svg` are normal and expected. `net-assemble-logo.svg`, `comic_background.svg`, `qr-code.svg` and `trainer-photo.jpg` are the source assets to inline from.

## README and slides move together

Every slide has a matching README section, and the README is the only "index" of the deck. When adding or changing a slide, update `README.md` in the same change using the existing pattern:

```markdown
### Slide N - <Short title>
![Slide NN](_slides/slide-NN.svg)

> **TL;DR:** <one sentence, addressed to the audience in second person>

<one short paragraph explaining the slide>
```

Sections are ordered by slide number, and the TL;DR is deliberately written as "We are learning…" / "You are learning…" — keep that voice.

## Verification

There is no automated check for the slides. After editing an SVG, confirm it renders (open it in a browser) and that nothing overflows the 3840x2160 canvas before committing. Demo code is verified with `dotnet build` and `dotnet test` inside the relevant `example-N/` folder.
