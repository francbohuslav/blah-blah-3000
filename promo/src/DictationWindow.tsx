import React from "react";
import { interpolate, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT, ICON_FONT } from "./theme";
import { LAST_TAIL, REPLACEMENT, SEGMENTS, SPOKEN_REPLACED, T } from "./timeline";

const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;

const IconButton: React.FC<{ glyph: string; color: string; opacity?: number }> = ({ glyph, color, opacity = 1 }) => (
  <div
    style={{
      width: 34,
      height: 30,
      borderRadius: 6,
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontFamily: ICON_FONT,
      fontSize: 16,
      color,
      opacity,
    }}
  >
    {glyph}
  </div>
);

// Pixel-faithful CSS copy of the dictation window (1040x240).
export const DictationWindow: React.FC<{ speaking: boolean; level: number }> = ({ speaking, level }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;

  let status = "Opening microphone…";
  let statusColor = C.muted;
  if (f >= T.recordingStart) {
    status = "● Recording";
    statusColor = C.recording;
  }
  if (f >= T.enterAt) {
    status = "Processing…";
    statusColor = C.muted;
  }

  // Whisper indicator: transcribing while a sentence is being processed, otherwise the last timing.
  let whisper = "";
  let whisperColor = C.level;
  SEGMENTS.forEach((s) => {
    if (f >= s.speakEnd + 0.3 && f < s.shownAt) {
      whisper = "Whisper: ⟳ transcribing…";
      whisperColor = C.busy;
    } else if (f >= s.shownAt) {
      whisper = `Whisper: ✓ ${s.whisperTime} s`;
      whisperColor = C.level;
    }
  });

  const caretOn = Math.floor(f * 2) % 2 === 0 && f < T.enterAt;
  const replaced = f >= T.replaceAt;
  const replaceFlash = interpolate(f, [T.replaceAt, T.replaceAt + 0.5], [0, 1], clamp);

  return (
    <div
      style={{
        width: 1040,
        height: 240,
        boxSizing: "border-box",
        padding: 14,
        background: C.windowBg,
        border: `1px solid ${C.windowBorder}`,
        fontFamily: FONT,
        color: C.text,
        display: "flex",
        flexDirection: "column",
        boxShadow: "0 30px 80px rgba(0,0,0,0.55)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", height: 34 }}>
        <span style={{ fontSize: 14, fontWeight: 600 }}>BlahBlah3000</span>
        <span style={{ fontSize: 14, color: C.muted, whiteSpace: "pre" }}> 1.1.0</span>
        <span style={{ fontSize: 14, color: statusColor, marginLeft: 16 }}>{status}</span>
        <span style={{ fontSize: 14, color: whisperColor, marginLeft: 14 }}>{whisper}</span>
        <div style={{ flex: 1 }} />
        <span style={{ fontSize: 12, color: C.muted, marginRight: 8 }}>
          Enter = paste · Shift+Enter = new line · Esc = cancel
        </span>
        <IconButton glyph={""} color={C.link} opacity={0.3} />
        <IconButton glyph={""} color="#bdc1c6" />
      </div>
      <div
        style={{
          flex: 1,
          marginTop: 8,
          background: C.textBoxBg,
          padding: 6,
          fontSize: 18,
          lineHeight: "26px",
          overflow: "hidden",
        }}
      >
        {SEGMENTS.map((s, i) => {
          const op = interpolate(f, [s.shownAt, s.shownAt + 0.35], [0, 1], clamp);
          if (op === 0) {
            return null;
          }
          const isLast = i === SEGMENTS.length - 1;
          return (
            <span key={i} style={{ opacity: op }}>
              {s.text}
              {isLast &&
                (replaced ? (
                  <span
                    style={{
                      background: `rgba(107,90,31,${replaceFlash})`,
                      outline: replaceFlash < 1 ? `2px solid rgba(253,214,99,${1 - replaceFlash})` : undefined,
                    }}
                  >
                    {REPLACEMENT}
                  </span>
                ) : (
                  SPOKEN_REPLACED
                ))}
              {isLast && LAST_TAIL}{" "}
            </span>
          );
        })}
        {caretOn && <span style={{ borderLeft: `2px solid ${C.text}`, marginLeft: 1 }} />}
      </div>
      <div style={{ height: 4, background: C.levelTrack, marginTop: 8 }}>
        <div style={{ height: 4, width: `${(speaking ? level : 0.03) * 100}%`, background: C.level }} />
      </div>
    </div>
  );
};
