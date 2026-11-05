import React from "react";
import { Sequence } from "remotion";
import { AI_DURATION, AiScene } from "./AiScene";
import { LATCH_DURATION, LatchScene, PTT_DURATION, PttScene } from "./ChatScene";
import { Classic, CLASSIC_END, CLASSIC_START } from "./Classic";
import { Montage, MONTAGE_DURATION } from "./Montage";
import { Outro } from "./Outro";
import { sec, T } from "./timeline";
import { Title } from "./Title";
import { SceneFade } from "./util";

interface Part {
  /** Length on the screen, seconds. */
  duration: number;
  /** Where on the scene's own timeline it starts; a scene can be shown in a shortened form. */
  start?: number;
  node: React.ReactNode;
  /** The scene fades in and out (the title and the outro manage their own fades). */
  fade?: [number, number];
}

// Plays the parts one after another.
const Timeline: React.FC<{ parts: Part[] }> = ({ parts }) => {
  let at = 0;
  return (
    <>
      {parts.map((part, i) => {
        const from = sec(at);
        at += part.duration;
        const frames = sec(at) - from;
        const inner = <Sequence from={-sec(part.start ?? 0)}>{part.node}</Sequence>;
        return (
          <Sequence key={i} from={from} durationInFrames={frames}>
            {part.fade ? <SceneFade inS={part.fade[0]} outS={part.fade[1]}>{inner}</SceneFade> : inner}
          </Sequence>
        );
      })}
    </>
  );
};

const OUTRO_DURATION = 7;
const FADE: [number, number] = [0.4, 0.4];
const title: Part = { duration: T.titleEnd, node: <Title /> };
const classic: Part = { duration: CLASSIC_END - CLASSIC_START, start: CLASSIC_START, node: <Classic />, fade: FADE };
const outro = (duration: number): Part => ({ duration, node: <Outro />, fade: [0.5, 0.7] });

// The tour: every feature with its own scene.
const LONG: Part[] = [
  title,
  classic,
  { duration: PTT_DURATION, node: <PttScene />, fade: FADE },
  { duration: LATCH_DURATION, node: <LatchScene />, fade: FADE },
  { duration: AI_DURATION, node: <AiScene />, fade: FADE },
  { duration: MONTAGE_DURATION, node: <Montage />, fade: FADE },
  outro(OUTRO_DURATION),
];

// The short version: the classic dictation plus a taste of push to talk and of the AI correction.
const PTT_TASTE: [number, number] = [3.6, 11.4];
const AI_TASTE: [number, number] = [1.2, 7.4];
const SHORT: Part[] = [
  title,
  classic,
  { duration: PTT_TASTE[1] - PTT_TASTE[0], start: PTT_TASTE[0], node: <PttScene />, fade: FADE },
  { duration: AI_TASTE[1] - AI_TASTE[0], start: AI_TASTE[0], node: <AiScene />, fade: FADE },
  outro(5),
];

const total = (parts: Part[]) => parts.reduce((sum, p) => sum + p.duration, 0);

export const LONG_DURATION = total(LONG);
export const SHORT_DURATION = total(SHORT);

export const PromoLong: React.FC = () => <Timeline parts={LONG} />;
export const PromoShort: React.FC = () => <Timeline parts={SHORT} />;
