export const FPS = 30;
export const DURATION_S = 32.5;
export const sec = (s: number) => Math.round(s * FPS);

export interface Segment {
  text: string;
  speakStart: number;
  speakEnd: number;
  shownAt: number;
  whisperTime: string;
}

// The last segment contains a phrase that a replacement rule rewrites later.
export const SEGMENTS: Segment[] = [
  { text: "Hi Anna, thanks for the quick review.", speakStart: 11.0, speakEnd: 13.0, shownAt: 13.8, whisperTime: "0.45" },
  { text: "I have merged the changes and the release is scheduled for Friday.", speakStart: 14.0, speakEnd: 16.6, shownAt: 17.4, whisperTime: "0.61" },
  { text: "The notes are at ", speakStart: 17.8, speakEnd: 19.4, shownAt: 20.1, whisperTime: "0.38" },
];

export const SPOKEN_REPLACED = "blahblah dot com";
export const REPLACEMENT = "blahblah.com";
export const LAST_TAIL = ".";

export const T = {
  titleEnd: 3.2,
  mailIn: 2.8,
  keysStart: 7.0,
  windowOpen: 9.0,
  recordingStart: 10.0,
  replaceAt: 21.0,
  enterAt: 22.3,
  windowHide: 22.7,
  pasteStart: 22.9,
  pasteEnd: 24.9,
  outroStart: 27.6,
};

export const FULL_TEXT =
  SEGMENTS.map((s) => s.text).join(" ") + REPLACEMENT + LAST_TAIL;
