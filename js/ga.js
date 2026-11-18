// Evolving Art — genetic algorithm and fitness.
// A port of the original Silverlight C# (GeneticAlgorithm.cs, Individual.cs,
// PaintingGridEncoding.cs, PaintingEncoding.cs, ImageHolder.cs, Pixel.cs).
//
// This module has no browser dependencies, so it can also run in Node or a Web Worker.
// Image data is supplied as objects shaped like ImageData: { width, height, data: RGBA bytes }.

export const ROWS = 3;
export const COLUMNS = 4;
export const NUMBER_OF_PAINTINGS = ROWS * COLUMNS;

let random = Math.random;
/** Replace the random number source (e.g. with a seeded generator for testing). */
export function setRandom(fn) { random = fn; }
const nextInt = (n) => Math.floor(random() * n);
const flipCoin = () => random() < 0.5;

// ---------------------------------------------------------------------------
// Encodings
// ---------------------------------------------------------------------------

/** One panel in one slot: which painting, upside-down or not, which face showing. */
export class PaintingEncoding {
  constructor(paintingIndex, rotated, frontVisible) {
    this.paintingIndex = paintingIndex;
    this.rotated = rotated;
    this.frontVisible = frontVisible;
  }
  clone() { return new PaintingEncoding(this.paintingIndex, this.rotated, this.frontVisible); }
  mutate(rate) {
    if (random() < rate && flipCoin()) this.rotated = !this.rotated;
    if (random() < rate && flipCoin()) this.frontVisible = !this.frontVisible;
  }
}

/** A whole arrangement: 12 PaintingEncodings in row-major slot order. */
export class GridEncoding {
  constructor(encodings = []) { this.encodings = encodings; }

  static random() {
    const unplaced = [...Array(NUMBER_OF_PAINTINGS).keys()];
    const encodings = [];
    while (unplaced.length > 0) {
      const paintingIndex = unplaced.splice(nextInt(unplaced.length), 1)[0];
      const rotated = !flipCoin();
      const frontVisible = !flipCoin();
      encodings.push(new PaintingEncoding(paintingIndex, rotated, frontVisible));
    }
    return new GridEncoding(encodings);
  }

  clone() { return new GridEncoding(this.encodings.map((e) => e.clone())); }
  at(i) { return this.encodings[i]; }
  find(paintingIndex) { return this.encodings.find((e) => e.paintingIndex === paintingIndex); }

  /** Swap-based mutation: with probability `rate` per slot, swap with a random painting
   *  and possibly toggle its rotation/face. */
  mutate(rate) {
    for (const enc of this.encodings) {
      if (random() < rate) {
        const newIndex = nextInt(NUMBER_OF_PAINTINGS);
        const swap = this.find(newIndex);
        swap.paintingIndex = enc.paintingIndex;
        enc.paintingIndex = newIndex;
        enc.mutate(rate);
      }
    }
  }

  /** "Order crossover (modified)": half the slots are inherited directly from one parent;
   *  the rest are filled from the other parent where that doesn't duplicate a painting,
   *  and any conflicts are filled at random from the unused paintings. */
  crossover(other) {
    const crossoverPoints = [...Array(NUMBER_OF_PAINTINGS).keys()];
    const remaining1 = [];
    const remaining2 = [];
    const free1 = other.encodings.map((e) => e.clone());
    const free2 = this.encodings.map((e) => e.clone());
    while (crossoverPoints.length > NUMBER_OF_PAINTINGS / 2) {
      const [p] = crossoverPoints.splice(nextInt(crossoverPoints.length), 1);
      remaining1.push(p);
      remaining2.push(p);
    }
    const child1 = this.#childFromCrossover(other, true, crossoverPoints, remaining1, free1);
    const child2 = this.#childFromCrossover(other, false, crossoverPoints, remaining2, free2);
    child1.#crossoverFlags(this, other);
    child2.#crossoverFlags(this, other);
    return [child1, child2];
  }

  #childFromCrossover(other, useThis, crossoverPoints, remainingPoints, freeEncodings) {
    const source = useThis ? this : other;
    const otherSource = useThis ? other : this;
    const slots = new Array(NUMBER_OF_PAINTINGS);

    for (const i of crossoverPoints) {
      const enc = source.at(i).clone();
      slots[i] = enc;
      const j = freeEncodings.findIndex((f) => f.paintingIndex === enc.paintingIndex);
      if (j >= 0) freeEncodings.splice(j, 1);
    }
    for (let i = 0; i < remainingPoints.length;) {
      const slot = remainingPoints[i];
      const wanted = otherSource.at(slot).paintingIndex;
      const j = freeEncodings.findIndex((f) => f.paintingIndex === wanted);
      if (j >= 0) {
        slots[slot] = freeEncodings[j];
        freeEncodings.splice(j, 1);
        remainingPoints.splice(i, 1);
      } else {
        i++;
      }
    }
    for (const slot of remainingPoints) {
      slots[slot] = freeEncodings.splice(nextInt(freeEncodings.length), 1)[0];
    }
    return new GridEncoding(slots.map((e) => e.clone()));
  }

  #crossoverFlags(parent1, parent2) {
    for (const enc of this.encodings) {
      enc.rotated = (flipCoin() ? parent1 : parent2).find(enc.paintingIndex).rotated;
      enc.frontVisible = (flipCoin() ? parent1 : parent2).find(enc.paintingIndex).frontVisible;
    }
  }

  // --- Serialisation -------------------------------------------------------
  toJSON() {
    return this.encodings.map((e) => ({ painting: e.paintingIndex, rotated: e.rotated, front: e.frontVisible }));
  }
  static fromJSON(arr) {
    return new GridEncoding(arr.map((o) => new PaintingEncoding(o.painting, o.rotated, o.front)));
  }
  /** Compact form for URLs, e.g. "4a,7bR,..." (1-based painting number, face a/b, R = upside down). */
  toToken() {
    return this.encodings.map((e) => `${e.paintingIndex + 1}${e.frontVisible ? 'a' : 'b'}${e.rotated ? 'R' : ''}`).join(',');
  }
  static fromToken(token) {
    const parts = token.split(',');
    if (parts.length !== NUMBER_OF_PAINTINGS) throw new Error('Wrong number of panels');
    const enc = parts.map((p) => {
      const m = /^(\d{1,2})([ab])(R?)$/.exec(p);
      if (!m) throw new Error(`Bad panel token "${p}"`);
      return new PaintingEncoding(+m[1] - 1, m[3] === 'R', m[2] === 'a');
    });
    const seen = new Set(enc.map((e) => e.paintingIndex));
    if (seen.size !== NUMBER_OF_PAINTINGS || [...seen].some((i) => i < 0 || i >= NUMBER_OF_PAINTINGS)) {
      throw new Error('Each painting must appear exactly once');
    }
    return new GridEncoding(enc);
  }
}

// ---------------------------------------------------------------------------
// Image analysis (replaces ImageHolder / WriteableBitmap pixel access)
// ---------------------------------------------------------------------------

/** Average `thickness` pixels inward from an edge, one value per position along that edge.
 *  Returns an Int32Array of [a,r,g,b, a,r,g,b, ...] with C#-style integer averaging. */
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

function averagePixel(img) {
  const { data } = img;
  const n = data.length / 4;
  let a = 0, r = 0, g = 0, b = 0;
  for (let p = 0; p < data.length; p += 4) { r += data[p]; g += data[p + 1]; b += data[p + 2]; a += data[p + 3]; }
  return Int32Array.of(Math.trunc(a / n), Math.trunc(r / n), Math.trunc(g / n), Math.trunc(b / n));
}

/**
 * Pre-computed per-panel data used by the fitness functions.
 * `faces[i] = { front: ImageData, back: ImageData }` for i = 0..11.
 */
export class PaintingSet {
  constructor(faces) {
    this.faces = faces;
    this.edgeCache = new Map(); // thickness -> edges[painting][face][rotated] = {left,right,top,bottom}
    this.averages = faces.map((f) => ({ front: averagePixel(f.front), back: averagePixel(f.back) }));
  }

  edges(thickness) {
    if (!this.edgeCache.has(thickness)) {
      this.edgeCache.set(thickness, this.faces.map((f) => {
        const forFace = (img) => {
          const t = Math.max(1, Math.min(thickness, img.width, img.height));
          const left = edgeStrip(img, 'left', t), right = edgeStrip(img, 'right', t);
          const top = edgeStrip(img, 'top', t), bottom = edgeStrip(img, 'bottom', t);
          return {
            upright: { left, right, top, bottom },
            // Turned 180°: each edge becomes the opposite edge, read in reverse.
            rotated: { left: reversed(right), right: reversed(left), top: reversed(bottom), bottom: reversed(top) },
          };
        };
        // Both faces are analysed — the original loaded the front image for the back too.
        return { front: forFace(f.front), back: forFace(f.back) };
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
  let sum = 0, count = 0;
  if (type === 0) {
    const avg = (e) => set.averages[e.paintingIndex][e.frontVisible ? 'front' : 'back'];
    for (let row = 0; row < ROWS; row++) {
      for (let col = 0; col < COLUMNS; col++) {
        const i = row * COLUMNS + col;
        const here = avg(grid.at(i));
        if (col < COLUMNS - 1) { sum += pixelDistance(here, avg(grid.at(i + 1))); count++; }
        if (row < ROWS - 1) { sum += pixelDistance(here, avg(grid.at(i + COLUMNS))); count++; }
      }
    }
    return sum / count;
  }
  const edges = set.edges(edgeThickness);
  const edgeOf = (e) => edges[e.paintingIndex][e.frontVisible ? 'front' : 'back'][e.rotated ? 'rotated' : 'upright'];
  for (let row = 0; row < ROWS; row++) {
    for (let col = 0; col < COLUMNS; col++) {
      const i = row * COLUMNS + col;
      const here = edgeOf(grid.at(i));
      if (col < COLUMNS - 1) {
        const d = stripDistance(here.right, edgeOf(grid.at(i + 1)).left);
        sum += d.sum; count += d.count;
      }
      if (row < ROWS - 1) {
        const d = stripDistance(here.bottom, edgeOf(grid.at(i + COLUMNS)).top);
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
    while (this.population.length < this.settings.populationSize) {
      this.population.push(this.evaluate(GridEncoding.random()));
    }
    this.sort();
    this.crop();
  }

  crop() {
    if (this.population.length > this.settings.populationSize) {
      this.population.length = this.settings.populationSize;
    }
  }

  /** Rank-biased selection favouring the fittest (index = (U·√N)²). */
  selectParent() {
    const n = this.population.length;
    const idx = Math.floor((random() * Math.sqrt(n)) ** 2);
    return this.population[Math.min(idx, n - 1)];
  }

  /** One generation: breed a full set of children, merge with parents, keep the best N. */
  step() {
    if (this.population.length < this.settings.populationSize) this.initialise();
    this.generation++;
    this.sort();
    const children = [];
    while (children.length < this.settings.populationSize) {
      const p1 = this.selectParent();
      let p2 = p1;
      while (p2 === p1) p2 = this.selectParent();
      const kids = random() < this.settings.crossoverRate
        ? p1.encoding.crossover(p2.encoding)
        : [p1.encoding.clone(), p2.encoding.clone()];
      for (const kid of kids) {
        kid.mutate(this.settings.mutationRate);
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
    ga.population = obj.population.map((p) => ga.evaluate(GridEncoding.fromJSON(p.grid)));
    ga.sort();
    return ga;
  }
}
