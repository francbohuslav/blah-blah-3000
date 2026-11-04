import React from "react";
import { interpolate, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT } from "./theme";

// Bottom caption that fades in at `from` and out at `to` (seconds).
export const Caption: React.FC<{ text: string; from: number; to: number }> = ({ text, from, to }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const opacity = interpolate(f, [from, from + 0.4, to - 0.4, to], [0, 1, 1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const y = interpolate(f, [from, from + 0.4], [18, 0], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  return (
    <div
      style={{
        position: "absolute",
        left: 0,
        right: 0,
        bottom: 70,
        textAlign: "center",
        fontFamily: FONT,
        fontSize: 54,
        fontWeight: 600,
        color: C.text,
        opacity,
        transform: `translateY(${y}px)`,
        textShadow: "0 4px 24px rgba(0,0,0,0.8)",
      }}
    >
      {text}
    </div>
  );
};
