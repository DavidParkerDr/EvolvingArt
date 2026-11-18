# Evolving Art

An evolving musical painting on the boundary between permanence and change.

Twelve double-sided panels painted by Roberto Bono can be turned over, turned round and rearranged. You can explore them by hand, watch a random slideshow, or let a genetic algorithm evolve arrangements in which colour and pattern flow across the edges between panels. There are rectangular and square sets of panels, and each panel can be cut into a grid of smaller pieces. Music is by Andrea Alberti and Bob Salmieri.

Concept and algorithms: Yiannis Papadopoulos, David Parker, Martin Walker and Emma-Jane Alexander. Department of Computer Science, University of Hull.

## Running it

It's a static site: plain HTML, CSS and JavaScript modules, with no build step and no dependencies.

**On GitHub Pages:** in the repository go to *Settings → Pages*, choose *Deploy from a branch*, then *main* and */ (root)*. The site will appear at `https://<user>.github.io/EvolvingArt/`.

**Locally:** serve the folder over HTTP, for example:

```
python -m http.server 8000
```

then open <http://localhost:8000>. Opening `index.html` straight from disk won't work, because browsers block JavaScript modules and canvas pixel reads on `file://` pages.

## Layout

| Path | What it is |
|---|---|
| `index.html`, `css/style.css` | Page and styling |
| `js/app.js` | User interface: grid and animations, slideshow, sound, save/load, settings |
| `js/ga.js` | The genetic algorithm, panel splitting and fitness functions. It has no browser dependencies, so it also runs in Node. |
| `js/sets.js` | The list of painting sets, with their image and sound folders |
| `assets/images/rectangular/roberto1–4`, `assets/images/square/roberto1–4` | Panel images, `1a.png` (front) … `12b.png` (back) |
| `assets/sounds/rectangular/roberto3`, `assets/sounds/square/roberto1` | Music: per-face loops and a background track |
| `TurnerTest/`, `TurnerTestSquare/` | The original Silverlight 5 projects (C#/XAML), for reference. The site doesn't use them. |

### Adding a painting set

Put the 24 images in a new folder under `assets/images/`, then add an entry to `js/sets.js`. Whether the panels are rectangular or square is worked out from the image sizes, so no other change is needed. If there's music, list the faces that have loops and the background track (see the existing entries).

## How rectangular and square panels are handled

The two Silverlight projects shared the same engine. The square one generalised it in two ways, and both now apply to every set:

- **Turning.** A piece can be turned a quarter turn at a time if it's square (0°, 90°, 180°, 270°), but only upside down if it's rectangular, so it still fits its slot.
- **Pieces per panel.** Each panel can be cut into m × m pieces (m = 1–10), making a 3m × 4m grid of 12m² pieces. A back face is stored as seen from behind, so the back of the piece in column c comes from column m − 1 − c of the back image, as `PaintingSplitter` did.

## Changes from the Silverlight versions

- **Back faces count in the fitness.** The rectangular version loaded the front image for the back as well, so turning a panel over never changed its score. (The square version had already fixed this.)
- **Crossover works for every set.** The square version's rewrite of `OrderCrossoverModified` refilled each child from the same parent, so children always kept their parent's layout; only the turn and face settings were crossed over. Both shapes now use the rectangular version's crossover, which does mix the two parents' layouts.
- **What's shown is what was scored.** In the square version, `Painting.Configure` didn't apply the rotation when it wasn't animating, and could stop the animated turn at the wrong angle. Here the display is always built from the arrangement itself.
- **Flip animation.** Pieces are CSS 3D cards. Turning a piece over shows its back the right way round. Turning it top-to-bottom shows the back turned half a turn, as with a real panel.
- **Panel sounds** loop while a face that owns them is showing (on any of its pieces) and fade out when it's turned away. The loops are phase-aligned so they stay in step. The background track plays during the slideshow and evolution, as before.
- **Evolution** runs one generation per tick instead of busy-waiting on a background thread. The pace can be *Fast*, *one per second*, or *match slide duration* (the original behaviour).
- **Saving** uses JSON files. Grids and evolution runs saved as XML by either Silverlight version can still be loaded; old runs are re-scored with the current fitness. An arrangement can also be shared as a link, e.g. `#set=s1&m=2&grid=…`.
- **Keyboard:** R randomise, S slideshow, E evolve, M mute, F full screen, H hide controls, Esc show controls.
