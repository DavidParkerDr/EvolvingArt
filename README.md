# Evolving Art

An evolving musical painting on the boundary between permanence and change.

Twelve double-sided panels painted by Roberto Bono can be turned over, turned upside down and rearranged. You can explore them by hand, watch a random slideshow, or let a genetic algorithm evolve arrangements in which colour and pattern flow across the edges between panels. Music for the Roberto 3 set is by Andrea Alberti and Bob Salmieri.

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
| `js/app.js` | User interface: panel grid and animations, slideshow, sound, save/load, settings |
| `js/ga.js` | The genetic algorithm and fitness functions. It has no browser dependencies, so it also runs in Node. |
| `TurnerTest/` | The original Silverlight 5 project (C#/XAML). The web version loads its images and sounds directly from `TurnerTest/Turner1/Images` and `TurnerTest/Turner1/Sounds`. |

## Changes from the Silverlight version

- **Back faces now count in the fitness.** The original loaded the front image for the back as well, in both `GeneticAlgorithm.LoadImages()` and `ImageHolder`, so turning a panel over never changed its score. Both faces are now analysed.
- **Flip animation.** Panels are CSS 3D cards. Turning a panel over shows its back the right way round; previously the animated flip left the back mirrored. Turning it top-to-bottom shows the back upside down, as with a real panel.
- **Panel sounds** loop while the face that owns them is showing and fade out when it's turned away. The loops are phase-aligned so they stay in step. The background track plays during the slideshow and evolution, as before.
- **Evolution** runs one generation per tick instead of busy-waiting on a background thread. The pace can be *Fast*, *one per second*, or *match slide duration* (the original behaviour). Whole-panel colour fitness is precomputed, so it's no longer slow.
- **Saving** uses JSON files. Grids and evolution runs saved as XML by the old version can still be loaded. Old runs are re-scored with the corrected fitness. An arrangement can also be shared as a link (`#set=3&grid=…`).
- **Keyboard:** R randomise, S slideshow, E evolve, M mute, F full screen, H hide controls, Esc show controls.
