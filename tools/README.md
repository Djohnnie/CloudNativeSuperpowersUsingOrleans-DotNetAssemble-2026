# Tools

Utility scripts for publishing this presentation.

---

## svg_to_pptx.py

Converts the numbered `slide-NN.svg` files into a single Microsoft PowerPoint (`.pptx`) file. Each SVG becomes one full-bleed slide in a 16:9 widescreen presentation. `slide-template.svg` is excluded.

### Why this exists

Slide content is authored and version-controlled as SVG files in `_slides/`. When a presenter or attendee needs a `.pptx` file, this script produces one on demand.

### Requirements

- **Python 3.8+**
- **python-pptx** — `pip install python-pptx`
- **Microsoft Edge** — used as a headless renderer to convert SVG → PNG before embedding in the deck. Edge ships with Windows 11 and is available at its standard install location; if yours differs, update `EDGE_PATHS` near the top of the script.

### Usage

```
python tools/svg_to_pptx.py <svg_dir> [output.pptx]
```

| Argument | Description |
|---|---|
| `svg_dir` | Path to a directory containing numbered `slide-NN.svg` files |
| `output.pptx` | *(optional)* Output path. Defaults to `<svg_dir>/<dirname>.pptx` |

### Examples

```powershell
python tools/svg_to_pptx.py _slides CloudNativeSuperpowersUsingOrleans-DotNetAssemble-2026.pptx
```

### How it works

1. Collects numbered `slide-NN.svg` files in the given directory, sorted by filename (excluding the template).
2. Launches Edge in headless mode for each SVG and captures its full 3840×2160 canvas as a PNG screenshot.
3. Embeds every PNG as a full-bleed image on a blank slide in a new Presentation object.
4. Saves the resulting `.pptx` to the output path. If a slide cannot be rendered, generation fails instead of silently skipping it.
