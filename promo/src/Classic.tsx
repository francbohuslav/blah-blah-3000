import React from "react";
import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { Caption } from "./Caption";
import { DictationWindow } from "./DictationWindow";
import { Keycap } from "./Keycap";
import { MailApp } from "./MailApp";
import { C, FONT } from "./theme";
import { SEGMENTS, T } from "./timeline";
import { clamp, Fade, speechLevel } from "./util";
import { Waveform } from "./Waveform";

// The classic dictation: a shortcut opens the window, sentences appear as you pause, Enter pastes the text into the mail.
// Its times are those of the original promo (see timeline.ts), so it is placed with an offset (the first seconds were the title).
export const Classic: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;

  const speaking = SEGMENTS.some((s) => f >= s.speakStart && f < s.speakEnd);
  const level = speechLevel(frame);

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

      <Fade times={[T.recordingStart + 0.4, T.recordingStart + 0.8, T.replaceAt - 0.3, T.replaceAt + 0.2]} style={{ left: 0, right: 0, top: 230, display: "flex", justifyContent: "center" }}>
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
      <Caption text="Speak. Text appears sentence by sentence." from={11.2} to={16.6} />
      <Caption text="Press Enter. It lands where you were typing." from={17.4} to={22.8} />
    </AbsoluteFill>
  );
};

