import React from "react";
import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Caption } from "./Caption";
import { Keycap } from "./Keycap";
import { SettingsMock } from "./SettingsMock";
import { C, FONT, ICON_FONT } from "./theme";
import { sceneDuration } from "./schedule";
import { clamp, Fade } from "./util";

const BEFORE =
  "Hi Tom, I wanted to, um, tell you that the market in team meeting moved to Friday at 10, and, uh, can you please tell Sarah about it? Thanks a lot";
const AFTER =
  "Hi Tom, I wanted to tell you that the marketing team meeting has moved to Friday at 10. Can you please tell Sarah about it? Thanks a lot.";

// Times (seconds) of this scene.
const A = { keys: 3.6, correcting: 4.4, done: 6.5, back: 13.5, window: 14.8 };

const IconButton: React.FC<{ glyph: string; color: string; opacity?: number; hover?: boolean }> = ({ glyph, color, opacity = 1, hover }) => (
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
      background: hover ? C.hover : "transparent",
    }}
  >
    {glyph}
  </div>
);

// The dictation window with the text correction button (a copy of the real window, see src/MainWindow.xaml).
const AiWindow: React.FC<{ f: number; level: number }> = ({ f, level }) => {
  const mix = interpolate(f, [A.done, A.done + 0.2, A.back - 0.2, A.back], [0, 1, 1, 0], clamp);
  let ai = "";
  let aiColor = C.level;
  if (f < A.correcting) {
    ai = "Whisper: ✓ 0.52 s";
  } else if (f < A.done) {
    ai = "AI: ⟳ correcting… (Esc = cancel)";
    aiColor = C.busy;
  } else if (f < A.back) {
    ai = "AI: ✓ 1.1 s – check the text, Enter = paste";
  }
  const revertHover = f >= A.back - 0.5 && f < A.back;
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
        <span style={{ fontSize: 14, color: C.recording, marginLeft: 16 }}>● Recording</span>
        <span style={{ fontSize: 14, color: aiColor, marginLeft: 14 }}>{ai}</span>
        <div style={{ flex: 1 }} />
        <span style={{ fontSize: 12, color: C.muted, marginRight: 8 }}>Enter = paste · Shift+Enter = new line · Esc = cancel</span>
        <IconButton glyph={""} color="#bdc1c6" opacity={mix} hover={revertHover} />
        <IconButton glyph={""} color={C.busy} />
        <IconButton glyph={""} color={C.link} opacity={0.3} />
        <IconButton glyph={""} color="#bdc1c6" />
      </div>
      <div style={{ flex: 1, marginTop: 8, background: C.textBoxBg, padding: 6, fontSize: 18, lineHeight: "26px", overflow: "hidden", position: "relative" }}>
        <div style={{ opacity: 1 - mix }}>{BEFORE}</div>
        <div style={{ position: "absolute", left: 6, right: 6, top: 6, opacity: mix }}>{AFTER}</div>
      </div>
      <div style={{ height: 4, background: C.levelTrack, marginTop: 8 }}>
        <div style={{ height: 4, width: `${level * 100}%`, background: C.level }} />
      </div>
    </div>
  );
};

// Correct the dictated text with an AI service (optional), then the settings where the service is chosen.
export const AiScene: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const pop = spring({ frame: Math.round((f - 0.1) * fps), fps, config: { damping: 14, stiffness: 160 } });
  const windowOut = interpolate(f, [A.window - 0.3, A.window + 0.1], [1, 0], clamp);
  const settingsIn = interpolate(f, [A.window + 0.2, A.window + 0.7, 23.0, 23.5], [0, 1, 1, 0], clamp);
  return (
    <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 40%, #202226, ${C.page})` }}>
      <div
        style={{
          position: "absolute",
          left: 440,
          top: 420,
          opacity: Math.min(1, pop) * windowOut,
          transform: `scale(${1.45 * (0.9 + 0.1 * pop)})`,
        }}
      >
        <AiWindow f={f} level={0.03} />
      </div>

      <Fade times={[A.keys - 0.5, A.keys - 0.2, A.correcting + 0.4, A.correcting + 0.8]} style={{ left: 0, right: 0, top: 150 }}>
        <div style={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 28, fontFamily: FONT, fontSize: 56, color: C.muted }}>
          <Keycap label="Ctrl" pressAt={A.keys - 0.1} />+<Keycap label="K" pressAt={A.keys + 0.2} />
        </div>
      </Fade>

      <div style={{ position: "absolute", left: 0, right: 0, top: 100, display: "flex", justifyContent: "center", opacity: settingsIn }}>
        <div style={{ transform: "scale(1.15)", transformOrigin: "top center" }}>
          <SettingsMock tab="llm" f={f - A.window} />
        </div>
      </div>

      <Caption text="Dictated text has the odd slip." from={0.4} to={2.8} />
      <Caption text="Ctrl+K: fix it with AI." from={3.4} to={6.0} />
      <Caption text="Fillers gone. Mistakes fixed. You check the result." from={6.8} to={10.8} />
      <Caption text="Don't like it? One click back." from={12.0} to={14.4} />
      <Caption text="Bring your own key: OpenAI, Groq, Gemini, xAI…" from={15.4} to={19.0} />
      <Caption text="Or a local model. Only the text is sent." from={20.1} to={23.3} />
    </AbsoluteFill>
  );
};

export const AI_DURATION = sceneDuration("ai");
