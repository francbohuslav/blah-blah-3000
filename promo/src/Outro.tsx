import React from "react";
import { AbsoluteFill, Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT } from "./theme";
import { clamp } from "./util";

const CHIPS = ["100% local Whisper", "Hold a key to dictate", "Optional AI cleanup", "REST API", "English & Czech"];

// The closing scene; time 0 is its start.
export const Outro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  return (
    <AbsoluteFill style={{ background: C.page, alignItems: "center", justifyContent: "center", fontFamily: FONT }}>
      <div style={{ display: "flex", alignItems: "center", gap: 28 }}>
        <Img src={staticFile("icon.svg")} style={{ width: 150, height: 150 }} />
        <div style={{ fontSize: 96, fontWeight: 700, color: C.text }}>BlahBlah3000</div>
      </div>
      <div style={{ display: "flex", flexWrap: "wrap", justifyContent: "center", gap: 22, width: 1400, marginTop: 56 }}>
        {CHIPS.map((chip, i) => {
          const p = spring({ frame: Math.round((f - 0.4 - i * 0.25) * fps), fps, config: { damping: 14 } });
          return (
            <div
              key={chip}
              style={{
                fontSize: 40,
                color: C.text,
                padding: "16px 34px",
                borderRadius: 40,
                border: `2px solid ${C.level}`,
                background: "#202124",
                opacity: Math.min(1, p),
                transform: `translateY(${(1 - p) * 30}px)`,
              }}
            >
              {chip}
            </div>
          );
        })}
      </div>
      <div style={{ fontSize: 40, color: C.link, marginTop: 70, opacity: interpolate(f, [1.8, 2.4], [0, 1], clamp) }}>
        github.com/francbohuslav/blah-blah-3000
      </div>
      <div style={{ fontSize: 28, color: C.muted, marginTop: 14, opacity: interpolate(f, [2.0, 2.6], [0, 1], clamp) }}>
        MIT · Windows 10 / 11
      </div>
    </AbsoluteFill>
  );
};
