// Evolving Art — browser front end.
// Replaces MainPage, PaintingGrid and Painting (XAML + code-behind) from both Silverlight versions.

import {
  PANEL_ROWS, PANEL_COLUMNS, NUMBER_OF_PANELS, DEFAULT_SETTINGS,
  PaintingEncoding, GridEncoding, PaintingSet, GeneticAlgorithm, fitness, normaliseAngle,
} from './ga.js';
import { PAINTING_SETS, DEFAULT_SET, findSet, normaliseSetId } from './sets.js';

const SLIDESHOW_DEFAULT = 6;
const STORAGE_KEY = 'evolvingArt.settings.v1';

const imageUrl = (set, panel, face) => `${set.images}/${panel}${face}.png`;
const soundUrl = (set, name) => `${set.sounds.folder}/${name}.mp3`;

const $ = (id) => document.getElementById(id);
const body = document.body;

// ---------------------------------------------------------------------------
// Settings (persisted per viewer where storage is available)
// ---------------------------------------------------------------------------

const settings = {
  set: DEFAULT_SET,
  slideSeconds: SLIDESHOW_DEFAULT,
  pace: 'slide',
  ...DEFAULT_SETTINGS,
};
try { Object.assign(settings, JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}')); } catch { /* storage unavailable */ }
settings.set = normaliseSetId(settings.set) || DEFAULT_SET;

function persistSettings() {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(settings)); } catch { /* ignore */ }
}
const gaSettings = () => ({
  populationSize: settings.populationSize,
  maxGenerations: settings.maxGenerations,
  mutationRate: settings.mutationRate,
  crossoverRate: settings.crossoverRate,
  fitnessType: settings.fitnessType,
  edgeThickness: settings.edgeThickness,
});

// ---------------------------------------------------------------------------
// Sound
// ---------------------------------------------------------------------------

class SoundEngine {
  constructor() {
    this.enabled = true;
    this.ctx = null;
    this.buffers = new Map();       // url -> Promise<AudioBuffer|null>
    this.voices = new Map();        // face key (e.g. "6b") -> { source, gain }
    this.set = null;
    this.faces = new Set();
    this.background = new Audio();
    this.background.loop = true;
    this.background.preload = 'none';
    this.wantBackground = false;
  }

  unlock() {
    if (!this.ctx) {
      const Ctx = window.AudioContext || window.webkitAudioContext;
      if (!Ctx) return;
      this.ctx = new Ctx();
      this.master = this.ctx.createGain();
      this.master.gain.value = this.enabled ? 1 : 0;
      this.master.connect(this.ctx.destination);
      this.epoch = this.ctx.currentTime;
    }
    if (this.ctx.state === 'suspended') this.ctx.resume();
  }

  useSet(set) {
    this.stopAllVoices();
    this.set = set;
    this.faces = new Set(set.sounds?.faces || []);
    this.background.pause();
    if (set.sounds?.background) this.background.src = soundUrl(set, set.sounds.background);
    else this.background.removeAttribute('src');
    this.background.load();
    this.syncBackground();
  }

  setEnabled(on) {
    this.enabled = on;
    if (this.master) this.master.gain.setTargetAtTime(on ? 1 : 0, this.ctx.currentTime, 0.05);
    this.syncBackground();
  }

  setBackground(on) { this.wantBackground = on; this.syncBackground(); }

  syncBackground() {
    const play = this.enabled && this.wantBackground && !!this.set?.sounds?.background && this.ctx;
    if (play && this.background.paused) this.background.play().catch(() => {});
    else if (!play && !this.background.paused) this.background.pause();
  }

  #buffer(url) {
    if (!this.buffers.has(url)) {
      this.buffers.set(url, fetch(url)
        .then((r) => (r.ok ? r.arrayBuffer() : Promise.reject(new Error(r.status))))
        .then((data) => this.ctx.decodeAudioData(data))
        .catch(() => null));
    }
    return this.buffers.get(url);
  }

  /** A panel face's loop plays while that face is showing. Loops are phase-aligned. */
  update(visibleFaces) {
    if (!this.ctx) return;
    const wanted = new Set([...visibleFaces].filter((k) => this.faces.has(k)));
    for (const key of [...this.voices.keys()]) if (!wanted.has(key)) this.#stopVoice(key);
    for (const key of wanted) if (!this.voices.has(key)) this.#startVoice(key);
  }

  async #startVoice(key) {
    const set = this.set;
    const voice = { source: null, gain: null };
    this.voices.set(key, voice);
    const buffer = await this.#buffer(soundUrl(set, key));
    if (!buffer || this.voices.get(key) !== voice || this.set !== set) return;
    const source = this.ctx.createBufferSource();
    source.buffer = buffer;
    source.loop = true;
    const gain = this.ctx.createGain();
    const now = this.ctx.currentTime;
    gain.gain.setValueAtTime(0, now);
    gain.gain.linearRampToValueAtTime(0.9, now + 0.3);
    source.connect(gain).connect(this.master);
    source.start(now, (now - this.epoch) % buffer.duration);
    voice.source = source;
    voice.gain = gain;
  }

  #stopVoice(key) {
    const voice = this.voices.get(key);
    this.voices.delete(key);
    if (!voice?.source) return;
    const now = this.ctx.currentTime;
    voice.gain.gain.cancelScheduledValues(now);
    voice.gain.gain.setValueAtTime(voice.gain.gain.value, now);
    voice.gain.gain.linearRampToValueAtTime(0, now + 0.4);
    voice.source.stop(now + 0.45);
  }

  stopAllVoices() { for (const k of [...this.voices.keys()]) this.#stopVoice(k); }
}

const sound = new SoundEngine();

// ---------------------------------------------------------------------------
// Panels
// ---------------------------------------------------------------------------

const ICONS = {
  flipH: '<svg viewBox="0 0 24 24"><path d="M12 3v18"/><path d="M8 7 4 12l4 5"/><path d="M16 7l4 5-4 5"/></svg>',
  spin: '<svg viewBox="0 0 24 24"><path d="M20 12a8 8 0 1 1-2.34-5.66"/><path d="M20 4v4h-4"/></svg>',
  flipV: '<svg viewBox="0 0 24 24"><path d="M3 12h18"/><path d="M7 8l5-4 5 4"/><path d="M7 16l5 4 5-4"/></svg>',
  move: '<svg viewBox="0 0 24 24"><path d="M12 3v18M3 12h18"/><path d="M9 6l3-3 3 3M9 18l3 3 3-3M6 9l-3 3 3 3M18 9l3 3-3 3"/></svg>',
};

const mod360 = (a) => ((a % 360) + 360) % 360;

/** One double-sided panel, shown as a 3D card. */
class Panel {
  constructor(index) {
    this.index = index;                                  // 0..11
    // Orientation as seen on screen: which face is showing, and how far it is turned clockwise.
    this.frontVisible = true;
    this.rotation = 0;
    this.animating = false;
    this.slot = index;

    this.el = document.createElement('div');
    this.el.className = 'piece';
    this.el.tabIndex = 0;
    this.el.setAttribute('role', 'gridcell');
    this.card = document.createElement('div');
    this.card.className = 'card';
    this.front = document.createElement('div');
    this.front.className = 'face front';
    this.back = document.createElement('div');
    this.back.className = 'face back';
    this.card.append(this.front, this.back);
    this.el.append(this.card);

    this.card.addEventListener('transitionend', (e) => {
      if (e.target === this.card && e.propertyName === 'transform') this.normalise();
    });
    this.el.addEventListener('click', () => onPanelClick(this));
    this.el.addEventListener('pointerenter', (e) => { if (e.pointerType === 'mouse') attachControls(this); });
    this.el.addEventListener('focus', () => attachControls(this));
    this.setSlot(index);
    this.show(Panel.pose(true, 0));
  }

  get encoding() { return new PaintingEncoding(this.index, this.rotation, this.frontVisible); }
  get faceKey() { return `${this.index + 1}${this.frontVisible ? 'a' : 'b'}`; }

  setImages(set) {
    this.front.style.backgroundImage = `url("${imageUrl(set, this.index + 1, 'a')}")`;
    this.back.style.backgroundImage = `url("${imageUrl(set, this.index + 1, 'b')}")`;
    this.updateLabel();
  }

  updateLabel() {
    const turn = { 0: '', 90: ', turned a quarter turn clockwise', 180: ', upside down', 270: ', turned a quarter turn anticlockwise' }[this.rotation];
    this.el.setAttribute('aria-label', `Panel ${this.index + 1}, ${this.frontVisible ? 'front' : 'back'}${turn}`);
  }

  setSlot(slot) {
    this.slot = slot;
    this.el.style.setProperty('--col', slot % state.columns);
    this.el.style.setProperty('--row', Math.floor(slot / state.columns));
  }

  /** The resting transform for an orientation: turn the card over (about its vertical axis) to show
   *  the right face, then turn it clockwise in the plane of the screen. */
  static pose(frontVisible, rotation) {
    return `rotateZ(${rotation}deg) rotateY(${frontVisible ? 0 : 180}deg)`;
  }

  /** Jump to `from`, then (if given) animate to `to`. Both must use the same list of transform
   *  functions so the browser interpolates each angle rather than the matrices. */
  show(from, to = null) {
    this.card.classList.add('snap');
    this.card.style.transform = from;
    void this.card.offsetWidth; // apply without transition
    this.card.classList.remove('snap');
    if (to) {
      this.animating = true;
      this.card.style.transform = to;
    }
    this.updateLabel();
  }

  /** After an animation, swap to the equivalent resting transform (no visible change). */
  normalise() {
    if (!this.animating) return;
    this.animating = false;
    this.show(Panel.pose(this.frontVisible, this.rotation));
  }

  /** Apply a turn/flip in screen space, prefixed onto the current pose, and animate it. */
  #act(fn, angle, frontVisible, rotation) {
    const rest = Panel.pose(this.frontVisible, this.rotation);
    this.frontVisible = frontVisible;
    this.rotation = mod360(rotation);
    this.show(`${fn}(0deg) ${rest}`, `${fn}(${angle}deg) ${rest}`);
  }

  // Turning over about the screen's vertical axis mirrors the turn: r → −r.
  flipHorizontal() { this.#act('rotateY', 180, !this.frontVisible, -this.rotation); }
  // Turning over about the screen's horizontal axis: r → 180° − r.
  flipVertical() { this.#act('rotateX', 180, !this.frontVisible, 180 - this.rotation); }
  turn() {
    const step = state.set.rotations.length === 4 ? 90 : 180;
    this.#act('rotateZ', step, this.frontVisible, this.rotation + step);
  }

  /** Show the given face/rotation, animating the shortest way there. */
  setState(frontVisible, rotation, animate) {
    if (!animate) {
      this.animating = false;
      this.frontVisible = frontVisible;
      this.rotation = rotation;
      this.show(Panel.pose(frontVisible, rotation));
      return;
    }
    let dz = mod360(rotation - this.rotation);
    if (dz === 270) dz = -90;
    const fromY = this.frontVisible ? 0 : 180;
    const toY = fromY + (frontVisible !== this.frontVisible ? 180 : 0);
    const from = `rotateZ(${this.rotation}deg) rotateY(${fromY}deg)`;
    const to = `rotateZ(${this.rotation + dz}deg) rotateY(${toY}deg)`;
    this.frontVisible = frontVisible;
    this.rotation = rotation;
    if (from === to) { this.show(Panel.pose(frontVisible, rotation)); return; }
    this.show(from, to);
  }
}

// Shared hover/tap controls, moved into whichever panel is active.
const controls = document.createElement('div');
controls.className = 'controls';
function controlButton(cls, label, icon, action) {
  const b = document.createElement('button');
  b.className = cls;
  b.type = 'button';
  b.title = label;
  b.setAttribute('aria-label', label);
  b.innerHTML = icon;
  b.addEventListener('click', (e) => { e.stopPropagation(); if (controls.piece) action(controls.piece); });
  controls.append(b);
  return b;
}
controlButton('flip-h', 'Turn over sideways, to show the other side', ICONS.flipH, (p) => { p.flipHorizontal(); onGridChanged(); });
const turnButton = controlButton('spin', 'Turn upside down', ICONS.spin, (p) => { p.turn(); onGridChanged(); });
controlButton('flip-v', 'Turn over top to bottom, to show the other side', ICONS.flipV, (p) => { p.flipVertical(); onGridChanged(); });
controlButton('move', 'Move to another slot', ICONS.move, (p) => toggleMove(p));

function attachControls(piece) {
  if (mode !== 'manual' || moveFrom) return;
  if (controls.piece !== piece) {
    controls.piece?.el.classList.remove('has-controls');
    controls.piece = piece;
    piece.el.append(controls);
    piece.el.classList.add('has-controls');
  }
}

function updateTurnButton() {
  const quarter = state.set?.rotations.length === 4;
  const label = quarter ? 'Turn a quarter turn clockwise' : 'Turn upside down';
  turnButton.title = label;
  turnButton.setAttribute('aria-label', label);
}

// ---------------------------------------------------------------------------
// App state
// ---------------------------------------------------------------------------

const gridEl = $('grid');
const state = {
  setInfo: null,        // catalogue entry
  set: null,            // PaintingSet (pixel data)
  rows: PANEL_ROWS,
  columns: PANEL_COLUMNS,
  aspect: 175 / 224,
};
let pieces = [];               // Panel objects, indexed by panel number
const panelCache = new Map(); // set id -> Promise<{ panels: [{front, back}] ImageData }>

let mode = 'manual';          // 'manual' | 'slideshow' | 'evolving'
let moveFrom = null;
let slideTimer = null;
let ga = null;
let gaKey = null;             // id of the painting set the GA belongs to
let gaTimer = null;
let lastBestToken = null;
let startFitness = null;

function currentGrid() {
  const slots = new Array(pieces.length);
  for (const p of pieces) slots[p.slot] = p.encoding;
  return new GridEncoding(slots);
}

function configure(grid, animate) {
  grid.encodings.forEach((enc, slot) => {
    const p = pieces[enc.paintingIndex];
    p.setSlot(slot);
    p.setState(enc.frontVisible, enc.rotation, animate);
  });
  onGridChanged();
}

function onGridChanged() {
  sound.update(new Set(pieces.map((p) => p.faceKey)));
  if (state.set) {
    const f = fitness(currentGrid(), state.set, { type: settings.fitnessType, edgeThickness: settings.edgeThickness });
    $('current-fitness').textContent = f.toFixed(2);
  }
}

function randomise() { configure(GridEncoding.random(state.set.count, state.set.rotations), true); }

function setMode(next) {
  mode = next;
  body.classList.toggle('manual', mode === 'manual');
  sound.setBackground(mode !== 'manual');
  if (mode !== 'manual') { cancelMove(); detachControls(); }
}

function detachControls() {
  controls.piece?.el.classList.remove('has-controls', 'active');
  controls.piece = null;
  controls.remove();
}

// ---------------------------------------------------------------------------
// Loading painting sets
// ---------------------------------------------------------------------------

function loadImage(url) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(`Could not load ${url}`));
    img.src = url;
  });
}

function pixels(img) {
  const canvas = document.createElement('canvas');
  canvas.width = img.naturalWidth;
  canvas.height = img.naturalHeight;
  const ctx = canvas.getContext('2d', { willReadFrequently: true });
  ctx.drawImage(img, 0, 0);
  return ctx.getImageData(0, 0, canvas.width, canvas.height);
}

function loadPanels(setInfo) {
  if (!panelCache.has(setInfo.id)) {
    const promise = Promise.all(Array.from({ length: NUMBER_OF_PANELS }, async (_, i) => {
      const [front, back] = await Promise.all([
        loadImage(imageUrl(setInfo, i + 1, 'a')),
        loadImage(imageUrl(setInfo, i + 1, 'b')),
      ]);
      return { front: pixels(front), back: pixels(back) };
    }));
    promise.catch(() => panelCache.delete(setInfo.id));
    panelCache.set(setInfo.id, promise);
  }
  return panelCache.get(setInfo.id);
}

let loadToken = 0;

/** Load (or switch to) a painting set. */
async function useSet(setId, grid = null) {
  const setInfo = findSet(setId) || findSet(DEFAULT_SET);
  const token = ++loadToken;
  $('loading').hidden = false;
  $('loading').textContent = 'Loading panels…';
  gridEl.style.visibility = 'hidden';
  try {
    const panels = await loadPanels(setInfo);
    if (token !== loadToken) return false;
    const setChanged = state.setInfo !== setInfo;
    state.setInfo = setInfo;
    state.set = new PaintingSet(panels);
    state.rows = state.set.rows;
    state.columns = state.set.columns;
    state.aspect = state.set.panelWidth / state.set.panelHeight;
    settings.set = setInfo.id;
    persistSettings();
    $('set-choice').value = setInfo.id;
    updateEdgeThicknessLimit();
    updateTurnButton();

    detachControls();
    cancelMove();
    pieces = Array.from({ length: state.set.count }, (_, i) => new Panel(i));
    pieces.forEach((p) => p.setImages(setInfo));
    gridEl.replaceChildren(...pieces.map((p) => p.el));
    gridEl.style.setProperty('--rows', state.rows);
    gridEl.style.setProperty('--cols', state.columns);
    if (setChanged) sound.useSet(setInfo);
    layout();

    if (ga && gaKey !== setInfo.id) resetEvolution();
    const start = grid && grid.isValid(state.set.count)
      ? grid.constrainRotations(state.set.rotations)
      : GridEncoding.random(state.set.count, state.set.rotations);
    configure(start, false);
    gridEl.style.visibility = '';
    $('loading').hidden = true;
    return true;
  } catch (err) {
    $('loading').textContent = `Sorry — the panels could not be loaded. ${err.message}`;
    console.error(err);
    return false;
  }
}

function layout() {
  const stage = $('stage').getBoundingClientRect();
  const pad = Math.min(48, stage.width * 0.05);
  const w = Math.max(100, stage.width - pad * 2);
  const h = Math.max(100, stage.height - pad * 2);
  const cw = Math.floor(Math.min(w / state.columns, (h / state.rows) * state.aspect));
  const ch = Math.floor(cw / state.aspect);
  document.documentElement.style.setProperty('--cw', `${cw}px`);
  document.documentElement.style.setProperty('--ch', `${ch}px`);
}
window.addEventListener('resize', layout);
$('stage').addEventListener('transitionend', layout);

// ---------------------------------------------------------------------------
// Manual interaction
// ---------------------------------------------------------------------------

function onPanelClick(piece) {
  if (mode === 'evolving') {
    flash('evolve-status', 'Pause evolution to rearrange panels by hand.');
    return;
  }
  if (mode === 'slideshow') stopSlideshow();
  if (moveFrom) {
    if (piece !== moveFrom) swap(moveFrom, piece);
    cancelMove();
    return;
  }
  // Tap support for touch screens (mouse users see the controls on hover).
  const wasActive = piece.el.classList.contains('active') && controls.piece === piece;
  attachControls(piece);
  for (const p of pieces) p.el.classList.remove('active');
  if (!wasActive) piece.el.classList.add('active');
}

function toggleMove(piece) {
  if (moveFrom === piece) { cancelMove(); return; }
  cancelMove();
  moveFrom = piece;
  piece.el.classList.add('move-from');
  body.classList.add('moving');
}

function cancelMove() {
  moveFrom?.el.classList.remove('move-from');
  moveFrom = null;
  body.classList.remove('moving');
  for (const p of pieces) p.el.classList.remove('active');
}

function swap(a, b) {
  const slotA = a.slot;
  a.el.classList.add('lifted');
  a.setSlot(b.slot);
  b.setSlot(slotA);
  setTimeout(() => a.el.classList.remove('lifted'), 750);
  onGridChanged();
}

// ---------------------------------------------------------------------------
// Slideshow
// ---------------------------------------------------------------------------

function startSlideshow() {
  if (mode === 'evolving') pauseEvolution();
  setMode('slideshow');
  randomise();
  clearInterval(slideTimer);
  slideTimer = setInterval(randomise, settings.slideSeconds * 1000);
  $('slideshow').textContent = 'Stop slideshow';
  $('slideshow').setAttribute('aria-pressed', 'true');
}

function stopSlideshow() {
  clearInterval(slideTimer);
  slideTimer = null;
  if (mode === 'slideshow') setMode('manual');
  $('slideshow').textContent = 'Start random slideshow';
  $('slideshow').setAttribute('aria-pressed', 'false');
}

// ---------------------------------------------------------------------------
// Evolution
// ---------------------------------------------------------------------------

function paceMs() {
  if (settings.pace === 'fast') return 80;
  if (settings.pace === 'slide') return settings.slideSeconds * 1000;
  return Number(settings.pace) || 1000;
}

function startEvolution() {
  if (!state.set) return;
  if (mode === 'slideshow') stopSlideshow();
  if (!ga) {
    ga = new GeneticAlgorithm(state.set, gaSettings());
    gaKey = state.setInfo.id;
    startFitness = null;
  } else {
    const before = ga.settings;
    ga.settings = { ...before, ...gaSettings() };
    if (before.fitnessType !== ga.settings.fitnessType || before.edgeThickness !== ga.settings.edgeThickness) ga.rescore();
  }
  if (ga.finished) {
    flash('evolve-status', `Reached ${ga.settings.maxGenerations} generations. Raise “Max generations” in Settings, or reset.`);
    return;
  }
  setMode('evolving');
  if (ga.population.length === 0) {
    ga.initialise();
    startFitness = ga.best.fitness;
    showBest();
  }
  updateEvolveUI();
  gaTimer = setTimeout(evolveTick, Math.min(paceMs(), 1500));
}

function evolveTick() {
  if (mode !== 'evolving') return;
  ga.step();
  showBest();
  updateEvolveUI();
  if (ga.finished) {
    pauseEvolution();
    flash('evolve-status', 'Evolution complete.');
  } else {
    gaTimer = setTimeout(evolveTick, paceMs());
  }
}

function showBest() {
  const token = ga.best.encoding.toToken();
  if (token !== lastBestToken) {
    lastBestToken = token;
    configure(ga.best.encoding, true);
  }
}

function pauseEvolution() {
  clearTimeout(gaTimer);
  gaTimer = null;
  if (mode === 'evolving') setMode('manual');
  updateEvolveUI();
}

function resetEvolution() {
  pauseEvolution();
  ga = null;
  gaKey = null;
  lastBestToken = null;
  startFitness = null;
  updateEvolveUI();
}

function updateEvolveUI() {
  const running = mode === 'evolving';
  $('evolve').textContent = running ? 'Pause evolution' : (ga && ga.generation > 0 ? 'Continue evolution' : 'Start evolution');
  $('reset-evolve').hidden = !ga || running;
  $('save-ga').disabled = !ga || running || ga.population.length === 0;
  $('load-ga').disabled = running;
  $('evolve-progress').hidden = !ga;
  if (ga) {
    const max = ga.settings.maxGenerations;
    $('generation').textContent = `${ga.generation} / ${max}`;
    $('progress-fill').style.width = `${Math.min(100, (ga.generation / max) * 100)}%`;
    $('best-fitness').textContent = ga.best ? ga.best.fitness.toFixed(2) : '–';
    $('start-fitness').textContent = startFitness != null ? startFitness.toFixed(2) : '–';
  }
}

// ---------------------------------------------------------------------------
// Save / load (JSON, plus XML files saved by either Silverlight version)
// ---------------------------------------------------------------------------

function download(filename, data) {
  const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }));
  const a = Object.assign(document.createElement('a'), { href: url, download: filename });
  document.body.append(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function pickFile() {
  return new Promise((resolve) => {
    const input = $('file-input');
    input.value = '';
    input.onchange = () => resolve(input.files[0] || null);
    input.click();
  });
}

const xmlBool = (s) => String(s).trim().toLowerCase() === 'true';
const xmlText = (el, name) => el?.querySelector(`:scope > ${name}`)?.textContent ?? null;

/** Rectangular version: <Rotated>True</Rotated>; square version: <Rotation>90</Rotation>. */
function gridFromXml(el) {
  return new GridEncoding([...el.querySelectorAll(':scope > PaintingEncoding')].map((pe) => {
    const rotation = xmlText(pe, 'Rotation');
    return new PaintingEncoding(
      parseInt(xmlText(pe, 'PaintingIndex'), 10),
      rotation != null ? normaliseAngle(Number(rotation)) : (xmlBool(xmlText(pe, 'Rotated')) ? 180 : 0),
      xmlBool(xmlText(pe, 'FrontVisible')),
    );
  }));
}
const isSquareXml = (el) => !!el && (el.querySelector(':scope > DimensionMultiplier') || el.querySelector(':scope > PaintingEncoding > Rotation'));

const SUBDIVIDED = 'That file is from the square version with panels cut into smaller pieces, which this version doesn’t use.';
const whole = (m) => { if ((parseInt(m, 10) || 1) !== 1) throw new Error(SUBDIVIDED); };

/** Returns { kind: 'grid'|'ga', set, grid?, ga? } from a JSON or legacy XML file. */
async function parseSaveFile(file) {
  const text = await file.text();
  if (text.trimStart().startsWith('<')) {
    const doc = new DOMParser().parseFromString(text, 'application/xml');
    if (doc.querySelector('parsererror')) throw new Error('Not a valid XML file');
    const root = doc.documentElement;
    if (root.nodeName === 'PaintingGridEncoding') {
      const square = isSquareXml(root);
      whole(xmlText(root, 'DimensionMultiplier'));
      return {
        kind: 'grid',
        set: normaliseSetId(xmlText(root, 'PaintingSelection'), { square }),
        grid: gridFromXml(root),
      };
    }
    if (root.nodeName === 'GeneticAlgorithm') {
      const individuals = [...root.querySelectorAll(':scope > Population > Individual > PaintingGridEncoding')];
      const square = isSquareXml(individuals[0]);
      whole(xmlText(individuals[0], 'DimensionMultiplier'));
      const num = (n, d) => { const v = Number(xmlText(root, n)); return Number.isFinite(v) && xmlText(root, n) != null ? v : d; };
      return {
        kind: 'ga',
        set: normaliseSetId(xmlText(root, 'PaintingSelection'), { square }),
        ga: {
          settings: {
            maxGenerations: num('MaxGenerations', 100),
            populationSize: num('PopulationSize', 40),
            mutationRate: num('MutationRate', 0.05),
            crossoverRate: num('CrossoverRate', 0.7),
            fitnessType: num('FitnessType', 1),
            edgeThickness: num('EdgeThickness', 1),
          },
          generation: num('Generation', 0),
          population: individuals.map((g) => ({ grid: gridFromXml(g).toJSON() })),
        },
      };
    }
    throw new Error(`Unrecognised file (${root.nodeName})`);
  }
  const obj = JSON.parse(text);
  const set = normaliseSetId(obj.set);
  whole(obj.multiplier);
  if (obj.type === 'evolving-art/grid') return { kind: 'grid', set, grid: GridEncoding.fromJSON(obj.grid) };
  if (obj.type === 'evolving-art/ga') return { kind: 'ga', set, ga: obj.ga };
  throw new Error('Unrecognised file');
}

async function loadGridFile() {
  const file = await pickFile();
  if (!file) return;
  try {
    const result = await parseSaveFile(file);
    if (result.kind !== 'grid') throw new Error('That file is an evolution run — use “Load run…” on the Evolve tab.');
    if (!result.grid.isValid(NUMBER_OF_PANELS)) throw new Error('The arrangement in that file is incomplete.');
    stopSlideshow();
    if (mode === 'evolving') pauseEvolution();
    const setId = result.set || settings.set;
    if (setId === state.setInfo?.id) {
      configure(result.grid.constrainRotations(state.set.rotations), true);
    } else {
      await useSet(setId, result.grid);
    }
    flash('explore-status', 'Arrangement loaded.');
  } catch (err) {
    flash('explore-status', err.message);
  }
}

async function loadGaFile() {
  const file = await pickFile();
  if (!file) return;
  try {
    const result = await parseSaveFile(file);
    if (result.kind !== 'ga') throw new Error('That file is a single arrangement — use “Load…” on the Explore tab.');
    const grids = result.ga.population.map((p) => GridEncoding.fromJSON(p.grid));
    if (grids.length < 2 || !grids.every((g) => g.isValid(NUMBER_OF_PANELS))) throw new Error('The population in that file is incomplete.');
    stopSlideshow();
    pauseEvolution();
    const setId = result.set || settings.set;
    if (setId !== state.setInfo?.id) await useSet(setId);
    const loaded = GeneticAlgorithm.fromJSON(state.set, result.ga); // re-scored with the current code
    ga = loaded;
    gaKey = state.setInfo.id;
    startFitness = null;
    Object.assign(settings, ga.settings);
    persistSettings();
    syncSettingsForm();
    lastBestToken = null;
    showBest();
    updateEvolveUI();
    flash('evolve-status', `Run loaded at generation ${ga.generation}.`);
  } catch (err) {
    flash('evolve-status', err.message);
  }
}

function shareLink() {
  history.replaceState(null, '', `#set=${state.setInfo.id}&grid=${currentGrid().toToken()}`);
  return location.href;
}

function readHash() {
  const params = new URLSearchParams(location.hash.slice(1));
  const set = normaliseSetId(params.get('set'));
  let grid = null;
  try { if (params.get('grid')) grid = GridEncoding.fromToken(params.get('grid')); } catch { grid = null; }
  if (grid && !grid.isValid(NUMBER_OF_PANELS)) grid = null;
  return { set, grid };
}

// ---------------------------------------------------------------------------
// UI wiring
// ---------------------------------------------------------------------------

const flashTimers = {};
function flash(id, message) {
  const el = $(id);
  el.textContent = message;
  clearTimeout(flashTimers[id]);
  flashTimers[id] = setTimeout(() => { el.textContent = ''; }, 5000);
}

// Tabs
const tabs = [...document.querySelectorAll('[role="tab"]')];
function selectTab(tab) {
  for (const t of tabs) {
    const selected = t === tab;
    t.setAttribute('aria-selected', String(selected));
    $(t.getAttribute('aria-controls')).hidden = !selected;
  }
}
tabs.forEach((t) => t.addEventListener('click', () => selectTab(t)));

// Explore
$('randomise').addEventListener('click', () => { stopSlideshow(); if (mode === 'evolving') pauseEvolution(); randomise(); });
$('slideshow').addEventListener('click', () => (mode === 'slideshow' ? stopSlideshow() : startSlideshow()));
$('save-grid').addEventListener('click', () => {
  download(`evolving-art-${state.setInfo.id}.json`, { type: 'evolving-art/grid', set: state.setInfo.id, grid: currentGrid().toJSON() });
});
$('load-grid').addEventListener('click', loadGridFile);
$('copy-link').addEventListener('click', async () => {
  const url = shareLink();
  try { await navigator.clipboard.writeText(url); flash('explore-status', 'Link copied.'); }
  catch { flash('explore-status', 'Link is now in the address bar.'); }
});

// Evolve
$('evolve').addEventListener('click', () => (mode === 'evolving' ? pauseEvolution() : startEvolution()));
$('reset-evolve').addEventListener('click', resetEvolution);
$('save-ga').addEventListener('click', () => {
  download(`evolving-art-run-${gaKey}-gen${ga.generation}.json`, { type: 'evolving-art/ga', set: gaKey, ga: ga.toJSON() });
});
$('load-ga').addEventListener('click', loadGaFile);

// Settings form
const setChoice = $('set-choice');
for (const group of [...new Set(PAINTING_SETS.map((s) => s.group))]) {
  const og = document.createElement('optgroup');
  og.label = group;
  for (const s of PAINTING_SETS.filter((x) => x.group === group)) {
    og.append(new Option(`${s.name} — ${s.group.split(' ')[0].toLowerCase()}${s.sounds ? ' · music' : ''}`, s.id));
  }
  setChoice.append(og);
}

const numberFields = {
  'edge-thickness': ['edgeThickness', 1, 50, true],
  'max-generations': ['maxGenerations', 1, 100000, true],
  'population-size': ['populationSize', 4, 1000, true],
  'mutation-rate': ['mutationRate', 0, 1, false],
  'crossover-rate': ['crossoverRate', 0, 1, false],
};

function updateEdgeThicknessLimit() {
  if (!state.set) return;
  const max = Math.max(1, Math.floor(Math.min(state.set.panelWidth, state.set.panelHeight) / 2));
  numberFields['edge-thickness'][2] = max;
  $('edge-thickness').max = String(max);
  if (settings.edgeThickness > max) { settings.edgeThickness = max; $('edge-thickness').value = max; }
}

function syncSettingsForm() {
  setChoice.value = settings.set;
  $('slide-duration').value = String(settings.slideSeconds);
  $('fitness-type').value = String(settings.fitnessType);
  $('pace').value = String(settings.pace);
  $('edge-thickness-field').hidden = settings.fitnessType !== 1;
  for (const [id, [key]] of Object.entries(numberFields)) $(id).value = settings[key];
  updateEvolveUI();
}

for (const [id, field] of Object.entries(numberFields)) {
  $(id).addEventListener('change', (e) => {
    const [key, min, max, integer] = field;
    let v = Number(e.target.value);
    if (!Number.isFinite(v)) v = DEFAULT_SETTINGS[key];
    v = Math.min(max, Math.max(min, integer ? Math.round(v) : v));
    settings[key] = v;
    e.target.value = v;
    persistSettings();
    if (key === 'edgeThickness') onGridChanged();
    if (ga && mode !== 'evolving' && key === 'maxGenerations') { ga.settings.maxGenerations = v; updateEvolveUI(); }
  });
}
$('fitness-type').addEventListener('change', (e) => {
  settings.fitnessType = Number(e.target.value);
  $('edge-thickness-field').hidden = settings.fitnessType !== 1;
  persistSettings();
  onGridChanged();
});
$('slide-duration').addEventListener('change', (e) => {
  settings.slideSeconds = Number(e.target.value);
  persistSettings();
  if (mode === 'slideshow') { clearInterval(slideTimer); slideTimer = setInterval(randomise, settings.slideSeconds * 1000); }
});
$('pace').addEventListener('change', (e) => { settings.pace = e.target.value; persistSettings(); });

async function changeSet(setId) {
  const wasSlideshow = mode === 'slideshow';
  if (mode === 'evolving') pauseEvolution();
  await useSet(setId);
  if (wasSlideshow && started) { clearInterval(slideTimer); slideTimer = setInterval(randomise, settings.slideSeconds * 1000); }
}
setChoice.addEventListener('change', () => changeSet(setChoice.value));

$('defaults').addEventListener('click', () => {
  Object.assign(settings, { slideSeconds: SLIDESHOW_DEFAULT, pace: 'slide', ...DEFAULT_SETTINGS });
  persistSettings();
  updateEdgeThicknessLimit();
  syncSettingsForm();
  onGridChanged();
});

// Header buttons
function setSound(on) {
  sound.setEnabled(on);
  $('sound-toggle').setAttribute('aria-pressed', String(on));
}
$('sound-toggle').addEventListener('click', () => setSound(!sound.enabled));
function toggleFullscreen() {
  if (document.fullscreenElement) document.exitFullscreen?.();
  else document.documentElement.requestFullscreen?.().catch(() => {});
}
$('fullscreen').addEventListener('click', toggleFullscreen);

function setUiHidden(hidden) {
  body.classList.toggle('ui-hidden', hidden);
  $('show-ui').hidden = !hidden;
  if (hidden) noteActivity();
  setTimeout(layout, 420);
}
$('hide-ui').addEventListener('click', () => setUiHidden(true));
$('show-ui').addEventListener('click', () => setUiHidden(false));

let started = false;
let idleTimer = null;
function noteActivity() {
  body.classList.remove('idle');
  clearTimeout(idleTimer);
  idleTimer = setTimeout(() => body.classList.add('idle'), 2500);
}
document.addEventListener('pointermove', noteActivity);

// Keyboard
document.addEventListener('keydown', (e) => {
  if (e.target.closest('input, select, textarea') || e.ctrlKey || e.metaKey || e.altKey) return;
  if (!started) return;
  switch (e.key) {
    case 'Escape':
      if (moveFrom) cancelMove();
      else setUiHidden(false);
      break;
    case 'h': case 'H': setUiHidden(!body.classList.contains('ui-hidden')); break;
    case 'r': case 'R': $('randomise').click(); break;
    case 's': case 'S': $('slideshow').click(); break;
    case 'e': case 'E': $('evolve').click(); break;
    case 'm': case 'M': setSound(!sound.enabled); break;
    case 'f': case 'F': toggleFullscreen(); break;
    default: return;
  }
  e.preventDefault();
});

// ---------------------------------------------------------------------------
// Start-up
// ---------------------------------------------------------------------------

(async function init() {
  const fromHash = readHash();
  if (fromHash.set) settings.set = fromHash.set;
  syncSettingsForm();
  setMode('manual');
  const beginButton = $('begin');
  beginButton.disabled = true;
  beginButton.textContent = 'Loading…';
  if (!(await useSet(settings.set, fromHash.grid))) return;
  beginButton.disabled = false;
  beginButton.textContent = 'Begin';
  beginButton.focus();
  beginButton.addEventListener('click', () => {
    started = true;
    sound.unlock();
    $('intro').classList.add('gone');
    setTimeout(() => $('intro').remove(), 700);
    onGridChanged(); // start any loops for faces already showing
    // Like the original, the piece opens with the random slideshow — unless a shared arrangement was linked.
    if (!fromHash.grid) startSlideshow();
  }, { once: true });
})();
