import React from "react";
import { useCurrentFrame } from "remotion";
import { C } from "./theme";

const BARS = 41;

// Decorative voice waveform; amplitude is 0..1.
export const Waveform: React.FC<{ amplitude: number }> = ({ amplitude }) => {
  const frame = useCurrentFrame();
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 7, height: 90 }}>
      {Array.from({ length: BARS }, (_, i) => {
        const envelope = Math.sin((i / (BARS - 1)) * Math.PI);
        const wobble =
          0.5 + 0.5 * Math.sin(frame * 0.55 + i * 1.7) * Math.cos(frame * 0.23 + i * 0.6);
        const h = 6 + amplitude * envelope * wobble * 84;
        return (
          <div
            key={i}
            style={{ width: 8, height: h, borderRadius: 4, background: C.recording, opacity: 0.55 + 0.45 * envelope }}
          />
        );
      })}
    </div>
  );
};
