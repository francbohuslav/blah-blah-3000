import React from "react";
import { AbsoluteFill, useCurrentFrame, useVideoConfig } from "remotion";
import { Caption } from "./Caption";
import { ChatApp, ChatMessage } from "./ChatApp";
import { Keycap } from "./Keycap";
import { C } from "./theme";
import { TrayLens } from "./TrayLens";
import { Waveform } from "./Waveform";
import { Fade, Seg, speechLevel, typedText } from "./util";

interface ChatSceneConfig {
  contact: string;
  messages: ChatMessage[];
  segs: Seg[];
  keyLabel: string;
  press: number;
  release: number;
  /** The tray icon turns red (after the hold delay) and back after the recording stops. */
  engage: number;
  trayOff: number;
  /** The automatic stop after silence: a countdown ring from `from` to `to`. */
  countdown?: [number, number];
  sendAt?: number;
  captions: [string, number, number][];
}

const ChatScene: React.FC<{ config: ChatSceneConfig }> = ({ config }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const { segs, press, release, engage, trayOff, countdown, sendAt } = config;

  const speaking = segs.some((s) => f >= s.speak[0] && f < s.speak[1]);
  const recording = f >= engage && f < trayOff;
  const lastSpeech = Math.max(...segs.map((s) => s.speak[1]));
  const waveEnd = (countdown ? countdown[0] : release) + 0.4;
  const draft = sendAt !== undefined && f >= sendAt ? "" : typedText(f, segs);
  const full = segs.map((s) => s.text).join(" ");
  const messages: ChatMessage[] = sendAt !== undefined ? [...config.messages, { from: "me", text: full, at: sendAt }] : config.messages;
  const ring = countdown ? 1 - (f - countdown[0]) / (countdown[1] - countdown[0]) : undefined;
  const ringOn = countdown !== undefined && f >= countdown[0] && f < countdown[1] + 0.05 && lastSpeech <= countdown[0];

  return (
    <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 40%, #202226, ${C.page})` }}>
      <div style={{ position: "absolute", left: 210, top: 200 }}>
        <ChatApp f={f} messages={messages} draft={draft} contact={config.contact} />
      </div>

      <Fade times={[press - 0.8, press - 0.4, release + 1.3, release + 1.7]} style={{ left: 160, top: 40 }}>
        <Keycap label={config.keyLabel} pressAt={press} releaseAt={release} />
      </Fade>

      <Fade times={[engage + 0.2, engage + 0.6, waveEnd, waveEnd + 0.4]} style={{ left: 520, top: 55 }}>
        <Waveform amplitude={speaking ? speechLevel(frame) : 0.05} />
      </Fade>

      <div style={{ position: "absolute", right: 60, top: 36 }}>
        <TrayLens f={f} recording={recording} ring={ringOn ? ring : undefined} />
      </div>

      {config.captions.map(([text, from, to]) => (
        <Caption key={text} text={text} from={from} to={to} />
      ))}
    </AbsoluteFill>
  );
};

// Hold a key, speak, let go: the sentences are typed into the field of another application.
export const PTT_SEGS: Seg[] = [
  { text: "Yes, definitely!", speak: [5.2, 6.3], shown: 6.8 },
  { text: "How about the new Thai place near the office?", speak: [7.1, 9.9], shown: 10.6 },
  { text: "I can book a table for noon.", speak: [11.3, 13.4], shown: 14.1 },
];

export const PttScene: React.FC = () => (
  <ChatScene
    config={{
      contact: "Lisa",
      messages: [
        { from: "them", text: "Hey! Are we still on for lunch tomorrow?", at: 0.3 },
        { from: "them", text: "Where should we go?", at: 1.5 },
      ],
      segs: PTT_SEGS,
      keyLabel: "Ctrl",
      press: 4.0,
      engage: 4.7,
      release: 15.2,
      trayOff: 15.8,
      sendAt: 18.8,
      captions: [
        ["Any app. Any text field.", 0.8, 3.6],
        ["Hold Ctrl and speak.", 4.0, 8.6],
        ["No window. Words appear at the cursor.", 8.8, 14.8],
        ["Let go. Done.", 15.2, 18.2],
      ],
    }}
  />
);

export const PTT_DURATION = 21;

// Latch mode: hold briefly, let go, speak; the recording stops by itself after a few seconds of silence.
export const LATCH_SEGS: Seg[] = [{ text: "It is twelve Harbour Street, second floor.", speak: [3.2, 5.6], shown: 6.2 }];

export const LatchScene: React.FC = () => (
  <ChatScene
    config={{
      contact: "Lisa",
      messages: [
        { from: "them", text: "Can you send me the address?", at: 0.3 },
      ],
      segs: LATCH_SEGS,
      keyLabel: "Ctrl",
      press: 1.6,
      engage: 2.3,
      release: 2.5,
      trayOff: 9.1,
      countdown: [5.7, 8.7],
      captions: [
        ["Or hold briefly, let go, and speak.", 0.6, 4.4],
        ["It stops by itself when you stop.", 6.0, 10.0],
      ],
    }}
  />
);

export const LATCH_DURATION = 10.8;
