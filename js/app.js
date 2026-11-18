// Evolving Art — browser front end.
// Replaces MainPage, PaintingGrid and Painting (XAML + code-behind) from the Silverlight version.

import {
  ROWS, COLUMNS, NUMBER_OF_PAINTINGS, DEFAULT_SETTINGS,
  PaintingEncoding, GridEncoding, PaintingSet, GeneticAlgorithm, fitness,
} from './ga.js';

// Assets stay where they were in the original Visual Studio project.
const ASSET_ROOT = 'TurnerTest/Turner1/';
const imageUrl = (set, n, face) => `${ASSET_ROOT}Images/Roberto${set}/${n}${face}.png`;
const soundUrl = (set, name) => `${ASSET_ROOT}Sounds/Roberto${set}/${name}.mp3`;

// Only Roberto 3 has music: a long background track plus loops for some panel backs.
const SOUND_MANIFEST = {
  3: { background: 'background', faces: new Set(['1b', '3b', '6b', '7b', '10b', '11b', '12b']) },
};

const SLIDESHOW_DEFAULT = 6;
const STORAGE_KEY = 'evolvingArt.settings.v1';

const $ = (id) => document.getElementById(id);
const body = document.body;

// ---------------------------------------------------------------------------
// Settings (persisted per viewer where storage is available)
// ---------------------------------------------------------------------------

const settings = {
  set: 3,
  slideSeconds: SLIDESHOW_DEFAULT,
  pace: 'slide',
  ...DEFAULT_SETTINGS,
};
try { Object.assign(settings, JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}')); } catch { /* storage unavailable */ }
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
    this.voices = new Map();        // paintingIndex -> { key, source, gain }
    this.manifest = null;
    this.set = null;
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
      this.master.connect(this.ctx.destination);
      this.epoch = this.ctx.currentTime;
    }
    if (this.ctx.state === 'suspended') this.ctx.resume();
  }

  useSet(set) {
    this.stopAllVoices();
    this.set = set;
    this.manifest = SOUND_MANIFEST[set] || null;
    this.background.pause();
    if (this.manifest?.background) {
      this.background.src = soundUrl(set, this.manifest.background);
    } else {
      this.background.removeAttribute('src');
    }
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
    const play = this.enabled && this.wantBackground && !!this.manifest?.background && this.ctx;
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

  /** Each visible face that has a loop plays it; loops are phase-aligned so they stay in step. */
  update(panels) {
    if (!this.ctx) return;
    for (const p of panels) {
      const key = `${p.index + 1}${p.front ? 'a' : 'b'}`;
      const wanted = this.manifest?.faces.has(key) ? key : null;
      const current = this.voices.get(p.index);
      if ((current?.key ?? null) === wanted) continue;
      if (current) this.#stopVoice(p.index);
      if (wanted) this.#startVoice(p.index, wanted);
    }
  }

  async #startVoice(index, key) {
    const set = this.set;
    const voice = { key, source: null, gain: null };
    this.voices.set(index, voice);
    const buffer = await this.#buffer(soundUrl(set, key));
    if (!buffer || this.voices.get(index) !== voice || this.set !== set) return;
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

  #stopVoice(index) {
    const voice = this.voices.get(index);
    this.voices.delete(index);
    if (!voice?.source) return;
    const now = this.ctx.currentTime;
    voice.gain.gain.cancelScheduledValues(now);
    voice.gain.gain.setValueAtTime(voice.gain.gain.value, now);
    voice.gain.gain.linearRampToValueAtTime(0, now + 0.4);
    voice.source.stop(now + 0.45);
  }

  stopAllVoices() { for (const i of [...this.voices.keys()]) this.#stopVoice(i); }
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

class Panel {
  constructor(index) {
    this.index = index;
    // Cumulative rotation angles (degrees) of the card: rotateZ(z) rotateX(x) rotateY(y).
    // Turning over sideways adds 180 to y, top-to-bottom adds 180 to x, upside down adds 180 to z.
    this.angles = { x: 0, y: 0, z: 0 };
    this.slot = index;

    this.el = document.createElement('div');
    this.el.className = 'piece';
    this.el.setAttribute('role', 'gridcell');
    this.card = document.createElement('div');
    this.card.className = 'card';
    this.frontImg = Object.assign(document.createElement('img'), { className: 'face front', alt: '', draggable: false });
    this.backImg = Object.assign(document.createElement('img'), { className: 'face back', alt: '', draggable: false });
    this.card.append(this.frontImg, this.backImg);

    const controls = document.createElement('div');
    controls.className = 'controls';
    const button = (cls, label, icon, action) => {
      const b = document.createElement('button');
      b.className = cls;
      b.type = 'button';
      b.title = label;
      b.setAttribute('aria-label', label);
      b.innerHTML = icon;
      b.addEventListener('click', (e) => { e.stopPropagation(); action(); });
      return b;
    };
    controls.append(
      button('flip-h', 'Turn over sideways, to show the other side', ICONS.flipH, () => this.userAction(() => this.flipHorizontal())),
      button('spin', 'Turn upside down', ICONS.spin, () => this.userAction(() => this.spin())),
      button('flip-v', 'Turn over top to bottom, to show the other side', ICONS.flipV, () => this.userAction(() => this.flipVertical())),
      button('move', 'Move to another slot', ICONS.move, () => toggleMove(this)),
    );
    this.el.append(this.card, controls);

    this.card.addEventListener('transitionend', (e) => {
      if (e.target === this.card && e.propertyName === 'transform') this.normalise();
    });
    this.el.addEventListener('click', () => onPanelClick(this));
    this.setSlot(index);
    this.render(false);
  }

  get front() { return ((this.angles.y + this.angles.x) / 180) % 2 === 0; }
  get rotated() { return ((this.angles.z + this.angles.x) / 180) % 2 === 1; }
  get encoding() { return new PaintingEncoding(this.index, this.rotated, this.front); }

  setImages(set) {
    this.frontImg.src = imageUrl(set, this.index + 1, 'a');
    this.backImg.src = imageUrl(set, this.index + 1, 'b');
    this.updateLabel();
  }

  updateLabel() {
    this.el.setAttribute('aria-label',
      `Panel ${this.index + 1}, ${this.front ? 'front' : 'back'}${this.rotated ? ', upside down' : ''}`);
  }

  setSlot(slot) {
    this.slot = slot;
    this.el.style.setProperty('--col', slot % COLUMNS);
    this.el.style.setProperty('--row', Math.floor(slot / COLUMNS));
  }

  render(animate) {
    this.card.classList.toggle('snap', !animate);
    const { x, y, z } = this.angles;
    this.card.style.transform = `rotateZ(${z}deg) rotateX(${x}deg) rotateY(${y}deg)`;
    if (!animate) {
      void this.card.offsetWidth; // apply without transition
      this.card.classList.remove('snap');
    }
    this.updateLabel();
  }

  /** After an animation, fold the angles back into an equivalent small form (no visible change). */
  normalise() {
    const { x, y, z } = this.angles;
    if (x === 0 && y < 360 && z < 360) return;
    this.angles = { x: 0, y: (y + x) % 360, z: (z + x) % 360 };
    this.render(false);
  }

  flipHorizontal() { this.angles.y += 180; this.render(true); }
  flipVertical() { this.angles.x += 180; this.render(true); }
  spin() { this.angles.z += 180; this.render(true); }

  /** Show the given face/orientation, animating the shortest way there. */
  setState(front, rotated, animate) {
    if (!animate) {
      this.angles = { x: 0, y: front ? 0 : 180, z: rotated ? 180 : 0 };
    } else {
      if (front !== this.front) this.angles.y += 180;
      if (rotated !== this.rotated) this.angles.z += 180;
    }
    this.render(animate);
  }

  userAction(fn) {
    fn();
    onGridChanged();
  }
}

// ---------------------------------------------------------------------------
// App state
// ---------------------------------------------------------------------------

const gridEl = $('grid');
const panels = Array.from({ length: NUMBER_OF_PAINTINGS }, (_, i) => new Panel(i));
gridEl.append(...panels.map((p) => p.el));

let paintingSet = null;       // PaintingSet (pixel data) for the current set
let loadedSet = null;
let mode = 'manual';          // 'manual' | 'slideshow' | 'evolving'
let moveFrom = null;
let slideTimer = null;
let ga = null;
let gaSet = null;
let gaTimer = null;
let lastBestToken = null;
let startFitness = null;

function currentGrid() {
  const slots = new Array(NUMBER_OF_PAINTINGS);
  for (const p of panels) slots[p.slot] = p.encoding;
  return new GridEncoding(slots);
}

function configure(grid, animate) {
  grid.encodings.forEach((enc, slot) => {
    const p = panels[enc.paintingIndex];
    p.setSlot(slot);
    p.setState(enc.frontVisible, enc.rotated, animate);
  });
  onGridChanged();
}

function onGridChanged() {
  sound.update(panels);
  if (paintingSet) {
    const f = fitness(currentGrid(), paintingSet, { type: settings.fitnessType, edgeThickness: settings.edgeThickness });
    $('current-fitness').textContent = f.toFixed(2);
  }
}

function randomise() { configure(GridEncoding.random(), true); }

function setMode(next) {
  mode = next;
  body.classList.toggle('manual', mode === 'manual');
  sound.setBackground(mode !== 'manual');
  if (mode !== 'manual') cancelMove();
  for (const p of panels) p.el.classList.remove('active');
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

let aspect = 175 / 224;
let setLoadToken = 0;

async function loadSet(set) {
  const token = ++setLoadToken;
  $('loading').hidden = false;
  gridEl.style.visibility = 'hidden';
  try {
    const faces = await Promise.all(panels.map(async (p) => {
      const [front, back] = await Promise.all([
        loadImage(imageUrl(set, p.index + 1, 'a')),
        loadImage(imageUrl(set, p.index + 1, 'b')),
      ]);
      return { front, back };
    }));
    if (token !== setLoadToken) return false;
    paintingSet = new PaintingSet(faces.map((f) => ({ front: pixels(f.front), back: pixels(f.back) })));
    aspect = faces[0].front.naturalWidth / faces[0].front.naturalHeight;
    loadedSet = set;
    panels.forEach((p) => p.setImages(set));
    sound.useSet(set);
    layout();
    gridEl.style.visibility = '';
    $('loading').hidden = true;
    if (ga && gaSet !== set) resetEvolution();
    onGridChanged();
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
  const cw = Math.floor(Math.min(w / COLUMNS, (h / ROWS) * aspect));
  const ch = Math.floor(cw / aspect);
  document.documentElement.style.setProperty('--cw', `${cw}px`);
  document.documentElement.style.setProperty('--ch', `${ch}px`);
}
window.addEventListener('resize', layout);
$('stage').addEventListener('transitionend', layout);

// ---------------------------------------------------------------------------
// Manual interaction
// ---------------------------------------------------------------------------

function onPanelClick(panel) {
  if (mode === 'evolving') {
    flash('evolve-status', 'Pause evolution to rearrange panels by hand.');
    return;
  }
  if (mode === 'slideshow') stopSlideshow();
  if (moveFrom) {
    if (panel !== moveFrom) swap(moveFrom, panel);
    cancelMove();
    return;
  }
  // Tap support for touch screens (hover devices see the controls anyway).
  const wasActive = panel.el.classList.contains('active');
  for (const p of panels) p.el.classList.remove('active');
  if (!wasActive) panel.el.classList.add('active');
}

function toggleMove(panel) {
  if (moveFrom === panel) { cancelMove(); return; }
  cancelMove();
  moveFrom = panel;
  panel.el.classList.add('move-from');
  body.classList.add('moving');
}

function cancelMove() {
  moveFrom?.el.classList.remove('move-from');
  moveFrom = null;
  body.classList.remove('moving');
  for (const p of panels) p.el.classList.remove('active');
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
  if (!paintingSet) return;
  if (mode === 'slideshow') stopSlideshow();
  if (!ga) {
    ga = new GeneticAlgorithm(paintingSet, gaSettings());
    gaSet = loadedSet;
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
  gaSet = null;
  lastBestToken = null;
  startFitness = null;
  updateEvolveUI();
}

function updateEvolveUI() {
  const running = mode === 'evolving';
  const btn = $('evolve');
  btn.textContent = running ? 'Pause evolution' : (ga && ga.generation > 0 ? 'Continue evolution' : 'Start evolution');
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
// Save / load (JSON, plus XML files saved by the Silverlight version)
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
const xmlText = (el, name) => el.querySelector(`:scope > ${name}`)?.textContent ?? null;

function gridFromXml(el) {
  return new GridEncoding([...el.querySelectorAll(':scope > PaintingEncoding')].map((pe) => new PaintingEncoding(
    parseInt(xmlText(pe, 'PaintingIndex'), 10),
    xmlBool(xmlText(pe, 'Rotated')),
    xmlBool(xmlText(pe, 'FrontVisible')),
  )));
}

/** Returns { kind: 'grid'|'ga', set, grid?, ga? } from a JSON or legacy XML file. */
async function parseSaveFile(file) {
  const text = await file.text();
  if (text.trimStart().startsWith('<')) {
    const doc = new DOMParser().parseFromString(text, 'application/xml');
    if (doc.querySelector('parsererror')) throw new Error('Not a valid XML file');
    const root = doc.documentElement;
    const set = parseInt(xmlText(root, 'PaintingSelection'), 10) || settings.set;
    if (root.nodeName === 'PaintingGridEncoding') return { kind: 'grid', set, grid: gridFromXml(root) };
    if (root.nodeName === 'GeneticAlgorithm') {
      const num = (n, d) => { const v = Number(xmlText(root, n)); return Number.isFinite(v) ? v : d; };
      return {
        kind: 'ga',
        set,
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
          population: [...root.querySelectorAll(':scope > Population > Individual')].map((ind) => ({
            grid: gridFromXml(ind.querySelector(':scope > PaintingGridEncoding')).toJSON(),
          })),
        },
      };
    }
    throw new Error(`Unrecognised file (${root.nodeName})`);
  }
  const obj = JSON.parse(text);
  if (obj.type === 'evolving-art/grid') return { kind: 'grid', set: obj.set, grid: GridEncoding.fromJSON(obj.grid) };
  if (obj.type === 'evolving-art/ga') return { kind: 'ga', set: obj.set, ga: obj.ga };
  throw new Error('Unrecognised file');
}

function validGrid(grid) {
  const seen = new Set(grid.encodings.map((e) => e.paintingIndex));
  return grid.encodings.length === NUMBER_OF_PAINTINGS && seen.size === NUMBER_OF_PAINTINGS
    && [...seen].every((i) => i >= 0 && i < NUMBER_OF_PAINTINGS);
}

async function switchSetIfNeeded(set) {
  if (set === loadedSet || !(set >= 1 && set <= 4)) return;
  settings.set = set;
  $('set-choice').value = String(set);
  persistSettings();
  await loadSet(set);
}

async function loadGridFile() {
  const file = await pickFile();
  if (!file) return;
  try {
    const result = await parseSaveFile(file);
    if (result.kind !== 'grid') throw new Error('That file is an evolution run — use “Load run…” on the Evolve tab.');
    if (!validGrid(result.grid)) throw new Error('The arrangement in that file is incomplete.');
    stopSlideshow();
    await switchSetIfNeeded(result.set);
    configure(result.grid, true);
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
    stopSlideshow();
    pauseEvolution();
    await switchSetIfNeeded(result.set);
    const loaded = GeneticAlgorithm.fromJSON(paintingSet, result.ga); // re-scored with the current code
    if (loaded.population.length < 2 || !loaded.population.every((i) => validGrid(i.encoding))) {
      throw new Error('The population in that file is incomplete.');
    }
    ga = loaded;
    gaSet = loadedSet;
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
  const hash = `#set=${loadedSet}&grid=${currentGrid().toToken()}`;
  history.replaceState(null, '', hash);
  return location.href;
}

function readHash() {
  const params = new URLSearchParams(location.hash.slice(1));
  const set = parseInt(params.get('set'), 10);
  let grid = null;
  try { if (params.get('grid')) grid = GridEncoding.fromToken(params.get('grid')); } catch { grid = null; }
  return { set: set >= 1 && set <= 4 ? set : null, grid };
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
  download(`evolving-art-roberto${loadedSet}.json`, { type: 'evolving-art/grid', set: loadedSet, grid: currentGrid().toJSON() });
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
  download(`evolving-art-run-roberto${gaSet}-gen${ga.generation}.json`, { type: 'evolving-art/ga', set: gaSet, ga: ga.toJSON() });
});
$('load-ga').addEventListener('click', loadGaFile);

// Settings form
const numberFields = {
  'edge-thickness': ['edgeThickness', 1, 50, true],
  'max-generations': ['maxGenerations', 1, 100000, true],
  'population-size': ['populationSize', 4, 1000, true],
  'mutation-rate': ['mutationRate', 0, 1, false],
  'crossover-rate': ['crossoverRate', 0, 1, false],
};

function syncSettingsForm() {
  $('set-choice').value = String(settings.set);
  $('slide-duration').value = String(settings.slideSeconds);
  $('fitness-type').value = String(settings.fitnessType);
  $('pace').value = String(settings.pace);
  $('edge-thickness-field').hidden = settings.fitnessType !== 1;
  for (const [id, [key]] of Object.entries(numberFields)) $(id).value = settings[key];
  updateEvolveUI();
}

for (const [id, [key, min, max, integer]] of Object.entries(numberFields)) {
  $(id).addEventListener('change', (e) => {
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
$('set-choice').addEventListener('change', async (e) => {
  settings.set = Number(e.target.value);
  persistSettings();
  await loadSet(settings.set);
});
$('defaults').addEventListener('click', () => {
  Object.assign(settings, { slideSeconds: SLIDESHOW_DEFAULT, pace: 'slide', ...DEFAULT_SETTINGS });
  persistSettings();
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
  layout();
  if (!(await loadSet(settings.set))) return;
  configure(fromHash.grid || GridEncoding.random(), false);
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
