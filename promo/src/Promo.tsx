import React from "react";
import { AbsoluteFill, Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { Caption } from "./Caption";
import { DictationWindow } from "./DictationWindow";
import { Keycap } from "./Keycap";
import { MailApp } from "./MailApp";
import { C, FONT } from "./theme";
import { SEGMENTS, T } from "./timeline";
import { Waveform } from "./Waveform";

const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;

const Fade: React.FC<{ times: [number, number, number, number]; children: React.ReactNode; style?: React.CSSProperties }> = ({
  times,
  children,
  style,
}) => {
  const f = useCurrentFrame() / useVideoConfig().fps;
  const opacity = interpolate(f, times, [0, 1, 1, 0], clamp);
  return <div style={{ position: "absolute", opacity, ...style }}>{children}</div>;
};

const Title: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const logo = spring({ frame, fps, config: { damping: 12 } });
  const text = interpolate(f, [0.5, 1.2], [0, 1], clamp);
  const tag = interpolate(f, [1.1, 1.8], [0, 1], clamp);
  const out = interpolate(f, [2.7, T.titleEnd], [1, 0], clamp);
  return (
    <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", opacity: out, background: C.page }}>
      <Img src={staticFile("icon.svg")} style={{ width: 260, height: 260, transform: `scale(${logo})` }} />
      <div style={{ fontFamily: FONT, fontSize: 120, fontWeight: 700, color: C.text, opacity: text, marginTop: 24 }}>
        BlahBlah3000
      </div>
      <div style={{ fontFamily: FONT, fontSize: 40, color: C.muted, opacity: tag, marginTop: 10 }}>
        It listens to your blah blah and writes it down.
      </div>
    </AbsoluteFill>
  );
};

const CHIPS = ["100% local Whisper", "No cloud, no account", "GPU accelerated (Vulkan)", "English & Czech"];

const Outro: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const bg = interpolate(f, [T.outroStart, T.outroStart + 0.5], [0, 1], clamp);
  const local = f - T.outroStart;
  return (
    <AbsoluteFill style={{ opacity: bg, background: C.page, alignItems: "center", justifyContent: "center", fontFamily: FONT }}>
      <div style={{ display: "flex", alignItems: "center", gap: 28 }}>
        <Img src={staticFile("icon.svg")} style={{ width: 150, height: 150 }} />
        <div style={{ fontSize: 96, fontWeight: 700, color: C.text }}>BlahBlah3000</div>
      </div>
      <div style={{ display: "flex", flexWrap: "wrap", justifyContent: "center", gap: 22, width: 1500, marginTop: 56 }}>
        {CHIPS.map((chip, i) => {
          const p = spring({ frame: Math.round((local - 0.4 - i * 0.25) * fps), fps, config: { damping: 14 } });
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
      <div style={{ fontSize: 40, color: C.link, marginTop: 70, opacity: interpolate(local, [1.8, 2.4], [0, 1], clamp) }}>
        github.com/francbohuslav/blah-blah-3000
      </div>
      <div style={{ fontSize: 28, color: C.muted, marginTop: 14, opacity: interpolate(local, [2.0, 2.6], [0, 1], clamp) }}>
        MIT · Windows 10 / 11
      </div>
    </AbsoluteFill>
  );
};

export const Promo: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;

  const speaking = SEGMENTS.some((s) => f >= s.speakStart && f < s.speakEnd);
  const level = 0.4 + 0.5 * Math.abs(Math.sin(frame * 0.7) * Math.cos(frame * 0.31));

  const pop = spring({ frame: Math.round((f - T.windowOpen) * fps), fps, config: { damping: 14, stiffness: 160 } });
  const winOpacity = f < T.windowOpen ? 0 : Math.min(1, pop) * interpolate(f, [T.windowHide, T.windowHide + 0.3], [1, 0], clamp);
  const winScale = 1.45 * (0.9 + 0.1 * pop);

  return (
    <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 40%, #202226, ${C.page})` }}>
      <Fade times={[T.mailIn, T.mailIn + 0.6, T.outroStart, T.outroStart + 0.5]} style={{ left: 360, top: 110 }}>
        <MailApp />
      </Fade>

      <Fade times={[T.keysStart - 0.2, T.keysStart + 0.3, T.enterAt - 0.5, T.enterAt - 0.1]} style={{ inset: 0, background: "rgba(19,20,22,0.9)" }}>
        <div />
      </Fade>

      <Fade times={[T.keysStart, T.keysStart + 0.3, T.windowOpen + 0.2, T.windowOpen + 0.5]} style={{ left: 0, right: 0, top: 150 }}>
        <div style={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 28, fontFamily: FONT, fontSize: 56, color: C.muted }}>
          <Keycap label="⊞ Win" pressAt={T.keysStart + 0.3} />+<Keycap label="Shift" pressAt={T.keysStart + 0.7} />+
          <Keycap label="D" pressAt={T.keysStart + 1.1} />
        </div>
      </Fade>

      <Fade times={[T.recordingStart + 0.4, T.recordingStart + 0.8, 19.4, 19.9]} style={{ left: 0, right: 0, top: 230, display: "flex", justifyContent: "center" }}>
        <Waveform amplitude={speaking ? level : 0.05} />
      </Fade>

      <div
        style={{
          position: "absolute",
          left: 440,
          top: 420,
          opacity: winOpacity,
          transform: `scale(${winScale})`,
        }}
      >
        <DictationWindow speaking={speaking} level={level} />
      </div>

      <Fade times={[T.enterAt - 0.4, T.enterAt - 0.2, T.windowHide + 0.1, T.windowHide + 0.4]} style={{ left: 0, right: 0, top: 130, display: "flex", justifyContent: "center" }}>
        <Keycap label="Enter ↵" pressAt={T.enterAt} wide />
      </Fade>

      <Caption text="Typing is slow." from={3.4} to={6.8} />
      <Caption text="Press a shortcut." from={7.2} to={9.4} />
      <Caption text="Speak. Text appears sentence by sentence." from={11.2} to={19.4} />
      <Caption text="Your own replacements." from={20.0} to={22.0} />
      <Caption text="Press Enter. It lands where you were typing." from={22.6} to={27.2} />

      <Outro />
      <Title />
    </AbsoluteFill>
  );
};
