# Copilot instructions

## What this repository is

Conference-session material for **"Cloud-Native Superpowers with Microsoft Orleans"** (.NET Assemble! 2026, by Johnny Hooyberghs).

Today the repository contains **only presentation assets** — there is no solution, project, build, test, or lint pipeline. `.gitignore` is the standard Visual Studio/.NET one, so .NET demo code is expected to be added later under the repository root. When you add such code, create a `.sln` at the root and use standard `dotnet build` / `dotnet test` workflows (single test: `dotnet test --filter "FullyQualifiedName~MyTest"`).

## Layout

- `README.md` — the narrated deck: every slide is embedded as an image plus a short description.
- `_slides/` — hand-authored SVG slides and shared assets.

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

There is no automated check. After editing an SVG, confirm it renders (open it in a browser) and that nothing overflows the 3840x2160 canvas before committing.
