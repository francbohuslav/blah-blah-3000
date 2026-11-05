import React from "react";
import { FONT, ICON_FONT } from "./theme";
import { AppIcon } from "./AppIcon";

// A light copy of the settings window (a standard Windows window), 640 x 700.
const L = {
  window: "#f0f0f0",
  panel: "#ffffff",
  text: "#1a1a1a",
  gray: "#7a7a7a",
  border: "#d0d0d0",
  accent: "#0078d4",
};

const TABS = ["General", "Activation", "Text correction", "Recording", "Hint", "Replacements", "Advanced"];

const Check: React.FC<{ label: string; on?: boolean; indent?: number }> = ({ label, on = true, indent = 0 }) => (
  <div style={{ display: "flex", alignItems: "center", gap: 10, fontSize: 15, marginLeft: indent, marginTop: 9 }}>
    <div
      style={{
        width: 17,
        height: 17,
        border: `1.5px solid ${on ? L.accent : "#6a6a6a"}`,
        background: on ? L.accent : "#fff",
        borderRadius: 3,
        color: "#fff",
        fontSize: 13,
        lineHeight: "15px",
        textAlign: "center",
        flexShrink: 0,
      }}
    >
      {on ? "✓" : ""}
    </div>
    <span>{label}</span>
  </div>
);

const Slider: React.FC<{ label: string; value: number }> = ({ label, value }) => (
  <div style={{ marginTop: 9 }}>
    <div style={{ fontSize: 15 }}>{label}</div>
    <div style={{ position: "relative", height: 18, marginTop: 6 }}>
      <div style={{ position: "absolute", top: 7, left: 0, right: 0, height: 4, background: "#c6c6c6", borderRadius: 2 }} />
      <div style={{ position: "absolute", top: 7, left: 0, width: `${value * 100}%`, height: 4, background: L.accent, borderRadius: 2 }} />
      <div style={{ position: "absolute", top: 1, left: `calc(${value * 100}% - 8px)`, width: 16, height: 16, borderRadius: 8, background: L.accent }} />
    </div>
  </div>
);

const Field: React.FC<{ value: string; width?: number | string; arrow?: boolean }> = ({ value, width = "100%", arrow }) => (
  <div
    style={{
      width,
      height: 30,
      border: `1px solid ${L.border}`,
      background: "#fff",
      borderRadius: 3,
      display: "flex",
      alignItems: "center",
      justifyContent: "space-between",
      padding: "0 10px",
      fontSize: 15,
      marginTop: 6,
      boxSizing: "border-box",
    }}
  >
    <span>{value}</span>
    {arrow && <span style={{ fontFamily: ICON_FONT, fontSize: 11, color: L.gray }}>{""}</span>}
  </div>
);

const Heading: React.FC<{ text: string; top?: number }> = ({ text, top = 16 }) => (
  <div style={{ fontSize: 15, fontWeight: 600, marginTop: top }}>{text}</div>
);

const Note: React.FC<{ text: string }> = ({ text }) => <div style={{ fontSize: 13, color: L.gray, marginTop: 4, lineHeight: "17px" }}>{text}</div>;

const Activation: React.FC = () => (
  <>
    <Heading text="Global shortcut for dictation" top={0} />
    <Field value="Win+Shift+D" />
    <Heading text="Push to talk (dictate without a window)" top={18} />
    <Check label="Hold the keys below and speak: each sentence is typed into the active window" />
    <Field value="Left Ctrl" arrow />
    <Slider label="Hold the keys for: 650 ms before recording starts" value={0.38} />
    <Check label="Latch: if you let go before saying anything, recording goes on until you press the keys again" />
    <Slider label="Latch mode: stop automatically after 3 s without speech" value={0.2} />
    <Check label="Play a short quiet tone when recording starts and ends" />
    <Slider label="While recording, lower the computer's sound to 20 % of the current volume" value={0.2} />
  </>
);

const PROVIDERS = ["OpenAI", "Groq", "Google Gemini", "xAI (Grok)", "OpenRouter", "Ollama (local)", "Other (own address)"];

const Llm: React.FC<{ f: number }> = ({ f }) => {
  const highlight = Math.floor(Math.max(0, f - 0.8) / 0.7) % PROVIDERS.length;
  return (
    <>
      <div style={{ fontSize: 14, lineHeight: "19px" }}>
        The dictation window can send the text to an AI service that fixes mistakes. Use any service with an OpenAI-compatible API or a
        local one such as Ollama. The speech recognition stays on this computer.
      </div>
      <Check label="Enable text correction (a button and Ctrl+K in the dictation window)" />
      <Heading text="Service" top={14} />
      <div style={{ border: `1px solid ${L.border}`, marginTop: 6, background: "#fff" }}>
        {PROVIDERS.map((name, i) => (
          <div
            key={name}
            style={{
              height: 31,
              display: "flex",
              alignItems: "center",
              padding: "0 12px",
              fontSize: 15,
              background: i === highlight ? L.accent : "#fff",
              color: i === highlight ? "#fff" : L.text,
            }}
          >
            {name}
          </div>
        ))}
      </div>
      <Heading text="API key" top={14} />
      <Field value="••••••••••••••••••••" />
      <Note text="The key is stored encrypted for your Windows account. A local service needs no key." />
    </>
  );
};

export const SettingsMock: React.FC<{ tab: "activation" | "llm"; f: number }> = ({ tab, f }) => {
  const active = tab === "activation" ? 1 : 2;
  return (
    <div
      style={{
        width: 640,
        height: 700,
        background: L.window,
        fontFamily: FONT,
        color: L.text,
        boxShadow: "0 40px 100px rgba(0,0,0,0.6)",
        border: "1px solid #5f6368",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      <div style={{ height: 34, background: "#fff", display: "flex", alignItems: "center", padding: "0 10px", gap: 10, fontSize: 13 }}>
        <AppIcon size={18} id={`settings-${tab}`} />
        BlahBlah3000 – settings
      </div>
      <div style={{ display: "flex", flexWrap: "wrap", padding: "10px 12px 0", gap: 0, fontSize: 14 }}>
        {TABS.map((name, i) => (
          <div
            key={name}
            style={{
              padding: "6px 11px",
              background: i === active ? L.panel : "transparent",
              border: i === active ? `1px solid ${L.border}` : "1px solid transparent",
              borderBottom: i === active ? `1px solid ${L.panel}` : "1px solid transparent",
              marginBottom: -1,
              position: "relative",
            }}
          >
            {name}
          </div>
        ))}
      </div>
      <div style={{ flex: 1, margin: "0 12px 12px", background: L.panel, border: `1px solid ${L.border}`, padding: 18, overflow: "hidden" }}>
        {tab === "activation" ? <Activation /> : <Llm f={f} />}
      </div>
    </div>
  );
};
