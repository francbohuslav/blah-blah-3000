import React from "react";
import { AbsoluteFill, useCurrentFrame, useVideoConfig } from "remotion";
import { Caption } from "./Caption";
import { SettingsMock } from "./SettingsMock";
import { C, FONT } from "./theme";
import { Fade } from "./util";

const CARD = 3.4;

const Panel: React.FC<{ title: string; children: React.ReactNode; width?: number }> = ({ title, children, width = 1100 }) => (
  <div
    style={{
      width,
      background: C.windowBg,
      border: `1px solid ${C.windowBorder}`,
      boxShadow: "0 30px 80px rgba(0,0,0,0.55)",
      fontFamily: FONT,
      color: C.text,
      padding: 30,
    }}
  >
    <div style={{ fontSize: 30, fontWeight: 600, marginBottom: 22 }}>{title}</div>
    {children}
  </div>
);

const Row: React.FC<{ said: string; written: string; head?: boolean }> = ({ said, written, head }) => (
  <div
    style={{
      display: "flex",
      fontSize: head ? 24 : 30,
      color: head ? C.muted : C.text,
      padding: "14px 0",
      borderBottom: `1px solid ${C.levelTrack}`,
    }}
  >
    <div style={{ flex: 1 }}>{said}</div>
    <div style={{ flex: 1, color: head ? C.muted : C.busy }}>{written}</div>
  </div>
);

const Replacements: React.FC = () => (
  <Panel title="Replacements and a hint for names">
    <Row head said="What is said" written="What is written" />
    <Row said="blahblah dot com" written="blahblah.com" />
    <Row said="my email" written="anna.novak@example.com" />
    <Row said="kubernetes" written="Kubernetes" />
    <div style={{ marginTop: 26, fontSize: 24, color: C.muted }}>Hint for Whisper</div>
    <div style={{ marginTop: 8, fontSize: 28, background: C.textBoxBg, padding: "14px 18px" }}>Kubernetes, Grafana, Novak. Today I finished the first part.</div>
  </Panel>
);

const Api: React.FC = () => (
  <Panel title="REST API for your own tools" width={1300}>
    <div style={{ background: "#0d0e0f", padding: 26, fontFamily: "Consolas, 'Cascadia Mono', monospace", fontSize: 24, lineHeight: "40px" }}>
      <div style={{ color: C.muted }}># speech in, text out – the same model, on your machine</div>
      <div>
        <span style={{ color: C.level }}>$</span> curl -X POST "http://localhost:8221/transcribe?language=en" --data-binary @speech.pcm
      </div>
      <div style={{ color: C.busy }}>{`{"text":"Hello from my own app.","ms":412}`}</div>
    </div>
  </Panel>
);

// A quick look at the rest: settings, replacements and the REST API.
export const Montage: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const cards: { caption: string; node: React.ReactNode }[] = [
    {
      caption: "Keys, hold time, latch, tones, volume — your way.",
      node: (
        <div style={{ transform: "scale(1.1)", transformOrigin: "top center" }}>
          <SettingsMock tab="activation" f={f} />
        </div>
      ),
    },
    { caption: "Your own replacements and hints.", node: <Replacements /> },
    { caption: "A REST API, if you want more.", node: <Api /> },
  ];
  return (
    <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 40%, #202226, ${C.page})` }}>
      {cards.map((card, i) => {
        const start = i * CARD;
        return (
          <React.Fragment key={card.caption}>
            <Fade
              times={[start, start + 0.35, start + CARD - 0.35, start + CARD]}
              style={{ left: 0, right: 0, top: i === 0 ? 70 : 220, display: "flex", justifyContent: "center" }}
            >
              {card.node}
            </Fade>
            <Caption text={card.caption} from={start + 0.2} to={start + CARD} />
          </React.Fragment>
        );
      })}
    </AbsoluteFill>
  );
};

export const MONTAGE_DURATION = 3 * CARD;
