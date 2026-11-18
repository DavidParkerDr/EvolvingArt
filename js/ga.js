// Evolving Art — genetic algorithm and fitness.
// A port of the original Silverlight C# (GeneticAlgorithm.cs, Individual.cs, PaintingGridEncoding.cs,
// PaintingEncoding.cs, ImageHolder.cs), merging the rectangular-panel version (TurnerTest) and the
// square-panel version (TurnerTestSquare) into one engine. Panels may be rectangular or square: square
// panels can be turned a quarter turn at a time (0/90/180/270°), rectangular ones only half a turn
// (0/180°), so they still fit the grid. Panels are always whole, as in the physical work.
//
// This module has no browser dependencies, so it also runs in Node or a Web Worker.
// Image data is supplied as objects shaped like ImageData: { width, height, data: RGBA bytes }.

export const PANEL_ROWS = 3;
export const PANEL_COLUMNS = 4;
export const NUMBER_OF_PANELS = PANEL_ROWS * PANEL_COLUMNS;

let random = Math.random;
/** Replace the random number source (e.g. with a seeded generator for testing). */
export function setRandom(fn) { random = fn; }
const nextInt = (n) => Math.floor(random() * n);
const flipCoin = () => random() < 0.5;
const pick = (arr) => arr[nextInt(arr.length)];

// ---------------------------------------------------------------------------
// Encodings
// ---------------------------------------------------------------------------

/** One panel in one slot: which panel, how far it is turned (degrees clockwise), which face is showing. */
export class PaintingEncoding {
  constructor(paintingIndex, rotation, frontVisible) {
    this.paintingIndex = paintingIndex;
    this.rotation = rotation;
    this.frontVisible = frontVisible;
  }
  clone() { return new PaintingEncoding(this.paintingIndex, this.rotation, this.frontVisible); }

  /** With probability `rate`, choose a new allowed rotation at random; likewise (with a coin toss) turn over. */
  mutate(rate, rotations) {
    if (random() < rate) this.rotation = pick(rotations);
    if (random() < rate && flipCoin()) this.frontVisible = !this.frontVisible;
  }
}

/** A whole arrangement: one PaintingEncoding per slot, in row-major order. */
export class GridEncoding {
  constructor(encodings = []) { this.encodings = encodings; }

  get length() { return this.encodings.length; }

  static random(count, rotations) {
    const unplaced = [...Array(count).keys()];
    const encodings = [];
    while (unplaced.length > 0) {
      const i = nextInt(unplaced.length);
      const paintingIndex = unplaced[i];
      unplaced[i] = unplaced[unplaced.length - 1];
      unplaced.pop();
      encodings.push(new PaintingEncoding(paintingIndex, pick(rotations), flipCoin()));
    }
    return new GridEncoding(encodings);
  }

  clone() { return new GridEncoding(this.encodings.map((e) => e.clone())); }
  at(i) { return this.encodings[i]; }

  /** Map from painting index to its encoding. */
  byPainting() {
    const map = new Array(this.encodings.length);
    for (const e of this.encodings) map[e.paintingIndex] = e;
    return map;
  }

  /** Swap-based mutation: with probability `rate` per slot, swap with a random panel and
   *  possibly change its rotation/face. */
  mutate(rate, rotations) {
    const n = this.encodings.length;
    const where = this.byPainting();
    for (const enc of this.encodings) {
      if (random() < rate) {
        const newIndex = nextInt(n);
        const swap = where[newIndex];
        swap.paintingIndex = enc.paintingIndex;
        enc.paintingIndex = newIndex;
        where[swap.paintingIndex] = swap;
        where[newIndex] = enc;
        enc.mutate(rate, rotations);
      }
    }
  }

  /** "Order crossover (modified)", as in the rectangular version: half the slots are inherited
   *  directly from one parent; the rest are taken from the other parent where that doesn't
   *  duplicate a panel, and any conflicts are filled at random from the unused panels.
   *  (The square version's rewrite of this function copied each parent unchanged — see README.) */
  crossover(other) {
    const n = this.encodings.length;
    // Half the slots, chosen at random, are crossover points (inherited directly); the rest are "remaining".
    const crossoverPoints = [...Array(n).keys()];
    const remaining = [];
    while (crossoverPoints.length > n / 2) {
      const i = nextInt(crossoverPoints.length);
      remaining.push(crossoverPoints[i]);
      crossoverPoints.splice(i, 1);
    }
    const child1 = this.#child(this, other, crossoverPoints, remaining);
    const child2 = this.#child(other, this, crossoverPoints, remaining);
    child1.#crossoverFlags(this, other);
    child2.#crossoverFlags(this, other);
    return [child1, child2];
  }

  #child(source, otherSource, crossoverPoints, remainingPoints) {
    const n = source.encodings.length;
    const slots = new Array(n);
    // panels still free, keyed by painting index; their flags come from otherSource
    const free = new Map(otherSource.encodings.map((e) => [e.paintingIndex, e]));
    for (const i of crossoverPoints) {
      const enc = source.at(i);
      slots[i] = enc.clone();
      free.delete(enc.paintingIndex);
    }
    const unfilled = [];
    for (const slot of remainingPoints) {
      const wanted = otherSource.at(slot).paintingIndex;
      if (free.has(wanted)) {
        slots[slot] = free.get(wanted).clone();
        free.delete(wanted);
      } else {
        unfilled.push(slot);
      }
    }
    const leftovers = [...free.values()];
    for (const slot of unfilled) {
      const i = nextInt(leftovers.length);
      slots[slot] = leftovers[i].clone();
      leftovers[i] = leftovers[leftovers.length - 1];
      leftovers.pop();
    }
    return new GridEncoding(slots);
  }

  #crossoverFlags(parent1, parent2) {
    const p1 = parent1.byPainting();
    const p2 = parent2.byPainting();
    for (const enc of this.encodings) {
      enc.rotation = (flipCoin() ? p1 : p2)[enc.paintingIndex].rotation;
      enc.frontVisible = (flipCoin() ? p1 : p2)[enc.paintingIndex].frontVisible;
    }
  }

  isValid(count) {
    if (this.encodings.length !== count) return false;
    const seen = new Uint8Array(count);
    for (const e of this.encodings) {
      if (!(e.paintingIndex >= 0 && e.paintingIndex < count) || seen[e.paintingIndex]) return false;
      seen[e.paintingIndex] = 1;
    }
    return true;
  }

  /** Force every rotation into the allowed set (e.g. a square-set arrangement loaded for rectangles). */
  constrainRotations(rotations) {
    for (const e of this.encodings) {
      if (!rotations.includes(e.rotation)) e.rotation = e.rotation >= 180 ? 180 : 0;
    }
    return this;
  }

  // --- Serialisation -------------------------------------------------------
  toJSON() {
    return this.encodings.map((e) => ({ painting: e.paintingIndex, rotation: e.rotation, front: e.frontVisible }));
  }
  static fromJSON(arr) {
    return new GridEncoding(arr.map((o) => new PaintingEncoding(
      o.painting,
      typeof o.rotation === 'number' ? normaliseAngle(o.rotation) : (o.rotated ? 180 : 0),
      o.front,
    )));
  }
  /** Compact form for URLs, e.g. "4a,7bR,2aR90,..." (1-based panel number, face a/b,
   *  R = upside down, R90 / R270 = quarter turns clockwise). */
  toToken() {
    const suffix = { 0: '', 90: 'R90', 180: 'R', 270: 'R270' };
    return this.encodings.map((e) => `${e.paintingIndex + 1}${e.frontVisible ? 'a' : 'b'}${suffix[e.rotation]}`).join(',');
  }
  static fromToken(token) {
    return new GridEncoding(token.split(',').map((p) => {
      const m = /^(\d{1,2})([ab])(R(?:90|180|270)?)?$/.exec(p);
      if (!m) throw new Error(`Bad panel token "${p}"`);
      const rotation = !m[3] ? 0 : m[3] === 'R' ? 180 : Number(m[3].slice(1));
      return new PaintingEncoding(+m[1] - 1, rotation, m[2] === 'a');
    }));
  }
}

export const normaliseAngle = (a) => ((Math.round(a / 90) * 90) % 360 + 360) % 360;

// ---------------------------------------------------------------------------
// Image analysis (replaces ImageHolder / WriteableBitmap pixel access)
// ---------------------------------------------------------------------------

/** Average `thickness` pixels inward from an edge, one value per position along that edge
 *  (top→bottom for left/right, left→right for top/bottom). Returns Int32Array [a,r,g,b, ...]
 *  with C#-style integer averaging. */
function edgeStrip(img, edge, thickness) {
  const { width: w, height: h, data } = img;
  const len = edge === 'left' || edge === 'right' ? h : w;
  const out = new Int32Array(len * 4);
  for (let i = 0; i < len; i++) {
    let a = 0, r = 0, g = 0, b = 0;
    for (let j = 0; j < thickness; j++) {
      let x, y;
      if (edge === 'left') { x = j; y = i; }
      else if (edge === 'right') { x = w - 1 - j; y = i; }
      else if (edge === 'top') { x = i; y = j; }
      else { x = i; y = h - 1 - j; }
      const p = (y * w + x) * 4;
      r += data[p]; g += data[p + 1]; b += data[p + 2]; a += data[p + 3];
    }
    out[i * 4] = Math.trunc(a / thickness);
    out[i * 4 + 1] = Math.trunc(r / thickness);
    out[i * 4 + 2] = Math.trunc(g / thickness);
    out[i * 4 + 3] = Math.trunc(b / thickness);
  }
  return out;
}

function reversed(strip) {
  const n = strip.length / 4;
  const out = new Int32Array(strip.length);
  for (let i = 0; i < n; i++) out.set(strip.subarray(i * 4, i * 4 + 4), (n - 1 - i) * 4);
  return out;
}

/** The edges of a panel after turning it a further 90° clockwise (matches ImageHolder in the square version). */
const quarterTurn = (e) => ({ left: e.bottom, right: e.top, top: reversed(e.left), bottom: reversed(e.right) });

function averagePixel(img) {
  const { data } = img;
  const n = data.length / 4;
  let a = 0, r = 0, g = 0, b = 0;
  for (let p = 0; p < data.length; p += 4) { r += data[p]; g += data[p + 1]; b += data[p + 2]; a += data[p + 3]; }
  return Int32Array.of(Math.trunc(a / n), Math.trunc(r / n), Math.trunc(g / n), Math.trunc(b / n));
}

/**
 * The 12 panels of one painting set, ready for the fitness functions.
 * `panels[i] = { front: ImageData, back: ImageData }`. Panels whose images are square can be turned in quarter turns.
 */
export class PaintingSet {
  constructor(panels) {
    const { width: W, height: H } = panels[0].front;
    this.rows = PANEL_ROWS;
    this.columns = PANEL_COLUMNS;
    this.count = NUMBER_OF_PANELS;
    this.panelWidth = W;
    this.panelHeight = H;
    this.square = W === H;
    this.rotations = this.square ? [0, 90, 180, 270] : [0, 180];
    this.panels = panels;
    this.averages = panels.map((p) => ({ front: averagePixel(p.front), back: averagePixel(p.back) }));
    this.edgeCache = new Map();
  }

  /** edges(t)[panel][face][rotation/90] = { left, right, top, bottom } */
  edges(thickness) {
    if (!this.edgeCache.has(thickness)) {
      this.edgeCache.set(thickness, this.panels.map((p) => {
        const forFace = (img) => {
          const t = Math.max(1, Math.min(thickness, img.width, img.height));
          const upright = {
            left: edgeStrip(img, 'left', t), right: edgeStrip(img, 'right', t),
            top: edgeStrip(img, 'top', t), bottom: edgeStrip(img, 'bottom', t),
          };
          const r90 = quarterTurn(upright);
          const r180 = quarterTurn(r90);
          return [upright, r90, r180, quarterTurn(r180)];
        };
        return { front: forFace(p.front), back: forFace(p.back) };
      }));
    }
    return this.edgeCache.get(thickness);
  }
}

function stripDistance(s1, s2) {
  const n = Math.min(s1.length, s2.length);
  let sum = 0;
  for (let i = 0; i < n; i += 4) {
    const da = s1[i] - s2[i], dr = s1[i + 1] - s2[i + 1], dg = s1[i + 2] - s2[i + 2], db = s1[i + 3] - s2[i + 3];
    sum += Math.sqrt(da * da + dr * dr + dg * dg + db * db);
  }
  return { sum, count: n / 4 };
}

function pixelDistance(p, q) {
  const da = p[0] - q[0], dr = p[1] - q[1], dg = p[2] - q[2], db = p[3] - q[3];
  return Math.sqrt(da * da + dr * dr + dg * dg + db * db);
}

/**
 * Fitness: mean colour distance across touching panel boundaries (lower = more harmonious).
 * type 1 = matching edge pixels (default), type 0 = whole-panel average colours.
 */
export function fitness(grid, set, { type = 1, edgeThickness = 1 } = {}) {
  const { rows, columns } = set;
  let sum = 0, count = 0;
  if (type === 0) {
    const avg = (e) => set.averages[e.paintingIndex][e.frontVisible ? 'front' : 'back'];
    for (let row = 0; row < rows; row++) {
      for (let col = 0; col < columns; col++) {
        const i = row * columns + col;
        const here = avg(grid.at(i));
        if (col < columns - 1) { sum += pixelDistance(here, avg(grid.at(i + 1))); count++; }
        if (row < rows - 1) { sum += pixelDistance(here, avg(grid.at(i + columns))); count++; }
      }
    }
    return sum / count;
  }
  const edges = set.edges(edgeThickness);
  const edgeOf = (e) => edges[e.paintingIndex][e.frontVisible ? 'front' : 'back'][e.rotation / 90];
  for (let row = 0; row < rows; row++) {
    for (let col = 0; col < columns; col++) {
      const i = row * columns + col;
      const here = edgeOf(grid.at(i));
      if (col < columns - 1) {
        const d = stripDistance(here.right, edgeOf(grid.at(i + 1)).left);
        sum += d.sum; count += d.count;
      }
      if (row < rows - 1) {
        const d = stripDistance(here.bottom, edgeOf(grid.at(i + columns)).top);
        sum += d.sum; count += d.count;
      }
    }
  }
  return sum / count;
}

// ---------------------------------------------------------------------------
// Genetic algorithm
// ---------------------------------------------------------------------------

export const DEFAULT_SETTINGS = Object.freeze({
  populationSize: 40,
  maxGenerations: 100,
  mutationRate: 0.05,
  crossoverRate: 0.7,
  fitnessType: 1,
  edgeThickness: 1,
});

export class Individual {
  constructor(encoding, fitnessValue) {
    this.encoding = encoding;
    this.fitness = fitnessValue;
  }
}

export class GeneticAlgorithm {
  constructor(set, settings = {}) {
    this.set = set;
    this.settings = { ...DEFAULT_SETTINGS, ...settings };
    this.population = [];
    this.generation = 0;
  }

  evaluate(encoding) {
    return new Individual(encoding, fitness(encoding, this.set, {
      type: this.settings.fitnessType,
      edgeThickness: this.settings.edgeThickness,
    }));
  }

  /** Re-score everybody (e.g. after the fitness settings change). */
  rescore() {
    this.population = this.population.map((ind) => this.evaluate(ind.encoding));
    this.sort();
  }

  sort() { this.population.sort((a, b) => a.fitness - b.fitness); }
  get best() { return this.population[0]; }
  get finished() { return this.generation >= this.settings.maxGenerations; }

  initialise() {
    const size = Math.max(2, this.settings.populationSize);
    while (this.population.length < size) {
      this.population.push(this.evaluate(GridEncoding.random(this.set.count, this.set.rotations)));
    }
    this.sort();
    this.crop();
  }

  crop() {
    const size = Math.max(2, this.settings.populationSize);
    if (this.population.length > size) this.population.length = size;
  }

  /** Rank-biased selection favouring the fittest (index = (U·√N)²). */
  selectParent() {
    const n = this.population.length;
    const idx = Math.floor((random() * Math.sqrt(n)) ** 2);
    return this.population[Math.min(idx, n - 1)];
  }

  /** One generation: breed a full set of children, merge with parents, keep the best N. */
  step() {
    if (this.population.length < Math.max(2, this.settings.populationSize)) this.initialise();
    this.generation++;
    this.sort();
    const { rotations } = this.set;
    const children = [];
    while (children.length < this.settings.populationSize) {
      const p1 = this.selectParent();
      let p2 = p1;
      while (p2 === p1) p2 = this.selectParent();
      const kids = random() < this.settings.crossoverRate
        ? p1.encoding.crossover(p2.encoding)
        : [p1.encoding.clone(), p2.encoding.clone()];
      for (const kid of kids) {
        kid.mutate(this.settings.mutationRate, rotations);
        children.push(this.evaluate(kid));
      }
    }
    this.population.push(...children);
    this.sort();
    this.crop();
    return this.best;
  }

  toJSON() {
    return {
      settings: this.settings,
      generation: this.generation,
      population: this.population.map((ind) => ({ fitness: ind.fitness, grid: ind.encoding.toJSON() })),
    };
  }

  static fromJSON(set, obj) {
    const ga = new GeneticAlgorithm(set, obj.settings);
    ga.generation = obj.generation | 0;
    ga.population = obj.population.map((p) => ga.evaluate(GridEncoding.fromJSON(p.grid).constrainRotations(set.rotations)));
    ga.sort();
    return ga;
  }
}
