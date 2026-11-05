import React from "react";
import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT } from "./theme";

// A keyboard key that is pressed at `pressAt` seconds and, when `releaseAt` is given, let go again.
export const Keycap: React.FC<{ label: string; pressAt: number; releaseAt?: number; wide?: boolean }> = ({ label, pressAt, releaseAt, wide }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const t = frame - pressAt * fps;
  const press = spring({ frame: t, fps, config: { damping: 14, stiffness: 260 } });
  let down = t >= 0 ? press : 0;
  if (releaseAt !== undefined) {
    const r = frame - releaseAt * fps;
    if (r >= 0) {
      down = Math.max(0, Math.min(1, 1 - spring({ frame: r, fps, config: { damping: 14, stiffness: 260 } })));
    }
  }
  const glow = interpolate(down, [0, 1], [0, 1]);
  return (
    <div
      style={{
        minWidth: wide ? 220 : 150,
        height: 120,
        padding: "0 28px",
        borderRadius: 22,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontFamily: FONT,
        fontSize: 48,
        fontWeight: 600,
        color: C.text,
        background: `linear-gradient(#3c4043, #2b2c2f)`,
        border: `2px solid ${glow > 0.5 ? C.recording : "#5f6368"}`,
        boxShadow: `0 ${10 - down * 7}px 0 #0d0e0f, 0 0 ${40 * glow}px rgba(242,139,130,${0.5 * glow})`,
        transform: `translateY(${down * 7}px)`,
      }}
    >
      {label}
    </div>
  );
};
