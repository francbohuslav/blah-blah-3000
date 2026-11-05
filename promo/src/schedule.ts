// The length of every scene of the tour (seconds), in the order they are played. It has no imports, so the tools can read it too.

/** The classic scene uses a part of the original timeline (see timeline.ts): from this time to that time. */
export const CLASSIC_START = 2.2;
export const CLASSIC_END = 23.6;

const round = (n: number) => Number(n.toFixed(2));

export const SCENES = [
  ["title", 6.8],
  ["classic", round(CLASSIC_END - CLASSIC_START)],
  ["ptt", 21],
  ["latch", 10.8],
  ["ai", 23.8],
  ["montage", 15],
  ["outro", 7],
] as const;

export type SceneId = (typeof SCENES)[number][0];

export const sceneDuration = (id: SceneId): number => SCENES.find(([name]) => name === id)![1];

/** When the scene starts in the video. */
export const sceneStart = (id: SceneId): number => {
  let at = 0;
  for (const [name, duration] of SCENES) {
    if (name === id) return round(at);
    at += duration;
  }
  throw new Error(`Unknown scene ${id}`);
};

export const TOTAL_DURATION = round(SCENES.reduce((sum, [, duration]) => sum + duration, 0));
