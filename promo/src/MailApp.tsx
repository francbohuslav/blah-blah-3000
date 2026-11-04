import React from "react";
import { interpolate, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT, ICON_FONT } from "./theme";
import { FULL_TEXT, T } from "./timeline";

const Field: React.FC<{ label: string; value: string }> = ({ label, value }) => (
  <div
    style={{
      display: "flex",
      padding: "18px 32px",
      borderBottom: "1px solid #3c4043",
      fontSize: 24,
      color: C.text,
    }}
  >
    <span style={{ width: 120, color: C.muted }}>{label}</span>
    <span>{value}</span>
  </div>
);

// Generic dark mail composer; the dictated text is typed into its body field.
export const MailApp: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const chars = Math.floor(interpolate(f, [T.pasteStart, T.pasteEnd], [0, FULL_TEXT.length], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  }));
  const typed = FULL_TEXT.slice(0, chars);
  const caretOn = Math.floor(f * 2) % 2 === 0 || (f > T.pasteStart && f < T.pasteEnd);
  const focused = f >= 3.4;

  return (
    <div
      style={{
        width: 1200,
        height: 790,
        background: "#26282b",
        borderRadius: 10,
        border: "1px solid #3c4043",
        boxShadow: "0 40px 100px rgba(0,0,0,0.6)",
        fontFamily: FONT,
        overflow: "hidden",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <div style={{ height: 56, background: "#1c1d1f", display: "flex", alignItems: "center", paddingLeft: 20 }}>
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke={C.link} strokeWidth="1.8">
          <rect x="3" y="5" width="18" height="14" rx="2" />
          <path d="M3 7l9 6 9-6" />
        </svg>
        <span style={{ marginLeft: 14, color: C.muted, fontSize: 20 }}>New message</span>
        <div style={{ flex: 1 }} />
        {["", "", ""].map((glyph) => (
          <div
            key={glyph}
            style={{ width: 72, height: 56, display: "flex", alignItems: "center", justifyContent: "center", fontFamily: ICON_FONT, fontSize: 16, color: C.text }}
          >
            {glyph}
          </div>
        ))}
      </div>
      <Field label="To" value="Anna Novak <anna@example.com>" />
      <Field label="Subject" value="Release on Friday" />
      <div
        style={{
          flex: 1,
          margin: 28,
          padding: 24,
          borderRadius: 12,
          background: "#202124",
          border: `2px solid ${focused ? C.link : "#3c4043"}`,
          fontSize: 28,
          lineHeight: "42px",
          color: C.text,
        }}
      >
        {typed.length === 0 && <span style={{ color: "#6f7378" }}>Write your message…</span>}
        {typed}
        {caretOn && focused && <span style={{ borderLeft: `3px solid ${C.text}`, marginLeft: 2 }}>&#8203;</span>}
      </div>
    </div>
  );
};
