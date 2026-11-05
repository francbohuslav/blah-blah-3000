import React from "react";
import { C, FONT, ICON_FONT } from "./theme";
import { AppIcon } from "./AppIcon";

const RED = "#e53935";

// A magnified piece of the taskbar: the tray icon turns red while recording. `ring` (1 → 0) draws the countdown of the automatic stop.
export const TrayLens: React.FC<{ f: number; recording: boolean; ring?: number }> = ({ f, recording, ring }) => {
  const pulse = recording ? 0.5 + 0.5 * Math.sin(f * 7) : 0;
  const radius = 58;
  const circumference = 2 * Math.PI * radius;
  return (
    <div
      style={{
        width: 620,
        height: 132,
        borderRadius: 66,
        background: "#1f2023",
        border: "1px solid #4a4d51",
        boxShadow: "0 24px 60px rgba(0,0,0,0.55)",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-around",
        padding: "0 34px",
        fontFamily: FONT,
      }}
    >
      {["", "", ""].map((glyph) => (
        <span key={glyph} style={{ fontFamily: ICON_FONT, fontSize: 38, color: C.muted }}>
          {glyph}
        </span>
      ))}
      <div style={{ position: "relative", width: 112, height: 112, display: "flex", alignItems: "center", justifyContent: "center" }}>
        <div
          style={{
            position: "absolute",
            width: 84,
            height: 84,
            borderRadius: 22,
            boxShadow: recording ? `0 0 ${24 + 22 * pulse}px rgba(229,57,53,${0.45 + 0.3 * pulse})` : "none",
          }}
        />
        <AppIcon size={84} id="tray" background={recording ? RED : "#202124"} stroke={recording ? RED : "#3c4043"} />
        {ring !== undefined && (
          <svg width={128} height={128} style={{ position: "absolute", transform: "rotate(-90deg)" }}>
            <circle cx={64} cy={64} r={radius} fill="none" stroke="#ffffff" strokeWidth={6} strokeLinecap="round"
              strokeDasharray={circumference} strokeDashoffset={circumference * (1 - Math.max(0, Math.min(1, ring)))} />
          </svg>
        )}
      </div>
      <span style={{ fontSize: 30, color: C.text }}>10:42</span>
    </div>
  );
};
