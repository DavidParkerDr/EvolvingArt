// The painting sets the piece can show. To add a set, put its 24 images (1a.png … 12b.png) in a folder
// and add an entry here. The shape (rectangular or square) is worked out from the images themselves.
//
// `sounds.faces` lists the panel faces that have their own music loop, as "<panel><a|b>" — each is
// `<folder>/<face>.mp3`. `sounds.background` is a long track played during the slideshow and evolution.

export const PAINTING_SETS = [
  { id: 'r1', group: 'Rectangular panels', name: 'Roberto 1', images: 'assets/images/rectangular/roberto1' },
  { id: 'r2', group: 'Rectangular panels', name: 'Roberto 2', images: 'assets/images/rectangular/roberto2' },
  {
    id: 'r3', group: 'Rectangular panels', name: 'Roberto 3', images: 'assets/images/rectangular/roberto3',
    sounds: {
      folder: 'assets/sounds/rectangular/roberto3',
      background: 'background',
      faces: ['1b', '3b', '6b', '7b', '10b', '11b', '12b'],
    },
  },
  { id: 'r4', group: 'Rectangular panels', name: 'Roberto 4', images: 'assets/images/rectangular/roberto4' },
  {
    id: 's1', group: 'Square panels', name: 'Roberto 1', images: 'assets/images/square/roberto1',
    sounds: {
      folder: 'assets/sounds/square/roberto1',
      background: 'background',
      faces: ['1b', '4b', '5a', '6a', '8b', '10b'],
    },
  },
  { id: 's2', group: 'Square panels', name: 'Roberto 2', images: 'assets/images/square/roberto2' },
  { id: 's3', group: 'Square panels', name: 'Roberto 3', images: 'assets/images/square/roberto3' },
  { id: 's4', group: 'Square panels', name: 'Roberto 4', images: 'assets/images/square/roberto4' },
];

export const DEFAULT_SET = 'r3';

export const findSet = (id) => PAINTING_SETS.find((s) => s.id === id) || null;

/** Accept older set identifiers: plain numbers (1–4) meant the rectangular sets. */
export function normaliseSetId(value, { square = false } = {}) {
  if (typeof value === 'string' && findSet(value)) return value;
  const n = parseInt(value, 10);
  if (n >= 1 && n <= 4) return `${square ? 's' : 'r'}${n}`;
  return null;
}
