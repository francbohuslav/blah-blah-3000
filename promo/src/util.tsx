import React from "react";
import { AbsoluteFill, interpolate, useCurrentFrame, useVideoConfig } from "remotion";

export const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;

// Fades its children in and out at the given times (seconds): [in start, in end, out start, out end].
export const Fade: React.FC<{ times: [number, number, number, number]; children: React.ReactNode; style?: React.CSSProperties }> = ({
  times,
  children,
  style,
}) => {
  const f = useCurrentFrame() / useVideoConfig().fps;
  const opacity = interpolate(f, times, [0, 1, 1, 0], clamp);
  return <div style={{ position: "absolute", opacity, ...style }}>{children}</div>;
};

// Fades a whole scene in at its start and out at its end (the length of the surrounding sequence).
export const SceneFade: React.FC<{ children: React.ReactNode; inS?: number; outS?: number }> = ({ children, inS = 0.4, outS = 0.4 }) => {
  const frame = useCurrentFrame();
  const { durationInFrames, fps } = useVideoConfig();
  const opacity = interpolate(frame, [0, inS * fps, durationInFrames - outS * fps, durationInFrames], [0, 1, 1, 0], clamp);
  return <AbsoluteFill style={{ opacity }}>{children}</AbsoluteFill>;
};

export interface Seg {
  text: string;
  speak: [number, number];
  shown: number;
}

// The text typed into a field so far: each sentence appears quickly (as the real application types it) after it was transcribed.
export const typedText = (f: number, segs: Seg[], typingTime = 0.3): string => {
  let out = "";
  segs.forEach((s, i) => {
    const chars = Math.floor(interpolate(f, [s.shown, s.shown + typingTime], [0, s.text.length], clamp));
    if (chars > 0) out += (i > 0 ? " " : "") + s.text.slice(0, chars);
  });
  return out;
};

// A pulsing level that looks like speech.
export const speechLevel = (frame: number): number => 0.4 + 0.5 * Math.abs(Math.sin(frame * 0.7) * Math.cos(frame * 0.31));
