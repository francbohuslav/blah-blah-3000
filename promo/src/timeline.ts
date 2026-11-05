export const FPS = 30;
export const sec = (s: number) => Math.round(s * FPS);

export interface Segment {
  text: string;
  speakStart: number;
  speakEnd: number;
  shownAt: number;
  whisperTime: string;
}

// The classic scene: two short sentences; the last one contains a phrase that a replacement rule rewrites silently.
export const SEGMENTS: Segment[] = [
  { text: "Hi Anna, the release is on Friday.", speakStart: 11.0, speakEnd: 12.8, shownAt: 13.5, whisperTime: "0.45" },
  { text: "The notes are at ", speakStart: 13.8, speakEnd: 15.4, shownAt: 16.1, whisperTime: "0.38" },
];

export const SPOKEN_REPLACED = "blahblah dot com";
export const REPLACEMENT = "blahblah.com";
export const LAST_TAIL = ".";

export const T = {
  titleEnd: 6.8,
  mailIn: 2.8,
  keysStart: 7.0,
  windowOpen: 9.0,
  recordingStart: 10.0,
  replaceAt: 16.9,
  enterAt: 18.4,
  windowHide: 18.8,
  pasteStart: 19.0,
  pasteEnd: 21.0,
  outroStart: 24.0,
};

export const FULL_TEXT =
  SEGMENTS.map((s) => s.text).join(" ") + REPLACEMENT + LAST_TAIL;
